using System;

namespace DesktopManager;

/// <summary>
/// Provides one Core Audio endpoint notification.
/// </summary>
public sealed class AudioEndpointChangedEventArgs : EventArgs {
    internal AudioEndpointChangedEventArgs(
        AudioEndpointChangeKind changeKind,
        string deviceId,
        AudioEndpointState? state = null,
        AudioDataFlow? dataFlow = null,
        AudioRole? role = null) {
        ChangeKind = changeKind;
        DeviceId = deviceId;
        State = state;
        DataFlow = dataFlow;
        Role = role;
    }

    /// <summary>Gets the kind of endpoint change.</summary>
    public AudioEndpointChangeKind ChangeKind { get; }

    /// <summary>Gets the affected Windows endpoint identifier.</summary>
    public string DeviceId { get; }

    /// <summary>Gets the new endpoint state for a state notification.</summary>
    public AudioEndpointState? State { get; }

    /// <summary>Gets the affected data flow for a default endpoint notification.</summary>
    public AudioDataFlow? DataFlow { get; }

    /// <summary>Gets the affected role for a default endpoint notification.</summary>
    public AudioRole? Role { get; }
}
