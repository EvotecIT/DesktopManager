namespace DesktopManager;

/// <summary>
/// This enumeration is used to set and get slide show options.
/// </summary>
[Flags]
public enum DesktopSlideshowOptions {
    /// <summary>
    /// No slideshow options are enabled.
    /// </summary>
    None = 0,
    /// <summary>
    /// When set, indicates that the order in which images in the slide show are displayed can be randomized.
    /// </summary>
    ShuffleImages = 0x01
}

/// <summary>
/// This enumeration is used to set and get slide show options.
/// </summary>
[Obsolete("Use DesktopSlideshowOptions.")]
[Flags]
public enum DesktopSlideShowOptions {
    /// <summary>
    /// No slideshow options are enabled.
    /// </summary>
    None = DesktopSlideshowOptions.None,
    /// <summary>
    /// When set, indicates that the order in which images in the slide show are displayed can be randomized.
    /// </summary>
    ShuffleImages = DesktopSlideshowOptions.ShuffleImages
}
