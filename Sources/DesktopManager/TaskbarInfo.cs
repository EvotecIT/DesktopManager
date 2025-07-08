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
}
