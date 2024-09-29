namespace DesktopManager;

/// <summary>
/// Provides methods to manage and interact with monitors, including getting monitor information and setting wallpapers.
/// </summary>
public class Monitors {
    private readonly MonitorService _monitorService;

    /// <summary>
    /// Initializes a new instance of the <see cref="Monitors"/> class.
    /// </summary>
    public Monitors() {
        IDesktopManager desktopManager = (IDesktopManager)new DesktopManagerWrapper(); // Explicit cast
        _monitorService = new MonitorService(desktopManager);
    }

    /// <summary>
    /// Gets a list of monitors based on the specified filters.
    /// </summary>
    /// <param name="connectedOnly">If true, only connected monitors are returned.</param>
    /// <param name="primaryOnly">If true, only the primary monitor is returned.</param>
    /// <param name="index">The index of the monitor to return.</param>
    /// <param name="deviceId">The device ID of the monitor to return.</param>
    /// <param name="deviceName">The device name of the monitor to return.</param>
    /// <returns>A list of monitors that match the specified filters.</returns>
    public List<Monitor> GetMonitors(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        var monitorsReturn = new List<Monitor>();
        var monitors = _monitorService.GetMonitors();
        foreach (var monitor in monitors) {
            if (connectedOnly != null && connectedOnly.Value && !monitor.IsConnected) {
                continue;
            }
            if (primaryOnly != null && primaryOnly.Value && !monitor.IsPrimary) {
                continue;
            }
            if (index != null && monitor.Index != index) {
                continue;
            }
            if (!string.IsNullOrEmpty(deviceId) && monitor.DeviceId != deviceId) {
                continue;
            }
            if (!string.IsNullOrEmpty(deviceName) && monitor.DeviceName != deviceName) {
                continue;
            }
            monitorsReturn.Add(monitor);
        }
        return monitorsReturn;
    }

    /// <summary>
    /// Gets a list of connected monitors.
    /// </summary>
    /// <returns>A list of connected monitors.</returns>
    public List<Monitor> GetMonitorsConnected() {
        return _monitorService.GetMonitorsConnected();
    }

    /// <summary>
    /// Sets the wallpaper for a specific monitor by its ID.
    /// </summary>
    /// <param name="monitorId">The ID of the monitor.</param>
    /// <param name="wallpaperPath">The file path of the wallpaper image.</param>
    public void SetWallpaper(string monitorId, string wallpaperPath) {
        _monitorService.SetWallpaper(monitorId, wallpaperPath);
    }

    /// <summary>
    /// Sets the wallpaper for a specific monitor by its index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="wallpaperPath">The file path of the wallpaper image.</param>
    public void SetWallpaper(int index, string wallpaperPath) {
        _monitorService.SetWallpaper(index, wallpaperPath);
    }

    /// <summary>
    /// Sets the wallpaper for all monitors.
    /// </summary>
    /// <param name="wallpaperPath">The file path of the wallpaper image.</param>
    public void SetWallpaper(string wallpaperPath) {
        _monitorService.SetWallpaper(wallpaperPath);
    }

    /// <summary>
    /// Gets the wallpaper for a specific monitor by its ID.
    /// </summary>
    /// <param name="monitorId">The ID of the monitor.</param>
    /// <returns>The file path of the wallpaper image.</returns>
    public string GetWallpaper(string monitorId) {
        return _monitorService.GetWallpaper(monitorId);
    }

    /// <summary>
    /// Gets the wallpaper for a specific monitor by its index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <returns>The file path of the wallpaper image.</returns>
    public string GetWallpaper(int index) {
        return _monitorService.GetWallpaper(index);
    }

    /// <summary>
    /// Gets the device path of a monitor at the specified index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <returns>The device path of the monitor.</returns>
    public string GetMonitorDevicePathAt(uint index) {
        return _monitorService.GetMonitorDevicePathAt(index);
    }

    /// <summary>
    /// Gets the current wallpaper position.
    /// </summary>
    /// <returns>The current wallpaper position.</returns>
    public DesktopWallpaperPosition GetWallpaperPosition() {
        return _monitorService.GetWallpaperPosition();
    }

    /// <summary>
    /// Sets the wallpaper position.
    /// </summary>
    /// <param name="position">The wallpaper position to set.</param>
    public void SetWallpaperPosition(DesktopWallpaperPosition position) {
        _monitorService.SetWallpaperPosition(position);
    }

    /// <summary>
    /// Gets the bounds of a monitor by its ID.
    /// </summary>
    /// <param name="monitorId">The ID of the monitor.</param>
    /// <returns>The bounds of the monitor.</returns>
    internal RECT GetMonitorRECT(string monitorId) {
        return _monitorService.GetMonitorBounds(monitorId);
    }

    /// <summary>
    /// Gets the position of a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <returns>The position of the monitor.</returns>
    public MonitorPosition GetMonitorPosition(string deviceId) {
        return _monitorService.GetMonitorPosition(deviceId);
    }

    /// <summary>
    /// Sets the position of a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="position">The position to set.</param>
    public void SetMonitorPosition(string deviceId, MonitorPosition position) {
        _monitorService.SetMonitorPosition(deviceId, position);
    }

    /// <summary>
    /// Sets the position of a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="left">The left position.</param>
    /// <param name="top">The top position.</param>
    /// <param name="right">The right position.</param>
    /// <param name="bottom">The bottom position.</param>
    public void SetMonitorPosition(string deviceId, int left, int top, int right, int bottom) {
        _monitorService.SetMonitorPosition(deviceId, left, top, right, bottom);
    }

    /// <summary>
    /// Gets a list of all display devices.
    /// </summary>
    /// <returns>A list of all display devices.</returns>
    public List<DISPLAY_DEVICE> DisplayDevicesAll() {
        return _monitorService.DisplayDevicesAll();
    }

    /// <summary>
    /// Gets a list of connected display devices.
    /// </summary>
    /// <returns>A list of connected display devices.</returns>
    public List<DISPLAY_DEVICE> DisplayDevicesConnected() {
        return _monitorService.DisplayDevicesConnected();
    }
}
