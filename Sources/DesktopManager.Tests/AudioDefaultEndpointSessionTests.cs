using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
public sealed class AudioDefaultEndpointSessionTests {
    private const int DeviceInvalidated = unchecked((int)0x88890004);
    private const int NotFound = unchecked((int)0x80070490);

    [TestMethod]
    public void Session_FollowsMatchingDefaultChangesAndDepartedEndpoints_OnItsOwnMta() {
        var endpoints = new Endpoints();
        var session = new AudioDefaultEndpointSession(AudioDataFlow.Render, AudioRole.Multimedia, endpoints.Open, endpoints.Subscribe);

        AudioEndpointLevelResult first = session.GetState();
        Assert.IsTrue(first.Succeeded);
        Assert.AreEqual(37f, first.Level!.VolumePercent);
        Assert.AreEqual(1, endpoints.Opened.Count);
        Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, endpoints.CallThread, "Core Audio work must leave the caller's thread.");
        Assert.AreEqual(ApartmentState.MTA, endpoints.CallApartment);

        endpoints.Raise(AudioDataFlow.Render, AudioRole.Communications);
        endpoints.Raise(AudioDataFlow.Capture, AudioRole.Multimedia);
        Assert.IsTrue(session.GetState().Succeeded);
        Assert.AreEqual(1, endpoints.Opened.Count, "Another role or direction must not move this session.");

        endpoints.Raise(AudioDataFlow.Render, AudioRole.Multimedia);
        Assert.IsTrue(session.SetVolume(50f).Succeeded);
        Assert.AreEqual(2, endpoints.Opened.Count, "A new default for this role must be opened.");
        Assert.AreEqual(1, endpoints.Opened[0].Disposals);
        Assert.AreEqual(0.5f, endpoints.Opened[1].VolumeScalar);

        endpoints.Opened[1].Available = false;
        Assert.IsTrue(session.SetMute(true).Succeeded, "An endpoint that became inactive must be replaced by the current default.");
        Assert.AreEqual(3, endpoints.Opened.Count);
        Assert.IsTrue(endpoints.Opened[2].Muted);

        endpoints.Opened[2].Failure = DeviceInvalidated;
        Assert.IsTrue(session.GetState().Succeeded, "One reopen must reach the new default after the old one is invalidated.");
        Assert.AreEqual(4, endpoints.Opened.Count);

        endpoints.Opened[3].Failure = DeviceInvalidated;
        endpoints.OpenFailure = NotFound;
        AudioEndpointLevelResult gone = session.GetState();
        Assert.AreEqual(DesktopOperationStatus.Unavailable, gone.Status);
        Assert.IsNull(gone.Level);
        Assert.AreEqual(DesktopOperationStatus.Unavailable, session.SetVolume(10f).Status);
        Assert.IsFalse(session.IsAvailable);

        endpoints.OpenFailure = 0;
        Assert.IsTrue(session.IsAvailable, "The session must recover once a default endpoint exists again.");

