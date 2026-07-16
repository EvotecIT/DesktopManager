using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Captures one active audio endpoint inside a workstation profile.
/// </summary>
public sealed class WorkstationAudioEndpointProfile {
    /// <summary>Gets or sets the stable Windows endpoint identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the friendly endpoint name for diagnostics.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the endpoint data flow.</summary>
    public AudioDataFlow DataFlow { get; set; }

    /// <summary>Gets or sets master volume from 0 through 100 when available.</summary>
    public float? VolumePercent { get; set; }

    /// <summary>Gets or sets master mute when available.</summary>
    public bool? IsMuted { get; set; }

    /// <summary>Gets or sets the default roles assigned to the endpoint.</summary>
    public List<AudioRole> DefaultRoles { get; set; } = new();
}
