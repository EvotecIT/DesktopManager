namespace DesktopManager;

/// <summary>
/// Represents information about a display device, including its display device details and bounds.
/// </summary>
public class DisplayDeviceInfo {
    /// <summary>
    /// Gets or sets the display device details.
    /// </summary>
    public DISPLAY_DEVICE DisplayDevice { get; set; }

    /// <summary>
    /// Gets or sets the bounds of the display device.
    /// </summary>
    public RECT Bounds { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayDeviceInfo"/> class with the specified display device and bounds.
    /// </summary>
    /// <param name="displayDevice">The display device details.</param>
    /// <param name="bounds">The bounds of the display device.</param>
    public DisplayDeviceInfo(DISPLAY_DEVICE displayDevice, RECT bounds) {
        DisplayDevice = displayDevice;
        Bounds = bounds;
    }
}