using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DesktopManager;

public partial class MonitorService {
    internal sealed class DisplayPathSnapshot {
        public string AdapterDeviceName { get; set; } = string.Empty;
        public string AdapterDeviceString { get; set; } = string.Empty;
        public DisplayDeviceStateFlags AdapterStateFlags { get; set; }
        public string AdapterDeviceKey { get; set; } = string.Empty;
        public RECT? AdapterBounds { get; set; }
        public RECT? AdapterWorkArea { get; set; }
        public string MonitorDeviceId { get; set; } = string.Empty;
        public string MonitorDeviceString { get; set; } = string.Empty;
        public string MonitorDeviceKey { get; set; } = string.Empty;
    }

    internal sealed class DisplayAdapterArea {
        public RECT Bounds { get; set; }
        public RECT WorkArea { get; set; }
    }

    internal static DisplayPathSnapshot? ResolveDisplayPathForMonitor(
        IReadOnlyList<DisplayPathSnapshot> displayPaths,
        string deviceId,
        RECT bounds,
        int fallbackIndex) {
        if (!string.IsNullOrWhiteSpace(deviceId)) {
            foreach (DisplayPathSnapshot displayPath in displayPaths) {
                if (MonitorDeviceIdsMatch(displayPath.MonitorDeviceId, deviceId)) {
                    return displayPath;
                }
            }
        }

        foreach (DisplayPathSnapshot displayPath in displayPaths) {
            if ((displayPath.AdapterStateFlags & DisplayDeviceStateFlags.AttachedToDesktop) == 0) {
                continue;
            }

            if (displayPath.AdapterBounds.HasValue && AreSameBounds(displayPath.AdapterBounds.Value, bounds)) {
                return displayPath;
            }
        }

        if (fallbackIndex >= 0 && fallbackIndex < displayPaths.Count) {
            return displayPaths[fallbackIndex];
        }

        return null;
    }

    private static List<DisplayPathSnapshot> EnumerateDisplayPaths() {
        List<DisplayPathSnapshot> displayPaths = new List<DisplayPathSnapshot>();
        Dictionary<string, DisplayAdapterArea> adapterAreas = EnumerateAdapterAreas();

        uint adapterIndex = 0;
        while (true) {
            DISPLAY_DEVICE adapter = new DISPLAY_DEVICE();
            adapter.cb = Marshal.SizeOf(adapter);

            if (!MonitorNativeMethods.EnumDisplayDevices(null, adapterIndex, ref adapter, (uint)EnumDisplayDevicesFlags.EDD_GET_DEVICE_INTERFACE_NAME)) {
                break;
            }

            RECT? bounds = null;
            RECT? workArea = null;
            DisplayAdapterArea? adapterArea = null;
            if (!string.IsNullOrWhiteSpace(adapter.DeviceName) &&
                adapterAreas.TryGetValue(adapter.DeviceName, out adapterArea)) {
                bounds = adapterArea.Bounds;
                workArea = adapterArea.WorkArea;
            }

            uint monitorIndex = 0;
            bool childFound = false;
            while (true) {
                DISPLAY_DEVICE monitor = new DISPLAY_DEVICE();
                monitor.cb = Marshal.SizeOf(monitor);

                if (!MonitorNativeMethods.EnumDisplayDevices(adapter.DeviceName, monitorIndex, ref monitor, (uint)EnumDisplayDevicesFlags.EDD_GET_DEVICE_INTERFACE_NAME)) {
                    break;
                }

                childFound = true;
                displayPaths.Add(new DisplayPathSnapshot {
                    AdapterDeviceName = adapter.DeviceName ?? string.Empty,
                    AdapterDeviceString = adapter.DeviceString ?? string.Empty,
                    AdapterStateFlags = adapter.StateFlags,
                    AdapterDeviceKey = adapter.DeviceKey ?? string.Empty,
                    AdapterBounds = bounds,
                    AdapterWorkArea = workArea,
                    MonitorDeviceId = monitor.DeviceID ?? string.Empty,
                    MonitorDeviceString = monitor.DeviceString ?? string.Empty,
                    MonitorDeviceKey = monitor.DeviceKey ?? string.Empty
                });
                monitorIndex++;
            }

            if (!childFound) {
                displayPaths.Add(new DisplayPathSnapshot {
                    AdapterDeviceName = adapter.DeviceName ?? string.Empty,
                    AdapterDeviceString = adapter.DeviceString ?? string.Empty,
                    AdapterStateFlags = adapter.StateFlags,
                    AdapterDeviceKey = adapter.DeviceKey ?? string.Empty,
                    AdapterBounds = bounds,
                    AdapterWorkArea = workArea
                });
            }

            adapterIndex++;
        }

        return displayPaths;
    }

    private static Dictionary<string, DisplayAdapterArea> EnumerateAdapterAreas() {
        Dictionary<string, DisplayAdapterArea> areasByAdapter = new Dictionary<string, DisplayAdapterArea>(StringComparer.OrdinalIgnoreCase);

        MonitorNativeMethods.MonitorEnumProc proc = (IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr lparam) => {
            MONITORINFOEX info = new MONITORINFOEX();
            info.cbSize = Marshal.SizeOf<MONITORINFOEX>();
            if (MonitorNativeMethods.GetMonitorInfo(hMonitor, ref info) &&
                !string.IsNullOrWhiteSpace(info.szDevice) &&
                !areasByAdapter.ContainsKey(info.szDevice)) {
                areasByAdapter.Add(info.szDevice, new DisplayAdapterArea {
                    Bounds = info.rcMonitor,
                    WorkArea = info.rcWork
                });
            }

            return true;
        };

        MonitorNativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
        return areasByAdapter;
    }

    private static void ApplyDisplayPathMetadata(Monitor monitor, DisplayPathSnapshot displayPath) {
        monitor.DeviceName = displayPath.AdapterDeviceName;
        monitor.DeviceString = !string.IsNullOrWhiteSpace(displayPath.MonitorDeviceString)
            ? displayPath.MonitorDeviceString
            : displayPath.AdapterDeviceString;
        monitor.StateFlags = displayPath.AdapterStateFlags;
        monitor.DeviceKey = !string.IsNullOrWhiteSpace(displayPath.MonitorDeviceKey)
            ? displayPath.MonitorDeviceKey
            : displayPath.AdapterDeviceKey;
        if (displayPath.AdapterWorkArea.HasValue) {
            monitor.WorkArea = displayPath.AdapterWorkArea.Value;
        }
    }

    internal static string ResolveDisplaySourceName(string adapterDeviceName, string monitorDeviceName) {
        return !string.IsNullOrWhiteSpace(adapterDeviceName)
            ? adapterDeviceName
            : monitorDeviceName ?? string.Empty;
    }

    private static bool MonitorDeviceIdsMatch(string candidate, string deviceId) {
        return !string.IsNullOrWhiteSpace(candidate) &&
               !string.IsNullOrWhiteSpace(deviceId) &&
               string.Equals(candidate.Trim(), deviceId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool AreSameBounds(RECT left, RECT right) {
        return left.Left == right.Left &&
               left.Top == right.Top &&
               left.Right == right.Right &&
               left.Bottom == right.Bottom;
    }
}
