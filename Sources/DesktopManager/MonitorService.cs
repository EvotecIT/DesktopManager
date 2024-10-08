namespace DesktopManager;

/// <summary>
/// Service for managing monitors and their settings.
/// </summary>
public class MonitorService {
    private const int ENUM_CURRENT_SETTINGS = -1;
    private readonly IDesktopManager _desktopManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorService"/> class.
    /// </summary>
    /// <param name="desktopManager">The desktop manager interface.</param>
    public MonitorService(IDesktopManager desktopManager) {
        _desktopManager = desktopManager;
    }

    /// <summary>
    /// Gets the list of all monitors.
    /// </summary>
    /// <returns>A list of <see cref="Monitor"/> objects.</returns>
    public List<Monitor> GetMonitors() {
        List<Monitor> list = new List<Monitor>();

        var count = _desktopManager.GetMonitorDevicePathCount();
        for (uint i = 0; i < count; i++) {
            var monitor = new Monitor(this) {
                Index = (int)i,
                DeviceId = _desktopManager.GetMonitorDevicePathAt(i)
            };
            if (monitor.DeviceId != "") {
                monitor.WallpaperPosition = _desktopManager.GetPosition();
                monitor.Wallpaper = _desktopManager.GetWallpaper(monitor.DeviceId);
                monitor.Rect = _desktopManager.GetMonitorBounds(monitor.DeviceId);

                // Populate new properties
                DISPLAY_DEVICE d = new DISPLAY_DEVICE();
                d.cb = Marshal.SizeOf(d);
                if (MonitorNativeMethods.EnumDisplayDevices(null, i, ref d, 0)) {
                    monitor.DeviceName = d.DeviceName;
                    monitor.DeviceString = d.DeviceString;
                    monitor.StateFlags = d.StateFlags;
                    monitor.DeviceKey = d.DeviceKey;
                }
            }
            list.Add(monitor);
        }

        return list;
    }

    /// <summary>
    /// Gets the list of connected monitors.
    /// </summary>
    /// <returns>A list of connected <see cref="Monitor"/> objects.</returns>
    public List<Monitor> GetMonitorsConnected() {
        List<Monitor> list = new List<Monitor>();
        foreach (var monitor in GetMonitors()) {
            if (monitor.DeviceId != "") {
                list.Add(monitor);
            }
        }
        return list;
    }

