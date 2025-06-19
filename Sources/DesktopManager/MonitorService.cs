using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>
/// Service for managing monitors and their settings.
/// </summary>
public class MonitorService {
    private const int ENUM_CURRENT_SETTINGS = -1;
    private readonly IDesktopManager _desktopManager;

    private void Execute(Action action, string operation) {
        try {
            action();
        } catch (COMException ex) {
            throw new DesktopManagerException(operation, ex);
        }
    }

    private T Execute<T>(Func<T> func, string operation) {
        try {
            return func();
        } catch (COMException ex) {
            throw new DesktopManagerException(operation, ex);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorService"/> class.
    /// </summary>
    /// <param name="desktopManager">The desktop manager interface.</param>
    public MonitorService(IDesktopManager desktopManager) {
        _desktopManager = desktopManager;

        try {
            Execute(() => _desktopManager.Enable(), nameof(IDesktopManager.Enable));
        } catch (DesktopManagerException) {
            // COM failures are ignored during initialization to support unsupported scenarios
        }
    }

    /// <summary>
    /// Gets the list of all monitors.
    /// </summary>
    /// <returns>A list of <see cref="Monitor"/> objects.</returns>
    public List<Monitor> GetMonitors() {
        try {
            List<Monitor> list = new List<Monitor>();

            var count = Execute(() => _desktopManager.GetMonitorDevicePathCount(), nameof(IDesktopManager.GetMonitorDevicePathCount));
            for (uint i = 0; i < count; i++) {
                var monitor = new Monitor(this) {
                    Index = (int)i,
                    DeviceId = Execute(() => _desktopManager.GetMonitorDevicePathAt(i), nameof(IDesktopManager.GetMonitorDevicePathAt))
                };
                if (monitor.DeviceId != "") {
                    monitor.WallpaperPosition = Execute(() => _desktopManager.GetPosition(), nameof(IDesktopManager.GetPosition));
                    monitor.Wallpaper = Execute(() => _desktopManager.GetWallpaper(monitor.DeviceId), nameof(IDesktopManager.GetWallpaper));
                    monitor.Rect = Execute(() => _desktopManager.GetMonitorBounds(monitor.DeviceId), nameof(IDesktopManager.GetMonitorBounds));

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
        } catch (DesktopManagerException) {
            return EnumerateMonitorsFallback();
        } catch (COMException) {
            return EnumerateMonitorsFallback();
        }
    }

    private List<Monitor> EnumerateMonitorsFallback() {
        List<Monitor> monitors = new List<Monitor>();
        int index = 0;
        MonitorNativeMethods.MonitorEnumProc proc = (IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr lparam) => {
            MONITORINFOEX info = new MONITORINFOEX();
            info.cbSize = Marshal.SizeOf<MONITORINFOEX>();
            if (MonitorNativeMethods.GetMonitorInfo(hMonitor, ref info)) {
                DISPLAY_DEVICE device = new DISPLAY_DEVICE();
                device.cb = Marshal.SizeOf(device);
                if (MonitorNativeMethods.EnumDisplayDevices(info.szDevice, 0, ref device, 0)) {
                    var monitor = new Monitor(this) {
                        Index = index,
                        DeviceId = info.szDevice,
                        DeviceName = device.DeviceName,
                        DeviceString = device.DeviceString,
                        StateFlags = device.StateFlags,
                        DeviceKey = device.DeviceKey,
                        Rect = info.rcMonitor
                    };
                    monitors.Add(monitor);
                }
            }
            index++;
            return true;
        };
        MonitorNativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
        return monitors;
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
        try {
            Execute(() => _desktopManager.SetWallpaper(monitorId, wallpaperPath), nameof(IDesktopManager.SetWallpaper));
        } catch (DesktopManagerException) {
            SetSystemWallpaper(wallpaperPath);
        } catch (COMException) {
            SetSystemWallpaper(wallpaperPath);
        }
    }

    /// <summary>
    /// Sets the wallpaper for a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(int index, string wallpaperPath) {
        try {
            var monitorId = Execute(() => _desktopManager.GetMonitorDevicePathAt((uint)index), nameof(IDesktopManager.GetMonitorDevicePathAt));
            Execute(() => _desktopManager.SetWallpaper(monitorId, wallpaperPath), nameof(IDesktopManager.SetWallpaper));
        } catch (DesktopManagerException) {
            SetSystemWallpaper(wallpaperPath);
        } catch (COMException) {
            SetSystemWallpaper(wallpaperPath);
        }
    }

    /// <summary>
    /// Sets the wallpaper for all connected monitors.
    /// </summary>
    /// <param name="wallpaperPath">The path to the wallpaper image.</param>
    public void SetWallpaper(string wallpaperPath) {
        try {
            var devicePathCount = GetMonitorsConnected();
            foreach (var device in devicePathCount) {
                Execute(() => _desktopManager.SetWallpaper(device.DeviceId, wallpaperPath), nameof(IDesktopManager.SetWallpaper));
            }
        } catch (DesktopManagerException) {
            SetSystemWallpaper(wallpaperPath);
        } catch (COMException) {
            SetSystemWallpaper(wallpaperPath);
        }
    }

    /// <summary>
    /// Gets the wallpaper for a specific monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <returns>The path to the wallpaper image.</returns>
    public string GetWallpaper(string monitorId) {
        try {
            return Execute(() => _desktopManager.GetWallpaper(monitorId), nameof(IDesktopManager.GetWallpaper));
        } catch (DesktopManagerException) {
            return GetSystemWallpaper();
        } catch (COMException) {
            return GetSystemWallpaper();
        }
    }

    /// <summary>
    /// Gets the wallpaper for a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <returns>The path to the wallpaper image.</returns>
    public string GetWallpaper(int index) {
        try {
            var monitorId = Execute(() => _desktopManager.GetMonitorDevicePathAt((uint)index), nameof(IDesktopManager.GetMonitorDevicePathAt));
            return Execute(() => _desktopManager.GetWallpaper(monitorId), nameof(IDesktopManager.GetWallpaper));
        } catch (DesktopManagerException) {
            return GetSystemWallpaper();
        } catch (COMException) {
            return GetSystemWallpaper();
        }
    }

    /// <summary>
    /// Gets the device path of a monitor by index.
    /// </summary>
    /// <param name="index">The index of the monitor.</param>
    /// <returns>The device path of the monitor.</returns>
    public string GetMonitorDevicePathAt(uint index) {
        return Execute(() => _desktopManager.GetMonitorDevicePathAt(index), nameof(IDesktopManager.GetMonitorDevicePathAt));
    }

    /// <summary>
    /// Gets the desktop background color.
    /// </summary>
    /// <returns>The background color as RGB value.</returns>
    public uint GetBackgroundColor() {
        try {
            return Execute(() => _desktopManager.GetBackgroundColor(), nameof(IDesktopManager.GetBackgroundColor));
        } catch (DesktopManagerException) {
            return GetBackgroundColorFallback();
        } catch (COMException) {
            return GetBackgroundColorFallback();
        }
    }

    /// <summary>
    /// Sets the desktop background color.
    /// </summary>
    /// <param name="color">Color as RGB value.</param>
    public void SetBackgroundColor(uint color) {
        try {
            Execute(() => _desktopManager.SetBackgroundColor(color), nameof(IDesktopManager.SetBackgroundColor));
        } catch (DesktopManagerException) {
            SetBackgroundColorFallback(color);
        } catch (COMException) {
            SetBackgroundColorFallback(color);
        }
    }

    /// <summary>
    /// Gets the wallpaper position.
    /// </summary>
    /// <returns>The wallpaper position.</returns>
    public DesktopWallpaperPosition GetWallpaperPosition() {
        try {
            return Execute(() => _desktopManager.GetPosition(), nameof(IDesktopManager.GetPosition));
        } catch (DesktopManagerException) {
            return GetWallpaperPositionFallback();
        } catch (COMException) {
            return GetWallpaperPositionFallback();
        }
    }

    /// <summary>
    /// Sets the wallpaper position.
    /// </summary>
    /// <param name="position">The wallpaper position.</param>
    public void SetWallpaperPosition(DesktopWallpaperPosition position) {
        try {
            Execute(() => _desktopManager.SetPosition(position), nameof(IDesktopManager.SetPosition));
        } catch (DesktopManagerException) {
            SetWallpaperPositionFallback(position);
        } catch (COMException) {
            SetWallpaperPositionFallback(position);
        }
    }

    /// <summary>
    /// Gets the bounds of a monitor.
    /// </summary>
    /// <param name="monitorId">The monitor ID.</param>
    /// <returns>The bounds of the monitor.</returns>
    public RECT GetMonitorBounds(string monitorId) {
        try {
            return Execute(() => _desktopManager.GetMonitorBounds(monitorId), nameof(IDesktopManager.GetMonitorBounds));
        } catch (DesktopManagerException) {
            return GetMonitorBoundsFallback(monitorId);
        } catch (COMException) {
            return GetMonitorBoundsFallback(monitorId);
        }
    }

    private RECT GetMonitorBoundsFallback(string deviceName) {
        RECT rect = new RECT();
        DEVMODE mode = new DEVMODE();
        mode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
        if (MonitorNativeMethods.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref mode)) {
            rect.Left = mode.dmPositionX;
            rect.Top = mode.dmPositionY;
            rect.Right = mode.dmPositionX + mode.dmPelsWidth;
            rect.Bottom = mode.dmPositionY + mode.dmPelsHeight;
        }
        return rect;
    }

    private void SetSystemWallpaper(string path) {
        MonitorNativeMethods.SystemParametersInfo(MonitorNativeMethods.SPI_SETDESKWALLPAPER, 0, path, MonitorNativeMethods.SPIF_UPDATEINIFILE | MonitorNativeMethods.SPIF_SENDWININICHANGE);
    }

    private string GetSystemWallpaper() {
        StringBuilder sb = new StringBuilder(260);
        if (MonitorNativeMethods.SystemParametersInfo(MonitorNativeMethods.SPI_GETDESKWALLPAPER, (uint)sb.Capacity, sb, 0)) {
            return sb.ToString();
        }
        return string.Empty;
    }

    private DesktopWallpaperPosition GetWallpaperPositionFallback() {
        try {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\\Desktop", false);
            if (key != null) {
                string style = key.GetValue("WallpaperStyle", "0")?.ToString() ?? "0";
                string tile = key.GetValue("TileWallpaper", "0")?.ToString() ?? "0";
                if (tile == "1") {
                    return DesktopWallpaperPosition.Tile;
                }
                return style switch {
                    "0" => DesktopWallpaperPosition.Center,
                    "2" => DesktopWallpaperPosition.Stretch,
                    "6" => DesktopWallpaperPosition.Fit,
                    "10" => DesktopWallpaperPosition.Fill,
                    "22" => DesktopWallpaperPosition.Span,
                    _ => DesktopWallpaperPosition.Center
                };
            }
        } catch {
        }
        return DesktopWallpaperPosition.Center;
    }

    private void SetWallpaperPositionFallback(DesktopWallpaperPosition position) {
        try {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\\Desktop", true);
            if (key != null) {
                switch (position) {
                    case DesktopWallpaperPosition.Tile:
                        key.SetValue("WallpaperStyle", "0");
                        key.SetValue("TileWallpaper", "1");
                        break;
                    case DesktopWallpaperPosition.Center:
                        key.SetValue("WallpaperStyle", "0");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case DesktopWallpaperPosition.Stretch:
                        key.SetValue("WallpaperStyle", "2");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case DesktopWallpaperPosition.Fit:
                        key.SetValue("WallpaperStyle", "6");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case DesktopWallpaperPosition.Fill:
                        key.SetValue("WallpaperStyle", "10");
                        key.SetValue("TileWallpaper", "0");
                        break;
                    case DesktopWallpaperPosition.Span:
                        key.SetValue("WallpaperStyle", "22");
                        key.SetValue("TileWallpaper", "0");
                        break;
                }
                SetSystemWallpaper(GetSystemWallpaper());
            }
        } catch {
        }
    }

    private uint GetBackgroundColorFallback() {
        try {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\\Colors", false);
            if (key != null) {
                string value = key.GetValue("Background")?.ToString();
                if (!string.IsNullOrEmpty(value)) {
                    var parts = value.Split(' ');
                    if (parts.Length == 3 &&
                        byte.TryParse(parts[0], out var r) &&
                        byte.TryParse(parts[1], out var g) &&
                        byte.TryParse(parts[2], out var b)) {
                        return (uint)(r | (g << 8) | (b << 16));
                    }
                }
            }
        } catch {
        }
        return 0;
    }

    private void SetBackgroundColorFallback(uint color) {
        try {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\\Colors", true);
            if (key != null) {
                byte r = (byte)(color & 0xFF);
                byte g = (byte)((color >> 8) & 0xFF);
                byte b = (byte)((color >> 16) & 0xFF);
                key.SetValue("Background", $"{r} {g} {b}");
            }
        } catch {
        }
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
    /// Sets the resolution of a monitor.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="width">The desired width.</param>
    /// <param name="height">The desired height.</param>
    public void SetMonitorResolution(string deviceId, int width, int height) {
        var deviceName = GetMonitors().First(m => m.DeviceId == deviceId).DeviceName;

        DEVMODE devMode = new DEVMODE();
        devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        if (!MonitorNativeMethods.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref devMode)) {
            throw new InvalidOperationException("Unable to get display settings");
        }

        devMode.dmFields = 0x00080000 | 0x00100000; // DM_PELSWIDTH | DM_PELSHEIGHT
        devMode.dmPelsWidth = width;
        devMode.dmPelsHeight = height;

        DisplayChangeConfirmation result = MonitorNativeMethods.ChangeDisplaySettingsEx(deviceName, ref devMode, IntPtr.Zero, ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero);
        if (result != DisplayChangeConfirmation.Successful && result != DisplayChangeConfirmation.Restart) {
            throw new InvalidOperationException($"Unable to set monitor resolution. Error: {result}");
        }
    }

    /// <summary>
    /// Sets the orientation of a monitor.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="orientation">The orientation to apply.</param>
    public void SetMonitorOrientation(string deviceId, DisplayOrientation orientation) {
        var deviceName = GetMonitors().First(m => m.DeviceId == deviceId).DeviceName;

        DEVMODE devMode = new DEVMODE();
        devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        if (!MonitorNativeMethods.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref devMode)) {
            throw new InvalidOperationException("Unable to get display settings");
        }

        if ((orientation == DisplayOrientation.Degrees90 || orientation == DisplayOrientation.Degrees270) &&
            (devMode.dmDisplayOrientation == (int)DisplayOrientation.Default || devMode.dmDisplayOrientation == (int)DisplayOrientation.Degrees180) ||
            (orientation == DisplayOrientation.Default || orientation == DisplayOrientation.Degrees180) &&
            (devMode.dmDisplayOrientation == (int)DisplayOrientation.Degrees90 || devMode.dmDisplayOrientation == (int)DisplayOrientation.Degrees270)) {
            int temp = devMode.dmPelsWidth;
            devMode.dmPelsWidth = devMode.dmPelsHeight;
            devMode.dmPelsHeight = temp;
            devMode.dmFields = 0x00080000 | 0x00100000 | 0x00000080;
        } else {
            devMode.dmFields = 0x00000080;
        }

        devMode.dmDisplayOrientation = (int)orientation;

        DisplayChangeConfirmation result = MonitorNativeMethods.ChangeDisplaySettingsEx(deviceName, ref devMode, IntPtr.Zero, ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero);
        if (result != DisplayChangeConfirmation.Successful && result != DisplayChangeConfirmation.Restart) {
            throw new InvalidOperationException($"Unable to set monitor orientation. Error: {result}");
        }
    }

    /// <summary>
    /// Starts a wallpaper slideshow using the provided images.
    /// </summary>
    /// <param name="wallpaperPaths">Collection of wallpaper file paths.</param>
    public void StartWallpaperSlideshow(IEnumerable<string> wallpaperPaths) {
        if (wallpaperPaths == null) {
            throw new ArgumentNullException(nameof(wallpaperPaths));
        }

        IntPtr arrayPtr = IntPtr.Zero;
        try {
            arrayPtr = CreateShellItemArray(wallpaperPaths);
            Execute(() => _desktopManager.SetSlideshow(arrayPtr), nameof(IDesktopManager.SetSlideshow));
        } finally {
            if (arrayPtr != IntPtr.Zero) {
                Marshal.Release(arrayPtr);
            }
        }
    }

    /// <summary>
    /// Stops the currently running wallpaper slideshow.
    /// </summary>
    public void StopWallpaperSlideshow() {
        Execute(() => _desktopManager.SetSlideshow(IntPtr.Zero), nameof(IDesktopManager.SetSlideshow));
    }

    /// <summary>
    /// Advances the slideshow in the given direction.
    /// </summary>
    /// <param name="direction">Direction to advance.</param>
    public void AdvanceWallpaperSlide(DesktopSlideshowDirection direction) {
        Execute(() => _desktopManager.AdvanceSlideshow(null, direction), nameof(IDesktopManager.AdvanceSlideshow));
    }

    private static IntPtr CreateShellItemArray(IEnumerable<string> paths) {
        Guid clsidEnum = new("2d3468c1-36a7-43b6-ac24-d3f02fd9607a");
        Guid iidShellItem = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");
        Guid iidShellItemArray = new("b63ea76d-1f85-456f-a19c-48159efa858b");

        var collection = (MonitorNativeMethods.IObjectCollection)Activator.CreateInstance(Type.GetTypeFromCLSID(clsidEnum));
        foreach (var path in paths) {
            if (string.IsNullOrEmpty(path)) continue;
            int hr = MonitorNativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iidShellItem, out IntPtr item);
            if (hr != 0) {
                Marshal.ThrowExceptionForHR(hr);
            }
            object obj = Marshal.GetObjectForIUnknown(item);
            collection.AddObject(obj);
            Marshal.Release(item);
        }

        IntPtr unk = Marshal.GetIUnknownForObject(collection);
        Marshal.QueryInterface(unk, ref iidShellItemArray, out IntPtr arrayPtr);
        Marshal.Release(unk);
        return arrayPtr;
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
