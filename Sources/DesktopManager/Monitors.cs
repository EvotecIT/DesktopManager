using System.IO;

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
    public List<Monitor> GetMonitors(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string deviceId = null, string deviceName = null) {
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
    /// Sets the wallpaper for a specific monitor using image data.
    /// </summary>
    /// <param name="monitorId">The ID of the monitor.</param>
    /// <param name="imageStream">Stream containing image data.</param>
    public void SetWallpaper(string monitorId, Stream imageStream) {
        _monitorService.SetWallpaper(monitorId, imageStream);
    }

    /// <summary>
    /// Sets the wallpaper for a specific monitor from a URL.
    /// </summary>
    /// <param name="monitorId">The ID of the monitor.</param>
    /// <param name="url">URL pointing to the image.</param>
    public void SetWallpaperFromUrl(string monitorId, string url) {
        _monitorService.SetWallpaperFromUrl(monitorId, url);
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
    /// Sets the wallpaper for a monitor by its index using image data.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="imageStream">Stream containing image data.</param>
    public void SetWallpaper(int index, Stream imageStream) {
        _monitorService.SetWallpaper(index, imageStream);
    }

    /// <summary>
    /// Sets the wallpaper for a monitor by its index from a URL.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="url">URL pointing to the image.</param>
    public void SetWallpaperFromUrl(int index, string url) {
        _monitorService.SetWallpaperFromUrl(index, url);
    }

    /// <summary>
    /// Sets the wallpaper for all monitors.
    /// </summary>
    /// <param name="wallpaperPath">The file path of the wallpaper image.</param>
    public void SetWallpaper(string wallpaperPath) {
        _monitorService.SetWallpaper(wallpaperPath);
    }

    /// <summary>
    /// Sets the wallpaper for all monitors using image data.
    /// </summary>
    /// <param name="imageStream">Stream containing image data.</param>
    public void SetWallpaper(Stream imageStream) {
        _monitorService.SetWallpaper(imageStream);
    }

    /// <summary>
    /// Sets the wallpaper for all monitors from a URL.
    /// </summary>
    /// <param name="url">URL pointing to the image.</param>
    public void SetWallpaperFromUrl(string url) {
        _monitorService.SetWallpaperFromUrl(url);
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
    /// Gets the desktop background color.
    /// </summary>
    /// <returns>The background color as RGB value.</returns>
    public uint GetBackgroundColor() {
        return _monitorService.GetBackgroundColor();
    }

    /// <summary>
    /// Sets the desktop background color.
    /// </summary>
    /// <param name="color">Color as RGB value.</param>
    public void SetBackgroundColor(uint color) {
        _monitorService.SetBackgroundColor(color);
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
    /// Sets the resolution of a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="width">The desired width.</param>
    /// <param name="height">The desired height.</param>
    public void SetMonitorResolution(string deviceId, int width, int height) {
        _monitorService.SetMonitorResolution(deviceId, width, height);
    }

    /// <summary>
    /// Sets the resolution of a monitor by its index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="width">The desired width.</param>
    /// <param name="height">The desired height.</param>
    public void SetMonitorResolution(int index, int width, int height) {
        var deviceId = _monitorService.GetMonitorDevicePathAt((uint)index);
        _monitorService.SetMonitorResolution(deviceId, width, height);
    }

    /// <summary>
    /// Sets the orientation of a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="orientation">The orientation to apply.</param>
    public void SetMonitorOrientation(string deviceId, DisplayOrientation orientation) {
        _monitorService.SetMonitorOrientation(deviceId, orientation);
    }

    /// <summary>
    /// Sets the orientation of a monitor by its index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="orientation">The orientation to apply.</param>
    public void SetMonitorOrientation(int index, DisplayOrientation orientation) {
        var deviceId = _monitorService.GetMonitorDevicePathAt((uint)index);
        _monitorService.SetMonitorOrientation(deviceId, orientation);
    }

    /// <summary>
    /// Gets the brightness of a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <returns>The current brightness level.</returns>
    public int GetMonitorBrightness(string deviceId) {
        return _monitorService.GetMonitorBrightness(deviceId);
    }

    /// <summary>
    /// Sets the brightness of a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="brightness">The brightness level to set.</param>
    public void SetMonitorBrightness(string deviceId, int brightness) {
        _monitorService.SetMonitorBrightness(deviceId, brightness);
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
