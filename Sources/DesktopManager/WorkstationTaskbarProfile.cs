namespace DesktopManager;

/// <summary>
/// Captures one taskbar inside a workstation profile.
/// </summary>
public sealed class WorkstationTaskbarProfile {
    /// <summary>Gets or sets the stable key of the monitor hosting the taskbar.</summary>
    public string MonitorStableKey { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the taskbar window was visible.</summary>
    public bool IsVisible { get; set; }

    /// <summary>Gets or sets the taskbar screen edge.</summary>
    public TaskbarPosition Position { get; set; }
}
