namespace DesktopManager;

/// <summary>
/// How deliberately the mouse pointer must push to enter a display guarded by a <see cref="CursorFence"/>.
/// </summary>
public enum CursorFenceMode {
    /// <summary>No fence; the pointer moves freely. A <see cref="CursorFence"/> is not started for this mode.</summary>
    Off,

    /// <summary>The pointer stops at the edge and enters only after a sustained, deliberate push.</summary>
    Sticky,

    /// <summary>The pointer never enters by mouse movement; hold Control to cross.</summary>
    Locked
}
