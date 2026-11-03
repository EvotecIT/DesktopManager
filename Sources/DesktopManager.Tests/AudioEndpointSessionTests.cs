using System;

namespace DesktopManager.Tests;

[TestClass]
public sealed class AudioEndpointSessionTests {
    [TestMethod]
    public void RetainedEndpoint_ReadsStateRoutesMutationsAndRejectsUnavailableOrDisposedAccess() {
        var backend = new FakeEndpoint();
        using var session = new AudioEndpointSession(backend);

        Assert.IsTrue(session.IsAvailable);
        AudioEndpointLevelInfo initial = session.GetState();
        Assert.AreEqual(37f, initial.VolumePercent);
        Assert.IsFalse(initial.IsMuted);
        Assert.AreEqual(0.6f, initial.PeakValue);

        session.SetVolume(25f);
        session.SetMute(true);
        Assert.AreEqual(0.25f, backend.VolumeScalar);
        Assert.IsTrue(backend.Muted);
        Assert.AreEqual(37f, initial.VolumePercent, "A returned reading must remain a snapshot.");
        AudioEndpointLevelInfo updated = session.GetState();
        Assert.AreEqual(25f, updated.VolumePercent);
        Assert.IsTrue(updated.IsMuted);
        backend.Peak = null;
        Assert.IsNull(session.GetState().PeakValue, "An endpoint without a meter still supplies volume and mute.");

        var interfaceId = new Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2");
        Assert.AreEqual(backend.InterfacePointer, session.ActivateInterface(interfaceId));
        Assert.AreEqual(interfaceId, backend.LastInterfaceId);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.SetVolume(float.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.SetVolume(float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.SetVolume(101f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.SetVolume(-1f));
        Assert.AreEqual(1, backend.VolumeWrites);

        backend.Available = false;
        Assert.IsFalse(session.IsAvailable);
        Assert.ThrowsExactly<InvalidOperationException>(() => session.GetState());
        Assert.ThrowsExactly<InvalidOperationException>(() => session.SetVolume(50f));
        Assert.ThrowsExactly<InvalidOperationException>(() => session.SetMute(false));
        Assert.ThrowsExactly<InvalidOperationException>(() => session.ActivateInterface(interfaceId));
        Assert.AreEqual(1, backend.VolumeWrites);
        Assert.AreEqual(1, backend.MuteWrites);
        Assert.AreEqual(1, backend.Activations);

        session.Dispose();
        session.Dispose();
        Assert.AreEqual(1, backend.Disposals);
        Assert.IsFalse(session.IsAvailable);
        Assert.ThrowsExactly<ObjectDisposedException>(() => session.GetState());
        Assert.ThrowsExactly<ObjectDisposedException>(() => session.SetMute(false));
        Assert.ThrowsExactly<ObjectDisposedException>(() => session.ActivateInterface(interfaceId));
    }

    private sealed class FakeEndpoint : IAudioEndpointBackend {
        internal bool Available = true;
        internal float VolumeScalar = 0.37f;
        internal bool Muted;
        internal float? Peak = 0.6f;
        internal int VolumeWrites;
        internal int MuteWrites;
        internal int Disposals;
        internal int Activations;
        // A routing token only: the fake never hands out a real COM reference.
        internal readonly IntPtr InterfacePointer = new(42);
        internal Guid LastInterfaceId;

        public bool IsAvailable => Available;
        public AudioEndpointLevelInfo GetState() => new(VolumeScalar * 100f, Muted, Peak);
        public void SetVolume(float scalar) { VolumeScalar = scalar; VolumeWrites++; }
        public void SetMute(bool muted) { Muted = muted; MuteWrites++; }
        public IntPtr ActivateInterface(Guid interfaceId) {
            LastInterfaceId = interfaceId;
            Activations++;
            return InterfacePointer;
        }
        public void Dispose() => Disposals++;
    }
}
