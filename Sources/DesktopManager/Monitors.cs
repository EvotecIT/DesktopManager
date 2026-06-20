using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Provides methods to manage and interact with monitors, including getting monitor information and setting wallpapers.
/// </summary>
public class Monitors {
    private readonly MonitorService _monitorService;
    private List<Monitor>? _cachedMonitors;

    /// <summary>
    /// Initializes a new instance of the <see cref="Monitors"/> class.
    /// </summary>
    public Monitors() {
        IDesktopManager desktopManager = (IDesktopManager)new DesktopManagerWrapper(); // Explicit cast
        _monitorService = new MonitorService(desktopManager);
    }

    /// <summary>
    /// Forces monitor enumeration and updates the cache.
    /// </summary>
    public void RefreshMonitors() {
        _cachedMonitors = _monitorService.GetMonitors();
    }

    /// <summary>
    /// Gets a list of monitors based on the specified filters.
    /// </summary>
    /// <param name="connectedOnly">If true, only connected monitors are returned.</param>
    /// <param name="primaryOnly">If true, only the primary monitor is returned.</param>
    /// <param name="index">The index of the monitor to return.</param>
    /// <param name="deviceId">The device ID of the monitor to return.</param>
    /// <param name="deviceName">The device name of the monitor to return.</param>
    /// <param name="refresh">When true, forces a fresh monitor snapshot before filtering.</param>
    /// <returns>A list of monitors that match the specified filters.</returns>
    public List<Monitor> GetMonitors(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null, bool refresh = false) {
        var monitorsReturn = new List<Monitor>();
        var monitors = refresh || _cachedMonitors == null
            ? _cachedMonitors = _monitorService.GetMonitors()
            : _cachedMonitors;
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
            if (!string.IsNullOrEmpty(deviceId) &&
                !string.Equals(monitor.DeviceId, deviceId, System.StringComparison.OrdinalIgnoreCase)) {
                continue;
            }
            if (!string.IsNullOrEmpty(deviceName) &&
                !string.Equals(monitor.DeviceName, deviceName, System.StringComparison.OrdinalIgnoreCase)) {
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
    /// Gets the current connected monitor topology using stable identities and visual row/column ordering.
    /// </summary>
    /// <param name="refresh">When true, forces a fresh monitor snapshot before building topology.</param>
    /// <returns>The current connected monitor topology.</returns>
    public MonitorTopologySnapshot GetMonitorTopology(bool refresh = false) {
        return MonitorTopologySnapshot.FromMonitors(GetMonitors(connectedOnly: true, refresh: refresh));
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
    /// Asynchronously sets the wallpaper for a specific monitor from a URL.
    /// </summary>
    /// <param name="monitorId">The ID of the monitor.</param>
    /// <param name="url">URL pointing to the image.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetWallpaperFromUrlAsync(string monitorId, string url) {
        return _monitorService.SetWallpaperFromUrlAsync(monitorId, url);
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
    /// Asynchronously sets the wallpaper for a monitor by its index from a URL.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="url">URL pointing to the image.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetWallpaperFromUrlAsync(int index, string url) {
        return _monitorService.SetWallpaperFromUrlAsync(index, url);
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
    /// Asynchronously sets the wallpaper for all monitors from a URL.
    /// </summary>
    /// <param name="url">URL pointing to the image.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetWallpaperFromUrlAsync(string url) {
        return _monitorService.SetWallpaperFromUrlAsync(url);
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
    /// Gets the current system theme.
    /// </summary>
    /// <returns>The current <see cref="SystemTheme"/>.</returns>
    public SystemTheme GetSystemTheme() {
        return _monitorService.GetSystemTheme();
    }

    /// <summary>
    /// Sets the current system theme.
    /// </summary>
    /// <param name="theme">The desired theme.</param>
    public void SetSystemTheme(SystemTheme theme) {
        _monitorService.SetSystemTheme(theme);
    }

    /// <summary>
    /// Sets the logon (lock screen) wallpaper.
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public void SetLogonWallpaper(string imagePath) {
        _monitorService.SetLogonWallpaper(imagePath);
    }

    /// <summary>
    /// Sets the wallpaper for all user profiles.
    /// </summary>
    /// <param name="wallpaperPath">Path to the wallpaper image.</param>
    /// <param name="position">Wallpaper position to store for users.</param>
    /// <param name="includeDefaultProfile">Whether to update the default user profile.</param>
    public void SetWallpaperForAllUsers(string wallpaperPath, DesktopWallpaperPosition position, bool includeDefaultProfile = true) {
        _monitorService.SetWallpaperForAllUsers(wallpaperPath, position, includeDefaultProfile);
    }

    /// <summary>
    /// Gets the current logon (lock screen) wallpaper path if available.
    /// </summary>
    /// <returns>Path to the wallpaper or empty string.</returns>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public string GetLogonWallpaper() {
        return _monitorService.GetLogonWallpaper();
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
    /// Sets the DPI scaling of a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="scalingPercent">The DPI scaling percentage.</param>
    public void SetMonitorDpiScaling(string deviceId, int scalingPercent) {
        _monitorService.SetMonitorDpiScaling(deviceId, scalingPercent);
    }

    /// <summary>
    /// Sets the DPI scaling of a monitor by its index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="scalingPercent">The DPI scaling percentage.</param>
    public void SetMonitorDpiScaling(int index, int scalingPercent) {
        var deviceId = _monitorService.GetMonitorDevicePathAt((uint)index);
        _monitorService.SetMonitorDpiScaling(deviceId, scalingPercent);
    }

    /// <summary>
    /// Starts a wallpaper slideshow on the desktop.
    /// </summary>
    /// <param name="wallpaperPath">Paths to slideshow images.</param>
    public void StartWallpaperSlideshow(IEnumerable<string> wallpaperPath) {
        _monitorService.StartWallpaperSlideshow(wallpaperPath);
    }

    /// <summary>
    /// Starts a wallpaper slideshow on the desktop with optional slideshow settings.
    /// </summary>
    /// <param name="wallpaperPath">Paths to slideshow images.</param>
    /// <param name="options">Optional slideshow options.</param>
    /// <param name="slideshowTick">Optional slideshow tick interval in milliseconds.</param>
    public void StartWallpaperSlideshow(IEnumerable<string> wallpaperPath, DesktopSlideshowOptions? options, uint? slideshowTick) {
        _monitorService.StartWallpaperSlideshow(wallpaperPath, options, slideshowTick);
    }

    /// <summary>
    /// Stops any running wallpaper slideshow.
    /// </summary>
    public void StopWallpaperSlideshow() {
        _monitorService.StopWallpaperSlideshow();
    }

    /// <summary>
    /// Advances the wallpaper slideshow in the specified direction.
    /// </summary>
    /// <param name="direction">Direction to advance.</param>
    public void AdvanceWallpaperSlide(DesktopSlideshowDirection direction) {
        _monitorService.AdvanceWallpaperSlide(direction);
    }

    /// <summary>
    /// Gets the current wallpaper slideshow configuration and state.
    /// </summary>
    /// <returns>The current wallpaper slideshow details.</returns>
    public DesktopWallpaperSlideshow GetWallpaperSlideshow() {
        return _monitorService.GetWallpaperSlideshow();
    }

    /// <summary>
    /// Sets the wallpaper slideshow options.
    /// </summary>
    /// <param name="options">Slideshow options.</param>
    /// <param name="slideshowTick">Slideshow tick interval in milliseconds.</param>
    public void SetWallpaperSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick) {
        _monitorService.SetWallpaperSlideshowOptions(options, slideshowTick);
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
    /// Gets the Advanced Color and HDR state of a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <returns>The Advanced Color and HDR state.</returns>
    public MonitorAdvancedColorInfo GetMonitorAdvancedColor(string deviceId) {
        return _monitorService.GetMonitorAdvancedColor(deviceId);
    }

    /// <summary>
    /// Enables or disables HDR for a monitor by its device ID.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="enabled">Whether HDR should be enabled.</param>
    public void SetMonitorHdr(string deviceId, bool enabled) {
        _monitorService.SetMonitorHdr(deviceId, enabled);
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
