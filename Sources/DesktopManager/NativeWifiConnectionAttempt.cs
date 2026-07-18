using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Correlates a Native Wi-Fi completion with the matching connection-start notification.
/// </summary>
internal sealed class NativeWifiConnectionAttempt {
    private const int Created = 0;
    private const int AwaitingStart = 1;
    private const int AwaitingCompletion = 2;
    private const int Completed = 3;
    private readonly TaskCompletionSource<DesktopWifiConnectionResult> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DesktopWifiProfileInfo _profile;
    private int _state;

    internal NativeWifiConnectionAttempt(DesktopWifiProfileInfo profile) {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    internal Task<DesktopWifiConnectionResult> Completion => _completion.Task;

    internal void Begin() {
        if (Interlocked.CompareExchange(ref _state, AwaitingStart, Created) != Created) {
            throw new InvalidOperationException("The Native Wi-Fi connection attempt has already started.");
        }
    }

    internal void Expire(string reason) {
        if (_completion.TrySetResult(new DesktopWifiConnectionResult(
                _profile,
                DesktopWifiConnectionOutcome.TimedOut,
                0,
                reason))) {
            Interlocked.Exchange(ref _state, Completed);
        }
    }

    internal void Observe(NativeWifiMethods.WlanNotificationData notification) {
        try {
            if (notification.NotificationSource != NativeWifiMethods.NotificationSourceAcm ||
                notification.InterfaceId != _profile.InterfaceId ||
                (notification.NotificationCode != NativeWifiMethods.NotificationAcmConnectionStart &&
                 notification.NotificationCode != NativeWifiMethods.NotificationAcmConnectionComplete) ||
                !TryReadConnectionNotification(notification, out string observedProfile, out uint reasonCode) ||
                !string.Equals(observedProfile, _profile.Name, StringComparison.Ordinal)) {
                return;
            }

            if (notification.NotificationCode == NativeWifiMethods.NotificationAcmConnectionStart) {
                Interlocked.CompareExchange(ref _state, AwaitingCompletion, AwaitingStart);
                return;
            }
            if (Volatile.Read(ref _state) != AwaitingCompletion) {
                return;
            }

            bool succeeded = reasonCode == NativeWifiMethods.ErrorSuccess;
            if (_completion.TrySetResult(new DesktopWifiConnectionResult(
                    _profile,
                    succeeded ? DesktopWifiConnectionOutcome.Connected : DesktopWifiConnectionOutcome.Failed,
                    reasonCode,
                    succeeded ? null : GetReasonText(reasonCode)))) {
                Interlocked.Exchange(ref _state, Completed);
            }
        } catch (Exception ex) {
            _completion.TrySetException(ex);
        }
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

        string fixedProfileName = Marshal.PtrToStringUni(
            IntPtr.Add(notification.Data, NativeWifiMethods.ConnectionNotificationProfileNameOffset),
            NativeWifiMethods.MaxNameLength) ?? string.Empty;
        int terminatorIndex = fixedProfileName.IndexOf('\0');
        profileName = terminatorIndex >= 0
            ? fixedProfileName.Substring(0, terminatorIndex)
            : fixedProfileName;
        reasonCode = unchecked((uint)Marshal.ReadInt32(
            notification.Data,
            NativeWifiMethods.ConnectionNotificationReasonCodeOffset));
        return true;
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
}