    /// <summary>
    /// Sets the wallpaper for a specific monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(string monitorId, string wallpaperPath) {
        _desktopManager.SetWallpaper(monitorId, wallpaperPath);
    }

    /// <summary>
    /// Sets the wallpaper for a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(int index, string wallpaperPath) {
        var monitorId = _desktopManager.GetMonitorDevicePathAt((uint)index);
        _desktopManager.SetWallpaper(monitorId, wallpaperPath);
    }

    /// <summary>
    /// Sets the wallpaper for all connected monitors.
    /// </summary>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(string wallpaperPath) {
        var devicePathCount = GetMonitorsConnected();
        foreach (var device in devicePathCount) {
            _desktopManager.SetWallpaper(device.DeviceId, wallpaperPath);
        }
    }

    /// <summary>
    /// Gets the wallpaper for a specific monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <returns>The path to the wallpaper image.</returns>
    public string GetWallpaper(string monitorId) {
        return _desktopManager.GetWallpaper(monitorId);
    }

    /// <summary>
    /// Gets the wallpaper for a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <returns>The path to the wallpaper image.</returns>
    public string GetWallpaper(int index) {
        return _desktopManager.GetWallpaper(_desktopManager.GetMonitorDevicePathAt((uint)index));
    }

    /// <summary>
    /// Gets the device path of a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <returns>The device path of the monitor.</returns>
    public string GetMonitorDevicePathAt(uint index) {
        return _desktopManager.GetMonitorDevicePathAt(index);
    }

    /// <summary>
    /// Gets the wallpaper position.
    /// </summary>
    /// <returns>The wallpaper position.</returns>
    public DesktopWallpaperPosition GetWallpaperPosition() {
        return _desktopManager.GetPosition();
    }

    /// <summary>
    /// Sets the wallpaper position.
    /// </summary>
    /// <param name="position">The wallpaper position.</param>
    public void SetWallpaperPosition(DesktopWallpaperPosition position) {
        _desktopManager.SetPosition(position);
    }

    /// <summary>
    /// Gets the bounds of a monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <returns>The bounds of the monitor.</returns>
    public RECT GetMonitorBounds(string monitorId) {
        return _desktopManager.GetMonitorBounds(monitorId);
    }

    /// <summary>
    /// Gets the position of a monitor.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <returns>The position of the monitor.</returns>
    /// <exception cref="ArgumentException">Thrown when the monitor is not found.</exception>
    public MonitorPosition GetMonitorPosition(string deviceId) {
        var monitors = GetMonitors();
        foreach (var monitor in monitors) {
            if (monitor.DeviceId == deviceId) {
                return new MonitorPosition(monitor.Rect.Left, monitor.Rect.Top, monitor.Rect.Right, monitor.Rect.Bottom);
            }
        }
        throw new ArgumentException("Monitor not found");
    }

    /// <summary>
    /// Sets the position of a monitor.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="position">The new position of the monitor.</param>
    public void SetMonitorPosition(string deviceId, MonitorPosition position) {
        SetMonitorPosition(deviceId, position.Left, position.Top, position.Right, position.Bottom);
    }

    /// <summary>
    /// Sets the position of a monitor.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="left">The left position.</param>
    /// <param name="top">The top position.</param>
    /// <param name="right">The right position.</param>
    /// <param name="bottom">The bottom position.</param>
    /// <exception cref="InvalidOperationException">Thrown when unable to set monitor position.</exception>
    /// <exception cref="ArgumentException">Thrown when the corresponding display device is not found.</exception>
    public void SetMonitorPosition(string deviceId, int left, int top, int right, int bottom) {
        var monitorRect = GetMonitorBounds(deviceId);

        // Enumerate through all display devices and match by RECT
        DISPLAY_DEVICE d = new DISPLAY_DEVICE();
        d.cb = Marshal.SizeOf(d);
        int deviceNum = 0;

        while (MonitorNativeMethods.EnumDisplayDevices(null, (uint)deviceNum, ref d, 0)) {
            if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0) {
                DEVMODE devMode = new DEVMODE();
                devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                if (MonitorNativeMethods.EnumDisplaySettings(d.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode)) {
                    // Compare RECTs
                    if (monitorRect.Left == devMode.dmPositionX &&
                        monitorRect.Top == devMode.dmPositionY &&
                        (monitorRect.Right - monitorRect.Left) == devMode.dmPelsWidth &&
                        (monitorRect.Bottom - monitorRect.Top) == devMode.dmPelsHeight) {
                        // Found a match, now set the position
                        devMode.dmFields = 0x00000020; // DM_POSITION
                        devMode.dmPositionX = left;
                        devMode.dmPositionY = top;

                        // Apply the changes directly
                        DisplayChangeConfirmation result = MonitorNativeMethods.ChangeDisplaySettingsEx(d.DeviceName, ref devMode, IntPtr.Zero, 0, IntPtr.Zero);
                        if (result != DisplayChangeConfirmation.Successful) {
                            Console.WriteLine($"ChangeDisplaySettingsEx failed with error code: {result}");
                            throw new InvalidOperationException("Unable to set monitor position");
                        }

                        Console.WriteLine($"Monitor position set successfully for {d.DeviceName}");
                        return;
                    }
                }
            }

            deviceNum++;
        }

        throw new ArgumentException("Corresponding display device not found for the given Monitor ID.");
    }

    /// <summary>
    /// Gets all display devices.
    /// </summary>
    /// <returns>A list of all <see cref="DISPLAY_DEVICE"/> objects.</returns>
    public List<DISPLAY_DEVICE> DisplayDevicesAll() {
        List<DISPLAY_DEVICE> devices = new List<DISPLAY_DEVICE>();

        DISPLAY_DEVICE d = new DISPLAY_DEVICE();
        d.cb = Marshal.SizeOf(d);

        int deviceNum = 0;
        while (MonitorNativeMethods.EnumDisplayDevices(null, (uint)deviceNum, ref d, 0)) {
            Console.WriteLine($"Device Name: {d.DeviceName}");
            Console.WriteLine($"Device String: {d.DeviceString}");
            Console.WriteLine($"State Flags: {d.StateFlags}");
            Console.WriteLine($"Device ID: {d.DeviceID}");
            Console.WriteLine($"Device Key: {d.DeviceKey}");
            Console.WriteLine();
            deviceNum++;
            devices.Add(d);
        }
        return devices;
    }

    /// <summary>
    /// Gets all connected display devices.
    /// </summary>
    /// <returns>A list of connected <see cref="DISPLAY_DEVICE"/> objects.</returns>
    public List<DISPLAY_DEVICE> DisplayDevicesConnected() {
        List<DISPLAY_DEVICE> devices = new List<DISPLAY_DEVICE>();
        DISPLAY_DEVICE device = new DISPLAY_DEVICE();
        device.cb = Marshal.SizeOf(device);

        uint deviceNum = 0;
        while (MonitorNativeMethods.EnumDisplayDevices(null, deviceNum, ref device, 0)) {
            if ((device.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0) {
                devices.Add(device);
            }
            deviceNum++;
        }

        return devices;
    }
}
