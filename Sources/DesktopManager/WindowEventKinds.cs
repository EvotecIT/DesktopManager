using System;

namespace DesktopManager;

/// <summary>
/// Window changes a <see cref="WindowEventWatcher"/> can report. Combine values to choose which hooks it installs.
/// </summary>
[Flags]
public enum WindowEventKinds {
    /// <summary>No events.</summary>
    None = 0,

    /// <summary>The user started moving or resizing a window.</summary>
    MoveSizeStarted = 1,

    /// <summary>The user finished moving or resizing a window.</summary>
    MoveSizeEnded = 2,

    /// <summary>A window moved, resized, maximized or restored, including while it animates. Frequent.</summary>
    LocationChanged = 4,

    /// <summary>A window was shown.</summary>
    Shown = 8,

    /// <summary>A window was destroyed.</summary>
    Destroyed = 16,

    /// <summary>A window became the foreground window.</summary>
    Foreground = 32,

    /// <summary>All events.</summary>
    All = MoveSizeStarted | MoveSizeEnded | LocationChanged | Shown | Destroyed | Foreground
}
