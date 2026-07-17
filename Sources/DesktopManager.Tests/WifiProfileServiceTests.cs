using System.Runtime.InteropServices;
using System.Text;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace DesktopManager.Tests;

/// <summary>
/// Protects saved-profile selection and the Native Wi-Fi notification contract without changing live connectivity.
/// </summary>
[TestClass]
#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows6.0.6000.0")]
#endif
public class WifiProfileServiceTests {
    [TestMethod]
    public void GetProfiles_InterfaceFilter_ReturnsOnlyProfilesStoredOnThatInterface() {
        DesktopWifiInterfaceInfo first = CreateInterface("First");
        DesktopWifiInterfaceInfo second = CreateInterface("Second");
        var api = new FakeWifiProfileApi(first, second);
        api.AddProfile(first, "First profile");
        api.AddProfile(second, "Second profile");
        using var service = new WifiProfileService(api);

        IReadOnlyList<DesktopWifiProfileInfo> profiles = service.GetProfiles(second.InterfaceId);

        Assert.HasCount(1, profiles);
        Assert.AreEqual("Second profile", profiles[0].Name);
        Assert.AreEqual(second.InterfaceId, profiles[0].InterfaceId);
    }

    [TestMethod]
    public async Task ConnectProfileAsync_ExactSavedProfile_DelegatesToItsInterface() {
        DesktopWifiInterfaceInfo wifiInterface = CreateInterface("Wireless adapter");
        var api = new FakeWifiProfileApi(wifiInterface);
        api.AddProfile(wifiInterface, "Corporate WiFi");
        using var service = new WifiProfileService(api);

        DesktopWifiConnectionResult result = await service.ConnectProfileAsync("Corporate WiFi");

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(api.LastConnectedProfile);
        Assert.AreEqual("Corporate WiFi", api.LastConnectedProfile.Name);
        Assert.AreEqual(wifiInterface.InterfaceId, api.LastConnectedProfile.InterfaceId);
    }

    [TestMethod]
    public async Task ConnectProfileAsync_AmbiguousProfile_RequiresInterfaceId() {
        DesktopWifiInterfaceInfo first = CreateInterface("First");
        DesktopWifiInterfaceInfo second = CreateInterface("Second");
        var api = new FakeWifiProfileApi(first, second);
        api.AddProfile(first, "Shared");
        api.AddProfile(second, "Shared");
        using var service = new WifiProfileService(api);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ConnectProfileAsync("Shared"));

