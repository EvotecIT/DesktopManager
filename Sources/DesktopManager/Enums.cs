namespace DesktopManager;

/// <summary>
/// This enumeration is used to set and get slide show options.
/// </summary>
public enum DesktopSlideShowOptions {
    /// <summary>
    /// When set, indicates that the order in which images in the slide show are displayed can be randomized.
    /// </summary>
    ShuffleImages = 0x01
}


/// <summary>
/// This enumeration is used by GetStatus to indicate the current status of the slideshow.
/// </summary>
public enum DesktopSlideShowState {
    /// <summary>
    /// Indicates that the slideshow is enabled.
    /// </summary>
    Enabled = 0x01,
    /// <summary>
    /// Indicates that the slideshow is currently running.
    /// </summary>
    Slideshow = 0x02,
    /// <summary>
    /// Indicates that the slideshow is disabled due to a remote session.
    /// </summary>
    DisabledByRemoteSession = 0x04,
}


/// <summary>
/// This enumeration is used by the AdvanceSlideshow method to indicate whether to advance the slide show forward or backward.
/// </summary>
public enum DesktopSlideshowDirection {
    /// <summary>
    /// Advance the slideshow forward.
    /// </summary>
    Forward = 0,
    /// <summary>
    /// Advance the slideshow backward.
    /// </summary>
    Backward = 1,
}

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

/// <summary>
/// This enumeration is used during display change confirmation process.
/// </summary>
public enum DisplayChangeConfirmation : int {
    /// <summary>
    /// The display change was successful.
    /// </summary>
    Successful = 0,
    /// <summary>
    /// The display change requires a restart.
    /// </summary>
    Restart = 1,
    /// <summary>
    /// The display change failed.
    /// </summary>
    Failed = -1,
    /// <summary>
    /// The display mode is not supported.
    /// </summary>
    BadMode = -2,
    /// <summary>
    /// The display was not updated.
    /// </summary>
    NotUpdated = -3,
    /// <summary>
    /// The display flags are invalid.
    /// </summary>
    BadFlags = -4,
    /// <summary>
    /// The display parameters are invalid.
    /// </summary>
    BadParam = -5,
    /// <summary>
    /// The dual view mode is invalid.
    /// </summary>
    BadDualView = -6
}

/// <summary>
/// Specifies the orientation of the display.
/// </summary>
public enum DisplayOrientation {
    /// <summary>
    /// Default landscape orientation.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Rotated 90 degrees.
    /// </summary>
    Degrees90 = 1,
    /// <summary>
    /// Rotated 180 degrees.
    /// </summary>
    Degrees180 = 2,
    /// <summary>
    /// Rotated 270 degrees.
    /// </summary>
    Degrees270 = 3
}

/// <summary>
/// Specifies the DPI awareness of a process.
/// </summary>
public enum ProcessDpiAwareness {
    /// <summary>The process is not DPI aware.</summary>
    Process_DPI_Unaware = 0,
    /// <summary>The process is system DPI aware.</summary>
    Process_System_DPI_Aware = 1,
    /// <summary>The process is per-monitor DPI aware.</summary>
    Process_Per_Monitor_DPI_Aware = 2
}
