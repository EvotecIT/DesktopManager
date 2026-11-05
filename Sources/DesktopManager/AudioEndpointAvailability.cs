using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>Recognizes Core Audio failures that mean the endpoint went away or audio is unavailable, rather than a defect.</summary>
internal static class AudioEndpointAvailability {
    // Element not found: no default endpoint for the flow and role.
    private const int NotFound = unchecked((int)0x80070490);
    // The device is not connected.
    private const int DeviceNotConnected = unchecked((int)0x8007048F);
    // Access denied, also reported as UnauthorizedAccessException.
    private const int AccessDenied = unchecked((int)0x80070005);
    // The RPC server is unavailable: the audio service stopped.
    private const int ServerUnavailable = unchecked((int)0x800706BA);
    // The object invoked has disconnected from its clients.
    private const int Disconnected = unchecked((int)0x80010108);
    private const int DeviceInvalidated = unchecked((int)0x88890004);
    private const int ServiceNotRunning = unchecked((int)0x88890010);
    private const int ResourcesInvalidated = unchecked((int)0x88890026);

    internal static bool IsUnavailable(Exception exception) {
        if (exception is UnauthorizedAccessException) {
            return true;
        }
        if (!(exception is COMException)) {
            return false;
        }
        switch (exception.HResult) {
            case NotFound:
            case DeviceNotConnected:
            case AccessDenied:
            case ServerUnavailable:
            case Disconnected:
            case DeviceInvalidated:
            case ServiceNotRunning:
            case ResourcesInvalidated:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsDefaultChange(AudioEndpointChangedEventArgs change, AudioDataFlow flow, AudioRole role) {
        return change.ChangeKind == AudioEndpointChangeKind.DefaultChanged && change.DataFlow == flow && change.Role == role;
    }
}
