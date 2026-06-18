namespace DesktopManager.App.Core;

/// <summary>
/// Monitor target identifiers used by window-management hotkey actions.
/// </summary>
public static class MonitorTargets {
    /// <summary>The monitor that currently contains the target window.</summary>
    public const string Current = "Current";

    /// <summary>The configured top-left monitor.</summary>
    public const string TopLeft = "TopLeft";

    /// <summary>The configured top-right monitor.</summary>
    public const string TopRight = "TopRight";

    /// <summary>The configured bottom-left monitor.</summary>
    public const string BottomLeft = "BottomLeft";

    /// <summary>The configured bottom-right monitor.</summary>
    public const string BottomRight = "BottomRight";
}
