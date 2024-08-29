namespace DesktopManager;

/// <summary>
/// This enumeration is used to set and get slide show options.
/// </summary> 
public enum DesktopSlideShowOptions {
    /// <summary>
    /// When set, indicates that the order in which images in the slide show are displayed can be randomized
    /// </summary>
    ShuffleImages = 0x01
}


/// <summary>
/// This enumeration is used by GetStatus to indicate the current status of the slideshow.
/// </summary>
public enum DesktopSlideShowState {
    Enabled = 0x01,
    Slideshow = 0x02,
    DisabledByRemoteSession = 0x04,
}


/// <summary>
/// This enumeration is used by the AdvanceSlideshow method to indicate whether to advance the slide show forward or backward.
/// </summary>
public enum DesktopSlideshowDirection {
    Forward = 0,
    Backward = 1,
}

/// <summary>
/// This enumeration indicates the wallpaper position for all monitors. (This includes when slideshows are running.)
/// The wallpaper position specifies how the image that is assigned to a monitor should be displayed.
/// </summary>
public enum DesktopWallpaperPosition {
    Center = 0,
    Tile = 1,
    Stretch = 2,
    Fit = 3,
    Fill = 4,
    Span = 5,
}

/// <summary>
/// This enumeration is used during display change confirmation process.
/// </summary>
public enum DisplayChangeConfirmation : int {
    Successful = 0,
    Restart = 1,
    Failed = -1,
    BadMode = -2,
    NotUpdated = -3,
    BadFlags = -4,
    BadParam = -5,
    BadDualView = -6
}
