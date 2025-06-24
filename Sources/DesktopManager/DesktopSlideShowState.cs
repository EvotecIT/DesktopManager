namespace DesktopManager;

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
