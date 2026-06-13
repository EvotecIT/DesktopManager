namespace DesktopManager;

/// <summary>
/// Identifies the placement to apply to a desktop window.
/// </summary>
public enum WindowPlacementKind {
    /// <summary>Restore the target window without changing its geometry.</summary>
    Restore,

    /// <summary>Maximize the target window, optionally after moving it to a target monitor.</summary>
    Maximize,

    /// <summary>Place the target window in the left half of the target monitor.</summary>
    LeftHalf,

    /// <summary>Place the target window in the right half of the target monitor.</summary>
    RightHalf,

    /// <summary>Move and resize the target window to the exact rectangle in the request.</summary>
    ExactRectangle
}
