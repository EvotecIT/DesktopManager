namespace DesktopManager;

/// <summary>
/// This enumeration indicates the wallpaper position for all monitors. (This includes when slideshows are running.)
/// The wallpaper position specifies how the image that is assigned to a monitor should be displayed.
/// </summary>
public enum DesktopWallpaperPosition {
    /// <summary>
    /// Center the wallpaper on the monitor.
    /// </summary>
    Center = 0,
    /// <summary>
    /// Tile the wallpaper across the monitor.
    /// </summary>
    Tile = 1,
    /// <summary>
    /// Stretch the wallpaper to fill the monitor.
    /// </summary>
    Stretch = 2,
    /// <summary>
    /// Fit the wallpaper to the monitor.
    /// </summary>
    Fit = 3,
    /// <summary>
    /// Fill the monitor with the wallpaper.
    /// </summary>
    Fill = 4,
    /// <summary>
    /// Span the wallpaper across all monitors.
    /// </summary>
    Span = 5,
}
