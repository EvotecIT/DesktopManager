namespace DesktopManager;

/// <summary>
/// Describes the current desktop mouse state.
/// </summary>
public sealed class DesktopMouseState {
    /// <summary>
    /// Gets or sets the cursor X coordinate in screen space.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the cursor Y coordinate in screen space.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets whether the left mouse button is currently pressed.
    /// </summary>
    public bool IsLeftButtonDown { get; set; }

    /// <summary>
    /// Gets or sets whether the right mouse button is currently pressed.
    /// </summary>
    public bool IsRightButtonDown { get; set; }

    /// <summary>
    /// Gets or sets whether the cursor is currently visible.
    /// </summary>
    public bool IsCursorVisible { get; set; }

    /// <summary>
    /// Gets or sets the current cursor handle when available.
    /// </summary>
    public IntPtr CursorHandle { get; set; }
}
