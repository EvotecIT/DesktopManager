namespace DesktopManager;

/// <summary>
/// Represents the bounds of a monitor.
/// </summary>
public class MonitorBounds {
    /// <summary>
    /// The left position of the monitor.
    /// </summary>
    public int Left;

    /// <summary>
    /// The top position of the monitor.
    /// </summary>
    public int Top;

    /// <summary>
    /// The right position of the monitor.
    /// </summary>
    public int Right;

    /// <summary>
    /// The bottom position of the monitor.
    /// </summary>
    public int Bottom;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorBounds"/> class with the specified rectangle.
    /// </summary>
    /// <param name="rect">The rectangle that defines the bounds of the monitor.</param>
    public MonitorBounds(RECT rect) {
        Left = rect.Left;
        Top = rect.Top;
        Right = rect.Right;
        Bottom = rect.Bottom;
    }
}
