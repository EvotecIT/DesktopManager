using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Microsoft.Win32;

namespace DesktopManager;

public partial class MonitorService {
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

    private static string WriteStreamToTempFile(Stream stream) {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try {
            using FileStream fs = File.Create(path);
            stream.CopyTo(fs);
            return path;
        } catch {
            DeleteTempFile(path);
            throw;
        }
    }

    private static void DeleteTempFile(string path) {
        try {
            File.Delete(path);
        } catch (Exception ex) {
            Console.WriteLine($"DeleteTempFile failed: {ex.Message}");
        }
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
        } catch (Exception ex) {
            Console.WriteLine($"GetWallpaperPositionFallback failed: {ex.Message}");
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
        } catch (Exception ex) {
            Console.WriteLine($"SetWallpaperPositionFallback failed: {ex.Message}");
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
        } catch (Exception ex) {
            Console.WriteLine($"GetBackgroundColorFallback failed: {ex.Message}");
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
        } catch (Exception ex) {
            Console.WriteLine($"SetBackgroundColorFallback failed: {ex.Message}");
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
        throw new ArgumentException($"Monitor with device ID '{deviceId}' not found");
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

        throw new ArgumentException($"Corresponding display device not found for monitor ID '{deviceId}'.");
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

    private PHYSICAL_MONITOR[] GetPhysicalMonitors(string deviceId) {
        IntPtr found = IntPtr.Zero;
        MonitorNativeMethods.MonitorEnumProc proc = (IntPtr h, IntPtr hdc, ref RECT r, IntPtr data) => {
            MONITORINFOEX info = new MONITORINFOEX();
            info.cbSize = Marshal.SizeOf<MONITORINFOEX>();
            if (MonitorNativeMethods.GetMonitorInfo(h, ref info) && info.szDevice == deviceId) {
                found = h;
                return false;
            }
            return true;
        };
        if (!MonitorNativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero)) {
            Console.WriteLine("EnumDisplayMonitors failed");
            return Array.Empty<PHYSICAL_MONITOR>();
        }
        if (found == IntPtr.Zero) {
            return Array.Empty<PHYSICAL_MONITOR>();
        }
        if (!MonitorNativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(found, out uint count) || count == 0) {
            return Array.Empty<PHYSICAL_MONITOR>();
        }
        PHYSICAL_MONITOR[] monitors = new PHYSICAL_MONITOR[count];
        if (!MonitorNativeMethods.GetPhysicalMonitorsFromHMONITOR(found, count, monitors)) {
            return Array.Empty<PHYSICAL_MONITOR>();
        }
        return monitors;
    }

    /// <summary>
    /// Gets the current brightness of a monitor.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <returns>The current brightness level.</returns>
    public int GetMonitorBrightness(string deviceId) {
        var monitors = GetPhysicalMonitors(deviceId);
        if (monitors.Length == 0) {
            throw new InvalidOperationException("Monitor handle not found");
        }
        try {
            if (MonitorNativeMethods.GetMonitorBrightness(monitors[0].hPhysicalMonitor, out uint min, out uint cur, out uint _)) {
                return (int)cur;
            }
            throw new InvalidOperationException("GetMonitorBrightness failed");
        } finally {
            if (!MonitorNativeMethods.DestroyPhysicalMonitors((uint)monitors.Length, monitors)) {
                Console.WriteLine("DestroyPhysicalMonitors failed");
            }
        }
    }

    /// <summary>
    /// Sets the brightness of a monitor.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="brightness">Brightness value to set.</param>
    public void SetMonitorBrightness(string deviceId, int brightness) {
        var monitors = GetPhysicalMonitors(deviceId);
        if (monitors.Length == 0) {
            throw new InvalidOperationException("Monitor handle not found");
        }
        try {
            if (!MonitorNativeMethods.SetMonitorBrightness(monitors[0].hPhysicalMonitor, (uint)brightness)) {
                throw new InvalidOperationException("SetMonitorBrightness failed");
            }
        } finally {
            if (!MonitorNativeMethods.DestroyPhysicalMonitors((uint)monitors.Length, monitors)) {
                Console.WriteLine("DestroyPhysicalMonitors failed");
            }
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

        MonitorNativeMethods.IObjectCollection? collection = null;
        try {
            collection = (MonitorNativeMethods.IObjectCollection)Activator.CreateInstance(Type.GetTypeFromCLSID(clsidEnum));
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
        } finally {
            if (collection != null) {
                Marshal.ReleaseComObject(collection);
            }
        }
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