        StringAssert.Contains(exception.Message, "multiple interfaces");
        Assert.IsNull(api.LastConnectedProfile);
    }

    [TestMethod]
    public async Task ConnectProfileAsync_ProfileNameComparison_PreservesWindowsCaseSensitivity() {
        DesktopWifiInterfaceInfo wifiInterface = CreateInterface("Wireless adapter");
        var api = new FakeWifiProfileApi(wifiInterface);
        api.AddProfile(wifiInterface, "CaseSensitive");
        using var service = new WifiProfileService(api);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConnectProfileAsync("casesensitive"));

        Assert.IsNull(api.LastConnectedProfile);
    }

    [TestMethod]
    public async Task ConnectProfileAsync_UnsupportedTimeout_FailsBeforeNativeConnection() {
        DesktopWifiInterfaceInfo wifiInterface = CreateInterface("Wireless adapter");
        var api = new FakeWifiProfileApi(wifiInterface);
        api.AddProfile(wifiInterface, "Saved");
        using var service = new WifiProfileService(api);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.ConnectProfileAsync("Saved", timeout: TimeSpan.FromMilliseconds(-1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.ConnectProfileAsync("Saved", timeout: TimeSpan.FromMilliseconds((double)int.MaxValue + 1)));

        Assert.IsNull(api.LastConnectedProfile);
    }

    [TestMethod]
    public void TryReadConnectionNotification_StopsProfileAtFirstNullTerminator() {
        IntPtr data = Marshal.AllocHGlobal(NativeWifiMethods.ConnectionNotificationMinimumSize);
        try {
            Marshal.Copy(
                Enumerable.Repeat((byte)0x41, NativeWifiMethods.ConnectionNotificationMinimumSize).ToArray(),
                0,
                data,
                NativeWifiMethods.ConnectionNotificationMinimumSize);
            byte[] profileName = Encoding.Unicode.GetBytes("Saved profile\0");
            Marshal.Copy(profileName, 0, IntPtr.Add(data, NativeWifiMethods.ConnectionNotificationProfileNameOffset), profileName.Length);
            Marshal.WriteInt32(data, NativeWifiMethods.ConnectionNotificationReasonCodeOffset, 12345);
            var notification = new NativeWifiMethods.WlanNotificationData {
                Data = data,
                DataSize = NativeWifiMethods.ConnectionNotificationMinimumSize
            };

            bool parsed = NativeWifiProfileApi.TryReadConnectionNotification(notification, out string observedProfile, out uint reasonCode);

            Assert.IsTrue(parsed);
            Assert.AreEqual("Saved profile", observedProfile);
            Assert.AreEqual(12345u, reasonCode);
        } finally {
            Marshal.FreeHGlobal(data);
        }
    }

    [TestMethod]
    public async Task TryCompleteConnection_IgnoresIntermediateAttemptFailureUntilConnectionCompletes() {
        DesktopWifiInterfaceInfo wifiInterface = CreateInterface("Wireless adapter");
        var profile = new DesktopWifiProfileInfo(wifiInterface, "Saved profile", false, false);
        var completion = new TaskCompletionSource<DesktopWifiConnectionResult>();
        IntPtr data = Marshal.AllocHGlobal(NativeWifiMethods.ConnectionNotificationMinimumSize);
        try {
            Marshal.Copy(
                new byte[NativeWifiMethods.ConnectionNotificationMinimumSize],
                0,
                data,
                NativeWifiMethods.ConnectionNotificationMinimumSize);
            byte[] profileName = Encoding.Unicode.GetBytes("Saved profile\0");
            Marshal.Copy(profileName, 0, IntPtr.Add(data, NativeWifiMethods.ConnectionNotificationProfileNameOffset), profileName.Length);
            Marshal.WriteInt32(data, NativeWifiMethods.ConnectionNotificationReasonCodeOffset, 12345);
            var notification = new NativeWifiMethods.WlanNotificationData {
                NotificationSource = NativeWifiMethods.NotificationSourceAcm,
                NotificationCode = NativeWifiMethods.NotificationAcmConnectionAttemptFail,
                InterfaceId = wifiInterface.InterfaceId,
                Data = data,
                DataSize = NativeWifiMethods.ConnectionNotificationMinimumSize
            };

            NativeWifiProfileApi.TryCompleteConnection(profile, notification, completion);

            Assert.IsFalse(completion.Task.IsCompleted);

            Marshal.WriteInt32(data, NativeWifiMethods.ConnectionNotificationReasonCodeOffset, 0);
            notification.NotificationCode = NativeWifiMethods.NotificationAcmConnectionComplete;
            NativeWifiProfileApi.TryCompleteConnection(profile, notification, completion);

            DesktopWifiConnectionResult result = await completion.Task;
            Assert.AreEqual(DesktopWifiConnectionOutcome.Connected, result.Outcome);
        } finally {
            Marshal.FreeHGlobal(data);
        }
    }

    [TestMethod]
    public void ToInterfaceState_UnknownNativeValue_RemainsExplicitlyUnknown() {
        DesktopWifiInterfaceState state = NativeWifiProfileApi.ToInterfaceState(
            (NativeWifiMethods.WlanInterfaceState)int.MaxValue);

        Assert.AreEqual(DesktopWifiInterfaceState.Unknown, state);
    }

    private static DesktopWifiInterfaceInfo CreateInterface(string description) {
        return new DesktopWifiInterfaceInfo(Guid.NewGuid(), description, DesktopWifiInterfaceState.Disconnected);
    }

    private sealed class FakeWifiProfileApi : IWifiProfileApi {
        private readonly IReadOnlyList<DesktopWifiInterfaceInfo> _interfaces;
        private readonly Dictionary<Guid, List<DesktopWifiProfileInfo>> _profiles = new();

        public FakeWifiProfileApi(params DesktopWifiInterfaceInfo[] interfaces) {
            _interfaces = interfaces;
        }

        public DesktopWifiProfileInfo? LastConnectedProfile { get; private set; }

        public void AddProfile(DesktopWifiInterfaceInfo wifiInterface, string name) {
            if (!_profiles.TryGetValue(wifiInterface.InterfaceId, out List<DesktopWifiProfileInfo>? profiles)) {
                profiles = new List<DesktopWifiProfileInfo>();
                _profiles[wifiInterface.InterfaceId] = profiles;
            }
            profiles.Add(new DesktopWifiProfileInfo(wifiInterface, name, false, false));
        }

        public IReadOnlyList<DesktopWifiInterfaceInfo> GetInterfaces() {
            return _interfaces;
        }

        public IReadOnlyList<DesktopWifiProfileInfo> GetProfiles(DesktopWifiInterfaceInfo wifiInterface) {
            return _profiles.TryGetValue(wifiInterface.InterfaceId, out List<DesktopWifiProfileInfo>? profiles)
                ? profiles
                : Array.Empty<DesktopWifiProfileInfo>();
        }

        public Task<DesktopWifiConnectionResult> ConnectProfileAsync(
            DesktopWifiProfileInfo profile,
            TimeSpan timeout,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            LastConnectedProfile = profile;
            return Task.FromResult(new DesktopWifiConnectionResult(
                profile,
                DesktopWifiConnectionOutcome.Connected,
                0,
                null));
        }

        public void Dispose() {
        }
    }
}
