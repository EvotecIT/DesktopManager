using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Captures a Windows Core Audio endpoint and its current user-facing state.
/// </summary>
public sealed class AudioEndpointInfo {
    internal AudioEndpointInfo(
        string id,
        string name,
        AudioDataFlow dataFlow,
        AudioEndpointState state,
        float? volumePercent,
        bool? isMuted,
        IReadOnlyList<AudioRole> defaultRoles) {
        Id = id;
        Name = name;
        DataFlow = dataFlow;
        State = state;
        VolumePercent = volumePercent;
        IsMuted = isMuted;
        DefaultRoles = defaultRoles;
    }

    /// <summary>Gets the stable Windows endpoint identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the friendly endpoint name.</summary>
    public string Name { get; }

    /// <summary>Gets whether the endpoint renders or captures audio.</summary>
    public AudioDataFlow DataFlow { get; }

    /// <summary>Gets the endpoint device state.</summary>
    public AudioEndpointState State { get; }

    /// <summary>Gets master volume from 0 through 100, or <c>null</c> when unavailable.</summary>
    public float? VolumePercent { get; }

    /// <summary>Gets the master mute state, or <c>null</c> when unavailable.</summary>
    public bool? IsMuted { get; }

    /// <summary>Gets the default roles currently assigned to this endpoint.</summary>
    public IReadOnlyList<AudioRole> DefaultRoles { get; }

    /// <summary>Gets whether this endpoint is a default for at least one role.</summary>
    public bool IsDefault => DefaultRoles.Count > 0;
}
