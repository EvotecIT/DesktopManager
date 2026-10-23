using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>Resolves a window's current monitor without treating an unknown observation as index zero.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class WindowMonitorResolver {
    internal static Monitor? Resolve(WindowInfo window, IReadOnlyList<Monitor> monitors) {
        if (window.Handle != IntPtr.Zero) {
            // Windows also remembers a minimized window's monitor. Match by device name,
            // since monitor handles and enumeration indices are not persistent identities.
            IntPtr handle = MonitorNativeMethods.MonitorFromWindow(window.Handle, MonitorNativeMethods.MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (handle != IntPtr.Zero && MonitorNativeMethods.GetMonitorInfo(handle, ref info)) {
                Monitor? native = monitors.FirstOrDefault(monitor =>
                    !string.IsNullOrEmpty(monitor.DeviceName) && string.Equals(monitor.DeviceName, info.szDevice, StringComparison.OrdinalIgnoreCase));
                if (native != null) { return native; }
            }
        }
        return FromBounds(window, monitors);
    }

    /// <summary>Uses largest intersection, then nearest rectangle for off-screen windows and detached native targets.</summary>
    internal static Monitor? FromBounds(WindowInfo window, IReadOnlyList<Monitor> monitors) {
        Monitor? best = null;
        double bestArea = -1, bestDistance = double.MaxValue;
        foreach (Monitor monitor in monitors) {
            RECT bounds = monitor.Rect;
            if (bounds.Right <= bounds.Left || bounds.Bottom <= bounds.Top) { continue; }
            double width = Math.Max(0L, Math.Min((long)window.Right, bounds.Right) - Math.Max((long)window.Left, bounds.Left));
            double height = Math.Max(0L, Math.Min((long)window.Bottom, bounds.Bottom) - Math.Max((long)window.Top, bounds.Top));
            double area = width * height;
            double dx = Math.Max(0L, Math.Max((long)bounds.Left - window.Right, (long)window.Left - bounds.Right));
            double dy = Math.Max(0L, Math.Max((long)bounds.Top - window.Bottom, (long)window.Top - bounds.Bottom));
            double distance = dx * dx + dy * dy;
            if (area > bestArea || (area == bestArea && distance < bestDistance)) {
                best = monitor; bestArea = area; bestDistance = distance;
            }
        }
        return best;
    }
}
