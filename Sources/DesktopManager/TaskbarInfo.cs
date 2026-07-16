using System;

namespace DesktopManager;

/// <summary>
/// Provides information about a taskbar on a specific monitor.
/// </summary>
public class TaskbarInfo {
    /// <summary>Handle of the taskbar window.</summary>
    public IntPtr Handle { get; internal set; }
    /// <summary>Index of the monitor hosting the taskbar.</summary>
    public int MonitorIndex { get; internal set; }

    /// <summary>Gets whether the taskbar window is currently visible.</summary>
    public bool IsVisible { get; internal set; }

    /// <summary>Gets the screen edge hosting this taskbar.</summary>
    public TaskbarPosition Position { get; internal set; }

    /// <summary>Gets the taskbar window bounds.</summary>
    public MonitorPosition Bounds { get; internal set; } = new(0, 0, 0, 0);
}
