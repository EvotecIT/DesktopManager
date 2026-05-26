namespace DesktopManager;

/// <summary>
/// This enumeration is used by GetStatus to indicate the current status of the slideshow.
/// </summary>
[Flags]
public enum DesktopSlideshowState {
    /// <summary>
    /// Indicates that the slideshow is not enabled.
    /// </summary>
    None = 0,
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
/// This enumeration is used by GetStatus to indicate the current status of the slideshow.
/// </summary>
[Obsolete("Use DesktopSlideshowState.")]
[Flags]
public enum DesktopSlideShowState {
    /// <summary>
    /// Indicates that the slideshow is not enabled.
    /// </summary>
    None = DesktopSlideshowState.None,
    /// <summary>
    /// Indicates that the slideshow is enabled.
    /// </summary>
    Enabled = DesktopSlideshowState.Enabled,
    /// <summary>
    /// Indicates that the slideshow is currently running.
    /// </summary>
    Slideshow = DesktopSlideshowState.Slideshow,
    /// <summary>
    /// Indicates that the slideshow is disabled due to a remote session.
    /// </summary>
    DisabledByRemoteSession = DesktopSlideshowState.DisabledByRemoteSession,
}
