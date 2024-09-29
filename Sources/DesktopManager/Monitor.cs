namespace DesktopManager;

/// <summary>
/// Represents a monitor and provides methods to interact with it.
/// </summary>
public class Monitor {
    private readonly MonitorService _monitorService;

    /// <summary>
    /// Gets or sets the index of the monitor.
    /// </summary>
    public int Index { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the monitor is connected.
    /// </summary>
    public bool IsConnected => (StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0;

    /// <summary>
    /// Gets a value indicating whether the monitor is the primary monitor.
    /// </summary>
    public bool IsPrimary => (StateFlags & DisplayDeviceStateFlags.PrimaryDevice) != 0;

    /// <summary>
    /// Gets or sets the device string of the monitor.
    /// </summary>
    public string DeviceString { get; internal set; }

    /// <summary>
    /// Gets the left position of the monitor.
    /// </summary>
    public int PositionLeft => Position.Left;

    /// <summary>
    /// Gets the top position of the monitor.
    /// </summary>
    public int PositionTop => Position.Top;

    /// <summary>
    /// Gets the right position of the monitor.
    /// </summary>
    public int PositionRight => Position.Right;

    /// <summary>
    /// Gets the bottom position of the monitor.
    /// </summary>
    public int PositionBottom => Position.Bottom;

    /// <summary>
    /// Gets or sets the device ID of the monitor.
    /// </summary>
    public string DeviceId { get; internal set; }

    /// <summary>
    /// Gets or sets the wallpaper of the monitor.
    /// </summary>
    public string Wallpaper { get; internal set; }

    /// <summary>
    /// Gets or sets the wallpaper position of the monitor.
    /// </summary>
    public DesktopWallpaperPosition WallpaperPosition { get; internal set; }

    /// <summary>
    /// Gets the position of the monitor.
    /// </summary>
    public MonitorPosition Position => GetMonitorPosition();

    /// <summary>
    /// Gets or sets the device name of the monitor.
    /// </summary>
    public string DeviceName { get; internal set; }

    /// <summary>
    /// Gets or sets the state flags of the monitor.
    /// </summary>
    public DisplayDeviceStateFlags StateFlags { get; internal set; }

    /// <summary>
    /// Gets or sets the device key of the monitor.
    /// </summary>
    public string DeviceKey { get; internal set; }

    /// <summary>
    /// Gets or sets the rectangle that defines the bounds of the monitor.
    /// </summary>
    internal RECT Rect { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Monitor"/> class with the specified monitor service.
    /// </summary>
    /// <param name="monitorService">The monitor service.</param>
    public Monitor(MonitorService monitorService) {
        _monitorService = monitorService;
    }

    /// <summary>
    /// Sets the wallpaper for the monitor.
    /// </summary>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(string wallpaperPath) {
        _monitorService.SetWallpaper(DeviceId, wallpaperPath);
    }

    /// <summary>
    /// Gets the wallpaper of the monitor.
    /// </summary>
    /// <returns>The path to the wallpaper image.</returns>
    public string GetWallpaper() {
        return _monitorService.GetWallpaper(DeviceId);
    }

    /// <summary>
    /// Sets the position of the monitor.
    /// </summary>
    /// <param name="position">The new position of the monitor.</param>
    public void SetMonitorPosition(MonitorPosition position) {
        _monitorService.SetMonitorPosition(DeviceId, position);
    }

    /// <summary>
    /// Sets the position of the monitor.
    /// </summary>
    /// <param name="left">The left position.</param>
    /// <param name="top">The top position.</param>
    /// <param name="right">The right position.</param>
    /// <param name="bottom">The bottom position.</param>
    public void SetMonitorPosition(int left, int top, int right, int bottom) {
        _monitorService.SetMonitorPosition(DeviceId, left, top, right, bottom);
    }

    /// <summary>
    /// Gets the position of the monitor.
    /// </summary>
    /// <returns>The position of the monitor.</returns>
    public MonitorPosition GetMonitorPosition() {
        return _monitorService.GetMonitorPosition(DeviceId);
    }

    /// <summary>
    /// Gets the bounds of the monitor.
    /// </summary>
    /// <returns>The bounds of the monitor.</returns>
    internal RECT GetMonitorBounds() {
        return _monitorService.GetMonitorBounds(DeviceId);
    }
}
