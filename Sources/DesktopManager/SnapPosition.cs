namespace DesktopManager;

/// <summary>
/// Specifies a window snap position within the monitor's work area.
/// </summary>
public enum SnapPosition
{
    /// <summary>Snap to the left half of the monitor.</summary>
    Left,
    /// <summary>Snap to the right half of the monitor.</summary>
    Right,
    /// <summary>Snap to the upper‑left quarter of the monitor.</summary>
    TopLeft,
    /// <summary>Snap to the upper‑right quarter of the monitor.</summary>
    TopRight,
    /// <summary>Snap to the lower‑left quarter of the monitor.</summary>
    BottomLeft,
    /// <summary>Snap to the lower‑right quarter of the monitor.</summary>
    BottomRight,
    /// <summary>Snap to the upper half of the monitor.</summary>
    Top,
    /// <summary>Snap to the lower half of the monitor.</summary>
    Bottom,
    /// <summary>Snap to the left third of the monitor.</summary>
    LeftThird,
    /// <summary>Snap to the middle third of the monitor.</summary>
    CenterThird,
    /// <summary>Snap to the right third of the monitor.</summary>
    RightThird,
    /// <summary>Keep the window's size and centre it on the monitor, shrinking it if it doesn't fit.</summary>
    Center
}
