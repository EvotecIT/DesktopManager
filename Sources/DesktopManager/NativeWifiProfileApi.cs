using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

internal interface IWifiProfileApi : IDisposable {
    IReadOnlyList<DesktopWifiInterfaceInfo> GetInterfaces();
    IReadOnlyList<DesktopWifiProfileInfo> GetProfiles(DesktopWifiInterfaceInfo wifiInterface);
    Task<DesktopWifiConnectionResult> ConnectProfileAsync(
        DesktopWifiProfileInfo profile,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

internal sealed class NativeWifiProfileApi : IWifiProfileApi {
    private const int MaximumInterfaceCount = 1024;
    private const int MaximumProfileCount = 16384;
    private static readonly NativeWifiConnectionCoordinator ConnectionCoordinator = new();
    private readonly SafeWlanClientHandle _clientHandle;
    private NativeWifiMethods.WlanNotificationCallback? _activeNotificationCallback;
    private NativeWifiConnectionAttempt? _ownedAttempt;
    private bool _disposed;
    private int _nativeHandleDisposed;

    public NativeWifiProfileApi() {
        uint error = NativeWifiMethods.WlanOpenHandle(
            NativeWifiMethods.ClientVersionLonghorn,
            IntPtr.Zero,
            out _,
            out SafeWlanClientHandle clientHandle);
        if (error != NativeWifiMethods.ErrorSuccess) {
            clientHandle?.Dispose();
            ThrowWindowsError(error, "Open the Windows Native Wi-Fi service");
        }

        _clientHandle = clientHandle ?? throw new InvalidOperationException(
            "Windows opened the Native Wi-Fi service without returning a client handle.");
    }

    public IReadOnlyList<DesktopWifiInterfaceInfo> GetInterfaces() {
        ThrowIfDisposed();
        uint error = NativeWifiMethods.WlanEnumInterfaces(_clientHandle, IntPtr.Zero, out IntPtr listPointer);
        ThrowWindowsError(error, "Enumerate wireless LAN interfaces");
        try {
            int count = ReadListCount(listPointer, MaximumInterfaceCount, "wireless LAN interface");
            int itemSize = Marshal.SizeOf<NativeWifiMethods.WlanInterfaceInfo>();
            var interfaces = new List<DesktopWifiInterfaceInfo>(count);
            for (int index = 0; index < count; index++) {
                IntPtr itemPointer = IntPtr.Add(listPointer, sizeof(uint) * 2 + itemSize * index);
                NativeWifiMethods.WlanInterfaceInfo item = Marshal.PtrToStructure<NativeWifiMethods.WlanInterfaceInfo>(itemPointer);
                interfaces.Add(new DesktopWifiInterfaceInfo(
                    item.InterfaceId,
                    item.Description ?? string.Empty,
                    ToInterfaceState(item.State)));
            }

            return interfaces.ToArray();
        } finally {
            NativeWifiMethods.WlanFreeMemory(listPointer);
        }
    }

    public IReadOnlyList<DesktopWifiProfileInfo> GetProfiles(DesktopWifiInterfaceInfo wifiInterface) {
        ThrowIfDisposed();
        if (wifiInterface == null) {
            throw new ArgumentNullException(nameof(wifiInterface));
        }

        Guid interfaceId = wifiInterface.InterfaceId;
        uint error = NativeWifiMethods.WlanGetProfileList(
            _clientHandle,
            ref interfaceId,
            IntPtr.Zero,
            out IntPtr listPointer);
        ThrowWindowsError(error, $"Enumerate saved Wi-Fi profiles on interface '{interfaceId}'");
        try {
            int count = ReadListCount(listPointer, MaximumProfileCount, "saved Wi-Fi profile");
            int itemSize = Marshal.SizeOf<NativeWifiMethods.WlanProfileInfo>();
            var profiles = new List<DesktopWifiProfileInfo>(count);
            for (int index = 0; index < count; index++) {
                IntPtr itemPointer = IntPtr.Add(listPointer, sizeof(uint) * 2 + itemSize * index);
                NativeWifiMethods.WlanProfileInfo item = Marshal.PtrToStructure<NativeWifiMethods.WlanProfileInfo>(itemPointer);
                profiles.Add(new DesktopWifiProfileInfo(
                    wifiInterface,
                    item.ProfileName ?? string.Empty,
                    (item.Flags & NativeWifiMethods.ProfileGroupPolicy) != 0,
                    (item.Flags & NativeWifiMethods.ProfileUser) != 0));
            }

            return profiles.ToArray();
        } finally {
            NativeWifiMethods.WlanFreeMemory(listPointer);
        }
    }

    public async Task<DesktopWifiConnectionResult> ConnectProfileAsync(
        DesktopWifiProfileInfo profile,
        TimeSpan timeout,
        CancellationToken cancellationToken) {
        ThrowIfDisposed();
        if (profile == null) {
            throw new ArgumentNullException(nameof(profile));
        }

        var stopwatch = Stopwatch.StartNew();
        bool entered = await ConnectionCoordinator.WaitForTurnAsync(timeout, cancellationToken).ConfigureAwait(false);
        if (!entered) {
            return CreatePendingAttemptTimeoutResult(
                profile,
                "Another Wi-Fi connection request did not finish before the timeout elapsed. No new Windows connection attempt was started.");
        }

        try {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            TimeSpan remaining = GetRemainingTimeout(timeout, stopwatch.Elapsed);
            if (remaining <= TimeSpan.Zero ||
                !await ConnectionCoordinator.DrainAsync(remaining, cancellationToken).ConfigureAwait(false)) {
                return CreatePendingAttemptTimeoutResult(
                    profile,
                    "A previous Windows connection attempt is still pending. No new connection attempt was started before the timeout elapsed.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotificationsRegistered();
            NativeWifiMethods.Dot11BssType bssType = GetProfileBssType(profile);
            var attempt = ConnectionCoordinator.Begin(profile);
            _ownedAttempt = attempt;
            try {
                Guid interfaceId = profile.InterfaceId;
                NativeWifiMethods.WlanConnectionParameters parameters = CreateConnectionParameters(profile, bssType);
                uint error = NativeWifiMethods.WlanConnect(
                    _clientHandle,
                    ref interfaceId,
                    ref parameters,
                    IntPtr.Zero);
                ThrowWindowsError(error, $"Start connection to saved Wi-Fi profile '{profile.Name}'");
            } catch {
                ConnectionCoordinator.Abandon(attempt);
                _ownedAttempt = null;
                throw;
            }

            remaining = GetRemainingTimeout(timeout, stopwatch.Elapsed);
            return remaining <= TimeSpan.Zero
                ? CreateCurrentAttemptTimeoutResult(profile)
                : await WaitForCompletionAsync(profile, attempt.Completion, remaining, cancellationToken).ConfigureAwait(false);
        } finally {
            ConnectionCoordinator.ReleaseTurn();
        }
    }

    internal static NativeWifiMethods.WlanConnectionParameters CreateConnectionParameters(
        DesktopWifiProfileInfo profile,
        NativeWifiMethods.Dot11BssType bssType) {
        return new NativeWifiMethods.WlanConnectionParameters {
            ConnectionMode = NativeWifiMethods.WlanConnectionMode.Profile,
            ProfileName = profile.Name,
            Dot11Ssid = IntPtr.Zero,
            DesiredBssidList = IntPtr.Zero,
            BssType = bssType,
            Flags = 0
        };
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        NativeWifiConnectionAttempt? ownedAttempt = _ownedAttempt;
        if (ownedAttempt != null && !ownedAttempt.Completion.IsCompleted) {
            _ = DisposeAfterCompletionAsync(ownedAttempt.Completion);
        } else {
            DisposeNativeHandle();
        }
    }

    internal static DesktopWifiInterfaceState ToInterfaceState(NativeWifiMethods.WlanInterfaceState state) {
        return state switch {
            NativeWifiMethods.WlanInterfaceState.NotReady => DesktopWifiInterfaceState.NotReady,
            NativeWifiMethods.WlanInterfaceState.Connected => DesktopWifiInterfaceState.Connected,
            NativeWifiMethods.WlanInterfaceState.AdHocNetworkFormed => DesktopWifiInterfaceState.AdHocNetworkFormed,
            NativeWifiMethods.WlanInterfaceState.Disconnecting => DesktopWifiInterfaceState.Disconnecting,
            NativeWifiMethods.WlanInterfaceState.Disconnected => DesktopWifiInterfaceState.Disconnected,
            NativeWifiMethods.WlanInterfaceState.Associating => DesktopWifiInterfaceState.Associating,
            NativeWifiMethods.WlanInterfaceState.Discovering => DesktopWifiInterfaceState.Discovering,
            NativeWifiMethods.WlanInterfaceState.Authenticating => DesktopWifiInterfaceState.Authenticating,
            _ => DesktopWifiInterfaceState.Unknown
        };
    }

    private static async Task<DesktopWifiConnectionResult> WaitForCompletionAsync(
        DesktopWifiProfileInfo profile,
        Task<DesktopWifiConnectionResult> completion,
        TimeSpan timeout,
        CancellationToken cancellationToken) {
        using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task timeoutTask = Task.Delay(timeout, waitCancellation.Token);
        Task finished = await Task.WhenAny(completion, timeoutTask).ConfigureAwait(false);
        if (finished == completion) {
            waitCancellation.Cancel();
            return await completion.ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return CreateCurrentAttemptTimeoutResult(profile);
    }

    private static DesktopWifiConnectionResult CreateCurrentAttemptTimeoutResult(DesktopWifiProfileInfo profile) {
        return new DesktopWifiConnectionResult(
            profile,
            DesktopWifiConnectionOutcome.TimedOut,
            0,
            "No WLAN completion notification was received before the timeout elapsed. The Windows connection attempt may still finish.");
    }

    private static DesktopWifiConnectionResult CreatePendingAttemptTimeoutResult(
        DesktopWifiProfileInfo profile,
        string reason) {
        return new DesktopWifiConnectionResult(
            profile,
            DesktopWifiConnectionOutcome.TimedOut,
            0,
            reason);
    }

    private static TimeSpan GetRemainingTimeout(TimeSpan timeout, TimeSpan elapsed) {
        TimeSpan remaining = timeout - elapsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private void EnsureNotificationsRegistered() {
        if (_activeNotificationCallback != null) {
            return;
        }

        NativeWifiMethods.WlanNotificationCallback callback = ObserveConnectionNotification;
        _activeNotificationCallback = callback;
        uint error = NativeWifiMethods.WlanRegisterNotification(
            _clientHandle,
            NativeWifiMethods.NotificationSourceAcm,
            false,
            callback,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
        if (error != NativeWifiMethods.ErrorSuccess) {
            _activeNotificationCallback = null;
        }
        ThrowWindowsError(error, "Register for WLAN Auto Configuration notifications");
    }

    private static void ObserveConnectionNotification(
        ref NativeWifiMethods.WlanNotificationData notification,
        IntPtr _) {
        ConnectionCoordinator.Observe(notification);
    }

    private async Task DisposeAfterCompletionAsync(Task<DesktopWifiConnectionResult> completion) {
        try {
            await completion.ConfigureAwait(false);
        } catch {
            // The native handle must still be released after a malformed or failed notification.
        }

        DisposeNativeHandle();
    }

    private void DisposeNativeHandle() {
        if (Interlocked.Exchange(ref _nativeHandleDisposed, 1) != 0) {
            return;
        }

        _clientHandle.Dispose();
        _activeNotificationCallback = null;
    }

    private NativeWifiMethods.Dot11BssType GetProfileBssType(DesktopWifiProfileInfo profile) {
        Guid interfaceId = profile.InterfaceId;
        IntPtr profileXmlPointer = IntPtr.Zero;
        try {
            uint error = NativeWifiMethods.WlanGetProfile(
                _clientHandle,
                ref interfaceId,
                profile.Name,
                IntPtr.Zero,
                out profileXmlPointer,
                IntPtr.Zero,
                IntPtr.Zero);
            ThrowWindowsError(error, $"Read the connection type for saved Wi-Fi profile '{profile.Name}'");
            string profileXml = Marshal.PtrToStringUni(profileXmlPointer) ?? throw new InvalidDataException(
                $"Windows returned empty metadata for saved Wi-Fi profile '{profile.Name}'.");
            return NativeWifiProfileParser.ReadBssType(profileXml);
        } finally {
            if (profileXmlPointer != IntPtr.Zero) {
                NativeWifiMethods.WlanFreeMemory(profileXmlPointer);
            }
        }
    }

    private static int ReadListCount(IntPtr listPointer, int maximum, string itemDescription) {
        if (listPointer == IntPtr.Zero) {
            throw new InvalidDataException($"Windows returned a null {itemDescription} list.");
        }

        uint count = unchecked((uint)Marshal.ReadInt32(listPointer));
        if (count > maximum) {
            throw new InvalidDataException($"Windows returned an invalid {itemDescription} count of {count}.");
        }

        return (int)count;
    }

    private static void ThrowWindowsError(uint error, string operation) {
        if (error == NativeWifiMethods.ErrorSuccess) {
            return;
        }

        var exception = new Win32Exception(unchecked((int)error));
        throw new Win32Exception(unchecked((int)error), $"{operation} failed. {exception.Message}");
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(NativeWifiProfileApi));
        }
    }
}
