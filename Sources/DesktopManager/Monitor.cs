using System;
using Microsoft.Win32;

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
    public string DeviceString { get; internal set; } = string.Empty;

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
    public string DeviceId { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets or sets the wallpaper of the monitor.
    /// </summary>
    public string Wallpaper { get; internal set; } = string.Empty;

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
    public string DeviceName { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state flags of the monitor.
    /// </summary>
    public DisplayDeviceStateFlags StateFlags { get; internal set; }

    /// <summary>
    /// Gets or sets the device key of the monitor.
    /// </summary>
    public string DeviceKey { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the manufacturer ID parsed from EDID if available.
    /// </summary>
    public string? Manufacturer { get; private set; }

    /// <summary>
    /// Gets the serial number parsed from EDID if available.
    /// </summary>
    public string? SerialNumber { get; private set; }

    /// <summary>
    /// Gets or sets the rectangle that defines the bounds of the monitor.
    /// </summary>
    internal RECT Rect { get; set; }

    /// <summary>
    /// Gets or sets the desktop work area of the monitor, excluding taskbars and app bars.
    /// </summary>
    internal RECT WorkArea { get; set; }

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
        if (string.IsNullOrWhiteSpace(DeviceId)) {
            return new MonitorPosition(Rect.Left, Rect.Top, Rect.Right, Rect.Bottom);
        }

        return _monitorService.GetMonitorPosition(DeviceId);
    }

    /// <summary>
    /// Gets the Advanced Color and HDR state of the monitor.
    /// </summary>
    /// <returns>The Advanced Color and HDR state of the monitor.</returns>
    public MonitorAdvancedColorInfo GetAdvancedColor() {
        return _monitorService.GetMonitorAdvancedColor(DeviceId);
    }

    /// <summary>
    /// Enables or disables HDR for the monitor.
    /// </summary>
    /// <param name="enabled">Whether HDR should be enabled.</param>
    public void SetHdr(bool enabled) {
        _monitorService.SetMonitorHdr(DeviceId, enabled);
    }

    /// <summary>
    /// Gets the bounds of the monitor.
    /// </summary>
    /// <returns>The bounds of the monitor.</returns>
    internal RECT GetMonitorBounds() {
        return _monitorService.GetMonitorBounds(DeviceId);
    }

    /// <summary>
    /// Gets the usable desktop work area of the monitor, falling back to the monitor bounds when unavailable.
    /// </summary>
    /// <returns>The work area of the monitor.</returns>
    internal RECT GetMonitorWorkArea() {
        if (IsNonEmpty(WorkArea)) {
            return WorkArea;
        }

        if (IsNonEmpty(Rect)) {
            return Rect;
        }

        return GetMonitorBounds();
    }

    private static bool IsNonEmpty(RECT rect) {
        return rect.Right > rect.Left && rect.Bottom > rect.Top;
    }

    /// <summary>
    /// Reads EDID information from the registry and populates manufacturer and serial.
    /// </summary>
    internal void LoadEdidInfo() {
        const string prefix = "\\Registry\\Machine\\";
        try {
            if (string.IsNullOrEmpty(DeviceKey) || !DeviceKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            string path = DeviceKey.Substring(prefix.Length);
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(path + "\\Device Parameters");
            if (key?.GetValue("EDID") is byte[] edid && edid.Length >= 16) {
                Manufacturer = ParseManufacturerFromEdid(edid);
                SerialNumber = ParseSerialNumberFromEdid(edid);
            }
        } catch (Exception ex) {
            Console.WriteLine($"LoadEdidInfo failed: {ex.Message}");
        }
    }

    internal static string ParseManufacturerFromEdid(byte[] edid) {
        int code = (edid[8] << 8) | edid[9];
        char m1 = (char)(((code >> 10) & 0x1F) + 0x40);
        char m2 = (char)(((code >> 5) & 0x1F) + 0x40);
        char m3 = (char)((code & 0x1F) + 0x40);
        return new string(new[] { m1, m2, m3 });
    }

    internal static string ParseSerialNumberFromEdid(byte[] edid) {
        uint serial = BitConverter.ToUInt32(edid, 12);
        return serial != 0 ? serial.ToString() : string.Empty;
    }
}
