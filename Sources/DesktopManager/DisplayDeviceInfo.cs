namespace DesktopManager;

public class DisplayDeviceInfo {
    public DISPLAY_DEVICE DisplayDevice { get; set; }
    public RECT Bounds { get; set; }

    public DisplayDeviceInfo(DISPLAY_DEVICE displayDevice, RECT bounds) {
        DisplayDevice = displayDevice;
        Bounds = bounds;
    }
}
