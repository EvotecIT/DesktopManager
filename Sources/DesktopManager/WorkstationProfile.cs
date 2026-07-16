using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Captures display, personalization, taskbar, and active audio state as one reusable workstation profile.
/// </summary>
public sealed class WorkstationProfile {
    /// <summary>Gets or sets the profile schema version.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Gets or sets the capture timestamp.</summary>
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets connected monitor state.</summary>
    public List<WorkstationMonitorProfile> Monitors { get; set; } = new();

    /// <summary>Gets or sets active audio endpoint state.</summary>
    public List<WorkstationAudioEndpointProfile> AudioEndpoints { get; set; } = new();

    /// <summary>Gets or sets personalization state.</summary>
    public PersonalizationSnapshot Personalization { get; set; } = new();

    /// <summary>Gets or sets taskbar state per connected monitor.</summary>
    public List<WorkstationTaskbarProfile> Taskbars { get; set; } = new();

    /// <summary>Gets or sets the global taskbar auto-hide state.</summary>
    public bool TaskbarAutoHide { get; set; }
}
