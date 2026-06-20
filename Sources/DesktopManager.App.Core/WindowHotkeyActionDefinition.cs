namespace DesktopManager.App.Core;

/// <summary>
/// Defines the window operation executed by a ManageWindow hotkey.
/// </summary>
public sealed class WindowHotkeyActionDefinition {
    /// <summary>Window target selector.</summary>
    public string Target { get; set; } = WindowTargets.ActiveWindow;

    /// <summary>Monitor target selector.</summary>
    public string Monitor { get; set; } = MonitorTargets.Current;

    /// <summary>DesktopManager monitor index when a profile is bound to a known workstation layout.</summary>
    public int? MonitorIndex { get; set; }

    /// <summary>Stable monitor identity captured when the profile was edited on a known workstation layout.</summary>
    public string? MonitorStableKey { get; set; }

    /// <summary>Placement to apply after the monitor target is resolved.</summary>
    public string Placement { get; set; } = WindowPlacements.Maximize;

    /// <summary>Exact X coordinate for fixed-position moves.</summary>
    public int? ExactLeft { get; set; }

    /// <summary>Exact Y coordinate for fixed-position moves.</summary>
    public int? ExactTop { get; set; }

    /// <summary>Exact width for fixed-position moves.</summary>
    public int? ExactWidth { get; set; }

    /// <summary>Exact height for fixed-position moves.</summary>
    public int? ExactHeight { get; set; }

    /// <summary>Whether the host should verify observed geometry after execution.</summary>
    public bool VerifyAfterAction { get; set; } = true;
}
