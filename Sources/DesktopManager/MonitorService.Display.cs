using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>
/// Provides methods for managing display-related monitor settings.
/// </summary>
public partial class MonitorService {
    private readonly List<PHYSICAL_MONITOR> _monitorHandles = new();
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
        if (string.IsNullOrWhiteSpace(monitorId)) {
            throw new ArgumentNullException(nameof(monitorId));
        }
        try {
            return Execute(() => _desktopManager.GetMonitorBounds(monitorId), nameof(IDesktopManager.GetMonitorBounds));
        } catch (DesktopManagerException) {
            return GetMonitorBoundsFallback(monitorId);
        } catch (COMException) {
            return GetMonitorBoundsFallback(monitorId);
        } catch (ArgumentException) {
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

    /// <summary>
    /// Sets the system wallpaper path using <c>SystemParametersInfo</c>.
    /// </summary>
    /// <param name="path">Path to the wallpaper image.</param>
    internal virtual void SetSystemWallpaper(string path) {
        if (string.IsNullOrEmpty(path)) {
            throw new ArgumentNullException(nameof(path));
        }

        MonitorNativeMethods.SystemParametersInfo(
            MonitorNativeMethods.SPI_SETDESKWALLPAPER,
            0,
            path,
            MonitorNativeMethods.SPIF_UPDATEINIFILE | MonitorNativeMethods.SPIF_SENDWININICHANGE);
    }

    /// <summary>
    /// Gets the current system wallpaper path.
    /// </summary>
    /// <returns>The wallpaper path if available; otherwise an empty string.</returns>
    private string GetSystemWallpaper() {
        StringBuilder sb = new StringBuilder(MonitorNativeMethods.MAX_PATH);
        if (MonitorNativeMethods.SystemParametersInfo(MonitorNativeMethods.SPI_GETDESKWALLPAPER, (uint)sb.Capacity, sb, 0)) {
            return sb.ToString();
        }
        return string.Empty;
    }


    private DesktopWallpaperPosition GetWallpaperPositionFallback() {
        try {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\\Desktop", false);
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
            DesktopManagerDiagnostics.Report($"GetWallpaperPositionFallback failed: {ex.Message}");
        }
        return DesktopWallpaperPosition.Center;
    }

    private void SetWallpaperPositionFallback(DesktopWallpaperPosition position) {
        try {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\\Desktop", true);
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
            DesktopManagerDiagnostics.Report($"SetWallpaperPositionFallback failed: {ex.Message}");
        }
    }

    private uint GetBackgroundColorFallback() {
        try {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\\Colors", false);
            if (key != null) {
                string? value = key.GetValue("Background")?.ToString();
                if (!string.IsNullOrEmpty(value)) {
                    string[] parts = value!.Split(' ');
                    if (parts.Length == 3 &&
                        byte.TryParse(parts[0], out var r) &&
                        byte.TryParse(parts[1], out var g) &&
                        byte.TryParse(parts[2], out var b)) {
                        return (uint)(r | (g << 8) | (b << 16));
                    }
                }
            }
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"GetBackgroundColorFallback failed: {ex.Message}");
        }
        return 0;
    }

