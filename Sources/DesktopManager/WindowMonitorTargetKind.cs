namespace DesktopManager;

/// <summary>
/// Identifies how a window-placement operation chooses the monitor to use.
/// </summary>
public enum WindowMonitorTargetKind {
    /// <summary>Use the monitor that currently contains the target window.</summary>
    Current,

    /// <summary>Use the top-left monitor from the current connected desktop layout.</summary>
    TopLeft,

    /// <summary>Use the top-right monitor from the current connected desktop layout.</summary>
    TopRight,

    /// <summary>Use the bottom-left monitor from the current connected desktop layout.</summary>
    BottomLeft,

    /// <summary>Use the bottom-right monitor from the current connected desktop layout.</summary>
    BottomRight
}
