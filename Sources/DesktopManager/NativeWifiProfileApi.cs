using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
    private readonly SafeWlanClientHandle _clientHandle;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private NativeWifiMethods.WlanNotificationCallback? _activeNotificationCallback;
    private bool _notificationRegistrationFaulted;
    private bool _disposed;

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

        await _connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            var completion = new TaskCompletionSource<DesktopWifiConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            NativeWifiMethods.WlanNotificationCallback callback = (ref NativeWifiMethods.WlanNotificationData notification, IntPtr _) => {
                TryCompleteConnection(profile, notification, completion);
            };

            _activeNotificationCallback = callback;
            uint error = NativeWifiMethods.WlanRegisterNotification(
                _clientHandle,
                NativeWifiMethods.NotificationSourceAcm,
                true,
                callback,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            if (error != NativeWifiMethods.ErrorSuccess) {
                _activeNotificationCallback = null;
            }
            ThrowWindowsError(error, "Register for WLAN Auto Configuration notifications");
            try {
                Guid interfaceId = profile.InterfaceId;
                var parameters = new NativeWifiMethods.WlanConnectionParameters {
                    ConnectionMode = NativeWifiMethods.WlanConnectionMode.Profile,
                    ProfileName = profile.Name,
                    Dot11Ssid = IntPtr.Zero,
                    DesiredBssidList = IntPtr.Zero,
                    BssType = NativeWifiMethods.Dot11BssType.Any,
                    Flags = 0
                };
                error = NativeWifiMethods.WlanConnect(
                    _clientHandle,
                    ref interfaceId,
                    ref parameters,
                    IntPtr.Zero);
                ThrowWindowsError(error, $"Start connection to saved Wi-Fi profile '{profile.Name}'");

                return await WaitForCompletionAsync(profile, completion.Task, timeout, cancellationToken).ConfigureAwait(false);
            } finally {
                uint unregisterError = NativeWifiMethods.WlanRegisterNotification(
                    _clientHandle,
                    NativeWifiMethods.NotificationSourceNone,
                    true,
                    null,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);
                if (unregisterError == NativeWifiMethods.ErrorSuccess) {
                    _activeNotificationCallback = null;
                } else {
                    _notificationRegistrationFaulted = true;
                }
                GC.KeepAlive(callback);
                ThrowWindowsError(unregisterError, "Unregister WLAN Auto Configuration notifications");
            }
        } finally {
            _connectionGate.Release();
        }
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _clientHandle.Dispose();
        _activeNotificationCallback = null;
        _connectionGate.Dispose();
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

    internal static bool TryReadConnectionNotification(
        NativeWifiMethods.WlanNotificationData notification,
        out string profileName,
        out uint reasonCode) {
        profileName = string.Empty;
        reasonCode = 0;
        if (notification.Data == IntPtr.Zero ||
            notification.DataSize < NativeWifiMethods.ConnectionNotificationMinimumSize) {
            return false;
        }

        profileName = (Marshal.PtrToStringUni(
            IntPtr.Add(notification.Data, NativeWifiMethods.ConnectionNotificationProfileNameOffset),
            NativeWifiMethods.MaxNameLength) ?? string.Empty).TrimEnd('\0');
        reasonCode = unchecked((uint)Marshal.ReadInt32(
            notification.Data,
            NativeWifiMethods.ConnectionNotificationReasonCodeOffset));
        return true;
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
        return new DesktopWifiConnectionResult(
            profile,
            DesktopWifiConnectionOutcome.TimedOut,
            0,
            "No WLAN completion notification was received before the timeout elapsed. The Windows connection attempt may still finish.");
    }

    private static void TryCompleteConnection(
        DesktopWifiProfileInfo profile,
        NativeWifiMethods.WlanNotificationData notification,
        TaskCompletionSource<DesktopWifiConnectionResult> completion) {
        try {
            if (notification.NotificationSource != NativeWifiMethods.NotificationSourceAcm ||
                notification.InterfaceId != profile.InterfaceId ||
                (notification.NotificationCode != NativeWifiMethods.NotificationAcmConnectionComplete &&
                 notification.NotificationCode != NativeWifiMethods.NotificationAcmConnectionAttemptFail) ||
                !TryReadConnectionNotification(notification, out string observedProfile, out uint reasonCode) ||
                !string.Equals(observedProfile, profile.Name, StringComparison.Ordinal)) {
                return;
            }

            bool succeeded = notification.NotificationCode == NativeWifiMethods.NotificationAcmConnectionComplete &&
                             reasonCode == NativeWifiMethods.ErrorSuccess;
            completion.TrySetResult(new DesktopWifiConnectionResult(
                profile,
                succeeded ? DesktopWifiConnectionOutcome.Connected : DesktopWifiConnectionOutcome.Failed,
                reasonCode,
                succeeded ? null : GetReasonText(reasonCode)));
        } catch (Exception ex) {
            completion.TrySetException(ex);
        }
    }

    private static string GetReasonText(uint reasonCode) {
        if (reasonCode == NativeWifiMethods.ErrorSuccess) {
            return "The WLAN Auto Configuration service reported that the connection attempt failed.";
        }

        var buffer = new StringBuilder(1024);
        uint error = NativeWifiMethods.WlanReasonCodeToString(
            reasonCode,
            (uint)buffer.Capacity,
            buffer,
            IntPtr.Zero);
        return error == NativeWifiMethods.ErrorSuccess && buffer.Length > 0
            ? buffer.ToString().Trim()
            : $"Windows WLAN reason code {reasonCode}.";
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
        if (_notificationRegistrationFaulted) {
            throw new InvalidOperationException(
                "The Native Wi-Fi notification registration is faulted. Dispose this service and create a new instance.");
        }
    }
}
