namespace DesktopManager;

/// <summary>
/// Describes the active display mode for a monitor.
/// </summary>
public sealed class MonitorDisplayMode {
    /// <summary>Gets or sets the pixel width.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the pixel height.</summary>
    public int Height { get; set; }

    /// <summary>Gets or sets the refresh rate in hertz.</summary>
    public int RefreshRate { get; set; }

    /// <summary>Gets or sets the display orientation.</summary>
    public DisplayOrientation Orientation { get; set; }
}
