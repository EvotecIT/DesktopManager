using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesktopManager;

/// <summary>
/// Service for managing monitors and their settings.
/// </summary>
public partial class MonitorService {
    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int DM_LOGPIXELS = 0x00020000;
    private readonly IDesktopManager _desktopManager;
    private readonly ILogger<MonitorService> _logger;

    /// <summary>
    /// Executes an action and translates COM exceptions to <see cref="DesktopManagerException"/>.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <param name="operation">Name of the operation for context.</param>
    private void Execute(Action action, string operation) {
        try {
            action();
        } catch (COMException ex) {
            throw new DesktopManagerException(operation, ex);
        }
    }

    /// <summary>
    /// Executes a function and translates COM exceptions to <see cref="DesktopManagerException"/>.
    /// </summary>
    /// <typeparam name="T">Return type of the function.</typeparam>
    /// <param name="func">Function to execute.</param>
    /// <param name="operation">Name of the operation for context.</param>
    /// <returns>The value returned by <paramref name="func"/>.</returns>
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
    /// <param name="logger">Optional logger instance.</param>
    public MonitorService(IDesktopManager desktopManager, ILogger<MonitorService>? logger = null) {
        _desktopManager = desktopManager;
        _logger = logger ?? NullLogger<MonitorService>.Instance;

        try {
            Execute(() => _desktopManager.Enable(), nameof(IDesktopManager.Enable));
        } catch (DesktopManagerException ex) {
            _logger.LogError(ex, "DesktopManager initialization failed.");
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
        if (!MonitorNativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero)) {
            _logger.LogError("EnumDisplayMonitors failed");
        }
        
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

}