        session.Dispose();
        session.Dispose();
        Assert.AreEqual(1, endpoints.Opened[endpoints.Opened.Count - 1].Disposals);
        Assert.IsTrue(endpoints.SubscriptionDisposed);
        Assert.IsFalse(session.IsAvailable);
        Assert.ThrowsExactly<ObjectDisposedException>(() => session.GetState());
    }

    [TestMethod]
    public void Session_ResolvesTheCurrentDefaultWhenNotificationsCannotBeRegistered() {
        var endpoints = new Endpoints();
        using var session = new AudioDefaultEndpointSession(AudioDataFlow.Capture, AudioRole.Communications,
            endpoints.Open, _ => throw new COMException("Notifications unavailable", unchecked((int)0x80004005)));

        Assert.AreEqual(37f, session.GetState().Level!.VolumePercent);
        Endpoint previous = endpoints.Opened[0];
        endpoints.DefaultVolumeScalar = 0.62f;
        Assert.AreEqual(62f, session.GetState().Level!.VolumePercent, "An active old endpoint must not hide a new default when notifications are unavailable.");
        Assert.AreEqual(1, previous.Disposals);

        Assert.IsTrue(session.SetMute(true).Succeeded);
        Assert.IsFalse(previous.Muted, "A current-default mute must not affect the previous microphone.");
        Assert.IsTrue(endpoints.Opened[endpoints.Opened.Count - 1].Muted);
    }

    [TestMethod]
    [DataRow(unchecked((int)0x80070490), true, DisplayName = "No default endpoint")]
    [DataRow(unchecked((int)0x8007048F), true, DisplayName = "Device disconnected")]
    [DataRow(unchecked((int)0x80070005), true, DisplayName = "Access denied")]
    [DataRow(unchecked((int)0x800706BA), true, DisplayName = "Audio service unavailable")]
    [DataRow(unchecked((int)0x80010108), true, DisplayName = "Object disconnected")]
    [DataRow(unchecked((int)0x88890004), true, DisplayName = "Device invalidated")]
    [DataRow(unchecked((int)0x88890010), true, DisplayName = "Audio service not running")]
    [DataRow(unchecked((int)0x88890026), true, DisplayName = "Resources invalidated")]
    [DataRow(unchecked((int)0x80004005), false, DisplayName = "Unspecified failure")]
    public void Session_ReportsDepartedAudioAsUnavailable_AndPropagatesOtherFailures(int hresult, bool unavailable) {
        var endpoints = new Endpoints { OpenFailure = hresult };
        using var session = new AudioDefaultEndpointSession(AudioDataFlow.Capture, AudioRole.Communications, endpoints.Open, endpoints.Subscribe);

        if (unavailable) {
            Assert.AreEqual(DesktopOperationStatus.Unavailable, session.GetState().Status);
            Assert.AreEqual(DesktopOperationStatus.Unavailable, session.SetMute(true).Status);
        } else {
            Exception thrown = Assert.ThrowsExactly<COMException>(() => session.GetState());
            Assert.AreEqual(hresult, thrown.HResult);
        }
    }

    private sealed class Endpoints {
        private EventHandler<AudioEndpointChangedEventArgs>? _handler;
        internal readonly List<Endpoint> Opened = new();
        internal int OpenFailure;
        internal float DefaultVolumeScalar = 0.37f;
        internal int CallThread;
        internal ApartmentState CallApartment;
        internal bool SubscriptionDisposed;

        internal IAudioEndpointBackend Open() {
            CallThread = Thread.CurrentThread.ManagedThreadId;
            CallApartment = Thread.CurrentThread.GetApartmentState();
            if (OpenFailure != 0) {
                Marshal.ThrowExceptionForHR(OpenFailure);
            }
            var endpoint = new Endpoint { VolumeScalar = DefaultVolumeScalar };
            Opened.Add(endpoint);
            return endpoint;
        }

        internal IDisposable Subscribe(EventHandler<AudioEndpointChangedEventArgs> handler) {
            _handler = handler;
            return new Subscription(this);
        }

        internal void Raise(AudioDataFlow flow, AudioRole role) {
            _handler!(this, new AudioEndpointChangedEventArgs(AudioEndpointChangeKind.DefaultChanged, "endpoint", dataFlow: flow, role: role));
        }

        private sealed class Subscription : IDisposable {
            private readonly Endpoints _owner;
            internal Subscription(Endpoints owner) => _owner = owner;
            public void Dispose() => _owner.SubscriptionDisposed = true;
        }
    }

    private sealed class Endpoint : IAudioEndpointBackend {
        internal bool Available = true;
        internal int Failure;
        internal float VolumeScalar = 0.37f;
        internal bool Muted;
        internal int Disposals;

        public bool IsAvailable => Available;
        public AudioEndpointLevelInfo GetState() {
            Fail();
            return new AudioEndpointLevelInfo(VolumeScalar * 100f, Muted, null);
        }
        public void SetVolume(float scalar) { Fail(); VolumeScalar = scalar; }
        public void SetMute(bool muted) { Fail(); Muted = muted; }
        public IntPtr ActivateInterface(Guid interfaceId) => throw new NotSupportedException();
        public void Dispose() => Disposals++;

        private void Fail() {
            if (Failure != 0) {
                Marshal.ThrowExceptionForHR(Failure);
            }
        }
    }
}
