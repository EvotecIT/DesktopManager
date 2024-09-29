namespace DesktopManager;

/// <summary>
/// Represents the position of a monitor.
/// </summary>
public class MonitorPosition {
    /// <summary>
    /// Gets or sets the left position of the monitor.
    /// </summary>
    public int Left { get; set; }

    /// <summary>
    /// Gets or sets the top position of the monitor.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Gets or sets the right position of the monitor.
    /// </summary>
    public int Right { get; set; }

    /// <summary>
    /// Gets or sets the bottom position of the monitor.
    /// </summary>
    public int Bottom { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorPosition"/> class with the specified positions.
    /// </summary>
    /// <param name="left">The left position of the monitor.</param>
    /// <param name="top">The top position of the monitor.</param>
    /// <param name="right">The right position of the monitor.</param>
    /// <param name="bottom">The bottom position of the monitor.</param>
    public MonitorPosition(int left, int top, int right, int bottom) {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}