    private void SetBackgroundColorFallback(uint color) {
        try {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\\Colors", true);
            if (key != null) {
                byte r = (byte)(color & 0xFF);
                byte g = (byte)((color >> 8) & 0xFF);
                byte b = (byte)((color >> 16) & 0xFF);
                key.SetValue("Background", $"{r} {g} {b}");
            }
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"SetBackgroundColorFallback failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the position of a monitor.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <returns>The position of the monitor.</returns>
    /// <exception cref="ArgumentException">Thrown when the monitor is not found.</exception>
    public MonitorPosition GetMonitorPosition(string deviceId) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentNullException(nameof(deviceId));
        }
        var monitors = GetMonitors();
        foreach (var monitor in monitors) {
            if (string.Equals(monitor.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase)) {
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
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentNullException(nameof(deviceId));
        }
        if (position == null) {
            throw new ArgumentNullException(nameof(position));
        }

        MonitorPosition current = GetMonitorPosition(deviceId);
        ValidatePositionDimensions(current, position);
        SetMonitorPosition(deviceId, position.Left, position.Top);
    }

    /// <summary>
    /// Sets the top-left position of a monitor without changing its resolution.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="left">The left position.</param>
    /// <param name="top">The top position.</param>
    /// <exception cref="InvalidOperationException">Thrown when unable to set monitor position.</exception>
    /// <exception cref="ArgumentException">Thrown when the monitor or its display source cannot be resolved.</exception>
    public void SetMonitorPosition(string deviceId, int left, int top) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentNullException(nameof(deviceId));
        }
        Monitor? monitor = GetMonitors().FirstOrDefault(candidate => string.Equals(candidate.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
        if (monitor == null || string.IsNullOrWhiteSpace(monitor.DeviceName)) {
            throw new ArgumentException($"Monitor with device ID '{deviceId}' does not have a resolvable display source.", nameof(deviceId));
        }

        DEVMODE devMode = new DEVMODE {
            dmSize = (short)Marshal.SizeOf(typeof(DEVMODE))
        };
        if (!MonitorNativeMethods.EnumDisplaySettings(monitor.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode)) {
            throw new InvalidOperationException($"Unable to get display settings for '{monitor.DeviceName}'.");
        }

        devMode.dmFields = 0x00000020; // DM_POSITION
        devMode.dmPositionX = left;
        devMode.dmPositionY = top;

        DisplayChangeConfirmation result = MonitorNativeMethods.ChangeDisplaySettingsEx(
            monitor.DeviceName,
            ref devMode,
            IntPtr.Zero,
            ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY,
            IntPtr.Zero);
        if (result != DisplayChangeConfirmation.Successful && result != DisplayChangeConfirmation.Restart) {
            throw new InvalidOperationException($"Unable to set monitor position. Error: {result}");
        }
    }

    internal static void ValidatePositionDimensions(MonitorPosition current, MonitorPosition requested) {
        if (current == null) {
            throw new ArgumentNullException(nameof(current));
        }
        if (requested == null) {
            throw new ArgumentNullException(nameof(requested));
        }

        int currentWidth = current.Right - current.Left;
        int currentHeight = current.Bottom - current.Top;
        int requestedWidth = requested.Right - requested.Left;
        int requestedHeight = requested.Bottom - requested.Top;
        if (requestedWidth <= 0 || requestedHeight <= 0) {
            throw new ArgumentException("Monitor bounds must have positive width and height.", nameof(requested));
        }
        if (requestedWidth != currentWidth || requestedHeight != currentHeight) {
            throw new ArgumentException(
                "SetMonitorPosition cannot change monitor dimensions. Preserve the current width and height, or call SetMonitorResolution separately.",
                nameof(requested));
        }
    }

    /// <summary>
    /// Sets the resolution of a monitor.
    /// </summary>
    /// <param name="deviceId">The device ID of the monitor.</param>
    /// <param name="width">The desired width.</param>
    /// <param name="height">The desired height.</param>
    public void SetMonitorResolution(string deviceId, int width, int height) {
        if (width <= 0) {
            throw new ArgumentOutOfRangeException(nameof(width));
        }
        if (height <= 0) {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
        var monitor = GetMonitors().FirstOrDefault(m => string.Equals(m.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
        if (monitor == null || string.IsNullOrWhiteSpace(monitor.DeviceName)) {
            throw new ArgumentException($"Monitor with device ID '{deviceId}' not found", nameof(deviceId));
        }
        var deviceName = monitor.DeviceName;

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
        var monitor = GetMonitors().FirstOrDefault(m => string.Equals(m.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
        if (monitor == null || string.IsNullOrWhiteSpace(monitor.DeviceName)) {
            throw new ArgumentException($"Monitor with device ID '{deviceId}' not found", nameof(deviceId));
        }
        var deviceName = monitor.DeviceName;

        DEVMODE devMode = new DEVMODE();
        devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        if (!MonitorNativeMethods.EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref devMode)) {
            throw new InvalidOperationException("Unable to get display settings");
        }

        if (((orientation == DisplayOrientation.Degrees90 || orientation == DisplayOrientation.Degrees270) &&
                (devMode.dmDisplayOrientation == (int)DisplayOrientation.Default || devMode.dmDisplayOrientation == (int)DisplayOrientation.Degrees180)) ||
            ((orientation == DisplayOrientation.Default || orientation == DisplayOrientation.Degrees180) &&
                (devMode.dmDisplayOrientation == (int)DisplayOrientation.Degrees90 || devMode.dmDisplayOrientation == (int)DisplayOrientation.Degrees270))) {
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
            DesktopManagerDiagnostics.Report("EnumDisplayMonitors failed");
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
            uint released = (uint)monitors.Count(m => m.hPhysicalMonitor != IntPtr.Zero);
            if (released > 0) {
                MonitorNativeMethods.DestroyPhysicalMonitors(released, monitors);
            }
            return Array.Empty<PHYSICAL_MONITOR>();
        }
        _monitorHandles.AddRange(monitors);
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
                DesktopManagerDiagnostics.Report("DestroyPhysicalMonitors failed");
            }
            foreach (var m in monitors) {
                _monitorHandles.Remove(m);
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
                DesktopManagerDiagnostics.Report("DestroyPhysicalMonitors failed");
            }
            foreach (var m in monitors) {
                _monitorHandles.Remove(m);
            }
        }
    }

    /// <summary>
    /// Starts a wallpaper slideshow using the provided images.
    /// </summary>
    /// <param name="wallpaperPaths">Collection of wallpaper file paths.</param>
    public void StartWallpaperSlideshow(IEnumerable<string> wallpaperPaths) {
        StartWallpaperSlideshow(wallpaperPaths, null, null);
    }

    /// <summary>
    /// Starts a wallpaper slideshow using the provided images and optional slideshow settings.
    /// </summary>
    /// <param name="wallpaperPaths">Collection of wallpaper file paths.</param>
    /// <param name="options">Optional slideshow options.</param>
    /// <param name="slideshowTick">Optional slideshow tick interval in milliseconds.</param>
    public void StartWallpaperSlideshow(IEnumerable<string> wallpaperPaths, DesktopSlideshowOptions? options, uint? slideshowTick) {
        if (wallpaperPaths == null) {
            throw new ArgumentNullException(nameof(wallpaperPaths));
        }

        EnsureDesktopWallpaperEnabled();

        IntPtr arrayPtr = IntPtr.Zero;
        try {
            arrayPtr = CreateShellItemArray(wallpaperPaths);
            Execute(() => _desktopManager.SetSlideshow(arrayPtr), nameof(IDesktopManager.SetSlideshow));
            if (options.HasValue || slideshowTick.HasValue) {
                var current = GetWallpaperSlideshow();
                SetWallpaperSlideshowOptions(
                    options ?? current.Options,
                    slideshowTick ?? current.SlideshowTick);
            }
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

    /// <summary>
    /// Gets the current desktop wallpaper slideshow configuration and state.
    /// </summary>
    /// <returns>The current wallpaper slideshow details.</returns>
    public DesktopWallpaperSlideshow GetWallpaperSlideshow() {
        DesktopSlideshowOptions options = DesktopSlideshowOptions.None;
        uint slideshowTick = 0;
        try {
            uint optionsHResult = _desktopManager.GetSlideshowOptions(out DesktopSlideshowOptions currentOptions, out uint currentTick);
            if (optionsHResult == 0) {
                options = currentOptions;
                slideshowTick = currentTick;
            }
        } catch (COMException) {
        } catch (DesktopManagerException) {
        }

        DesktopSlideshowState state = DesktopSlideshowState.None;
        try {
            uint statusHResult = _desktopManager.GetStatus(out DesktopSlideshowState currentState);
            if (statusHResult == 0) {
                state = currentState;
            }
        } catch (COMException) {
        } catch (DesktopManagerException) {
        }

        return new DesktopWallpaperSlideshow {
            ImagePaths = GetWallpaperSlideshowPaths(),
            State = state,
            Options = options,
            SlideshowTick = slideshowTick
        };
    }

    /// <summary>
    /// Sets desktop wallpaper slideshow options.
    /// </summary>
    /// <param name="options">Slideshow options.</param>
    /// <param name="slideshowTick">Slideshow tick interval in milliseconds.</param>
    public void SetWallpaperSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick) {
        Execute(() => _desktopManager.SetSlideshowOptions(options, slideshowTick), nameof(IDesktopManager.SetSlideshowOptions));
    }

    private IReadOnlyList<string> GetWallpaperSlideshowPaths() {
        IntPtr arrayPtr = IntPtr.Zero;
        uint hresult;
        try {
            hresult = _desktopManager.GetSlideshow(out arrayPtr);
        } catch (COMException) {
            return Array.Empty<string>();
        } catch (DesktopManagerException) {
            return Array.Empty<string>();
        }

        if (hresult != 0 || arrayPtr == IntPtr.Zero) {
            return Array.Empty<string>();
        }

        try {
            var array = (MonitorNativeMethods.IShellItemArray)Marshal.GetObjectForIUnknown(arrayPtr);
            try {
                int countResult = array.GetCount(out uint count);
                if (countResult != 0 || count == 0) {
                    return Array.Empty<string>();
                }

                var paths = new List<string>();
                for (uint index = 0; index < count; index++) {
                    int itemResult = array.GetItemAt(index, out MonitorNativeMethods.IShellItem item);
                    if (itemResult != 0 || item == null) {
                        continue;
                    }

                    try {
                        var path = GetShellItemDisplayName(item, MonitorNativeMethods.SIGDN.FileSystemPath)
                            ?? GetShellItemDisplayName(item, MonitorNativeMethods.SIGDN.NormalDisplay);
                        if (!string.IsNullOrWhiteSpace(path)) {
                            paths.Add(path!);
                        }
                    } finally {
                        Marshal.ReleaseComObject(item);
                    }
                }

                return paths;
            } finally {
                Marshal.ReleaseComObject(array);
            }
        } finally {
            Marshal.Release(arrayPtr);
        }
    }

    private static string? GetShellItemDisplayName(MonitorNativeMethods.IShellItem item, MonitorNativeMethods.SIGDN displayName) {
        IntPtr namePtr = IntPtr.Zero;
        int result = item.GetDisplayName(displayName, out namePtr);
        if (result != 0 || namePtr == IntPtr.Zero) {
            return null;
        }

        try {
            return Marshal.PtrToStringUni(namePtr);
        } finally {
            MonitorNativeMethods.CoTaskMemFree(namePtr);
        }
    }

    private static IntPtr CreateShellItemArray(IEnumerable<string> paths) {
        Guid clsidEnum = new("2d3468c1-36a7-43b6-ac24-d3f02fd9607a");
        Guid iidShellItem = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");
        Guid iidShellItemArray = new("b63ea76d-1f85-456f-a19c-48159efa858b");

        Type collectionType = Type.GetTypeFromCLSID(clsidEnum)
            ?? throw new InvalidOperationException("IObjectCollection CLSID not available.");
        object? instance = Activator.CreateInstance(collectionType);
        if (instance is not MonitorNativeMethods.IObjectCollection collection) {
            throw new InvalidOperationException("IObjectCollection instance not available.");
        }

        try {
            foreach (var path in paths) {
                if (string.IsNullOrEmpty(path)) continue;
                int hr = MonitorNativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iidShellItem, out IntPtr item);
                if (hr != 0) {
                    try {
                        Marshal.ThrowExceptionForHR(hr);
                    } catch (Exception ex) {
                        throw new InvalidOperationException($"SHCreateItemFromParsingName failed for '{path}'", ex);
                    }
                }
                object obj = Marshal.GetObjectForIUnknown(item);
                try {
                    collection.AddObject(obj);
                } finally {
                    Marshal.ReleaseComObject(obj);
                    Marshal.Release(item);
                }
            }

            IntPtr unk = Marshal.GetIUnknownForObject(collection);
#if NET10_0_OR_GREATER
            Marshal.QueryInterface(unk, in iidShellItemArray, out IntPtr arrayPtr);
#else
            Marshal.QueryInterface(unk, ref iidShellItemArray, out IntPtr arrayPtr);
#endif
            Marshal.Release(unk);
            return arrayPtr;
        } finally {
            Marshal.ReleaseComObject(collection);
        }
    }

    /// <summary>
    /// Gets all display devices.
    /// </summary>
    /// <returns>A list of all <see cref="DISPLAY_DEVICE"/> objects.</returns>
    public List<DISPLAY_DEVICE> DisplayDevicesAll() {
        List<DISPLAY_DEVICE> devices = new List<DISPLAY_DEVICE>();
        uint deviceNum = 0;
        while (true) {
            DISPLAY_DEVICE device = new DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(device);
            if (!MonitorNativeMethods.EnumDisplayDevices(null, deviceNum, ref device, (uint)EnumDisplayDevicesFlags.EDD_GET_DEVICE_INTERFACE_NAME)) {
                break;
            }
            devices.Add(device);
            deviceNum++;
        }
        return devices;
    }

    /// <summary>
    /// Gets all connected display devices.
    /// </summary>
    /// <returns>A list of connected <see cref="DISPLAY_DEVICE"/> objects.</returns>
    public List<DISPLAY_DEVICE> DisplayDevicesConnected() {
        List<DISPLAY_DEVICE> devices = new List<DISPLAY_DEVICE>();
        uint deviceNum = 0;
        while (true) {
            DISPLAY_DEVICE device = new DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(device);
            if (!MonitorNativeMethods.EnumDisplayDevices(null, deviceNum, ref device, (uint)EnumDisplayDevicesFlags.EDD_GET_DEVICE_INTERFACE_NAME)) {
                break;
            }
            if ((device.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0) {
                devices.Add(device);
            }
            deviceNum++;
        }

        return devices;
    }
}
