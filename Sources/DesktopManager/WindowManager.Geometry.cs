using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager;

public partial class WindowManager {
    private const int DwmExtendedFrameBounds = 9;

    [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
    private static extern int DwmGetWindowRect(IntPtr window, int attribute, out RECT value, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);

    /// <summary>
    /// The rectangle a snap position covers inside a work area. Halves and thirds share their edges,
    /// so neighbouring snapped windows leave no gap when the work area doesn't divide evenly.
    /// </summary>
    internal static RECT GetSnapRectangle(RECT area, SnapPosition position) {
        int width = area.Right - area.Left;
        int height = area.Bottom - area.Top;
        int midX = area.Left + width / 2;
        int midY = area.Top + height / 2;
        int thirdX = area.Left + width / 3;
        int twoThirdsX = area.Left + width * 2 / 3;
        return position switch {
            SnapPosition.Left => Rect(area.Left, area.Top, midX, area.Bottom),
            SnapPosition.Right => Rect(midX, area.Top, area.Right, area.Bottom),
            SnapPosition.TopLeft => Rect(area.Left, area.Top, midX, midY),
            SnapPosition.TopRight => Rect(midX, area.Top, area.Right, midY),
            SnapPosition.BottomLeft => Rect(area.Left, midY, midX, area.Bottom),
            SnapPosition.BottomRight => Rect(midX, midY, area.Right, area.Bottom),
            SnapPosition.Top => Rect(area.Left, area.Top, area.Right, midY),
            SnapPosition.Bottom => Rect(area.Left, midY, area.Right, area.Bottom),
            SnapPosition.LeftThird => Rect(area.Left, area.Top, thirdX, area.Bottom),
            SnapPosition.CenterThird => Rect(thirdX, area.Top, twoThirdsX, area.Bottom),
            SnapPosition.RightThird => Rect(twoThirdsX, area.Top, area.Right, area.Bottom),
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, "This snap position has no fixed rectangle.")
        };
    }

    /// <summary>A rectangle of the given size centred in a work area, shrunk to fit when it is larger.</summary>
    internal static RECT CenterInArea(RECT area, int width, int height) {
        width = Math.Min(Math.Max(width, 1), area.Right - area.Left);
        height = Math.Min(Math.Max(height, 1), area.Bottom - area.Top);
        int left = area.Left + (area.Right - area.Left - width) / 2;
        int top = area.Top + (area.Bottom - area.Top - height) / 2;
        return Rect(left, top, left + width, top + height);
    }

    /// <summary>
    /// The window rectangle that makes the visible frame cover <paramref name="visibleTarget"/>.
    /// Windows 10 and 11 draw an invisible resize border outside the visible frame, so placing the
    /// window rectangle itself would leave gaps between snapped windows and at the screen edges.
    /// </summary>
    internal static RECT ExpandForInvisibleFrame(RECT visibleTarget, RECT windowRect, RECT visibleFrame) {
        int left = visibleFrame.Left - windowRect.Left;
        int top = visibleFrame.Top - windowRect.Top;
        int right = windowRect.Right - visibleFrame.Right;
        int bottom = windowRect.Bottom - visibleFrame.Bottom;
        // A frame outside the window rectangle, or a border wider than any real resize border, is not one.
        if (left < 0 || top < 0 || right < 0 || bottom < 0 || Math.Max(Math.Max(left, top), Math.Max(right, bottom)) > 64) {
            return visibleTarget;
        }

        return Rect(visibleTarget.Left - left, visibleTarget.Top - top, visibleTarget.Right + right, visibleTarget.Bottom + bottom);
    }

    /// <summary>
    /// Keeps the top-left corner of a window that is not per-monitor DPI aware on its monitor. Windows maps such a
    /// window through the DPI of the monitor its corner lands on, so a border pushed onto the neighbouring monitor
    /// makes it jump and double in width. The visible frame then stops a border's width short of that edge.
    /// </summary>
    internal static RECT KeepOriginOnMonitor(RECT target, RECT monitorBounds) {
        target.Left = Math.Max(target.Left, monitorBounds.Left);
        target.Top = Math.Max(target.Top, monitorBounds.Top);
        return target;
    }

    private static bool IsPerMonitorDpiAware(IntPtr handle) {
        try {
            // DPI_AWARENESS_PER_MONITOR_AWARE covers per-monitor V1 and V2.
            return GetAwarenessFromDpiAwarenessContext(GetWindowDpiAwarenessContext(handle)) == 2;
        } catch (EntryPointNotFoundException) {
            // Before Windows 10 1607 there are no thread DPI contexts, so coordinates are not mixed.
            return true;
        }
    }

    /// <summary>Reads the window rectangle and the visible frame DWM draws inside it.</summary>
    private static bool TryGetVisibleFrame(IntPtr handle, out RECT windowRect, out RECT visibleFrame) {
        visibleFrame = default;
        if (!MonitorNativeMethods.GetWindowRect(handle, out windowRect)) {
            return false;
        }

        try {
            return DwmGetWindowRect(handle, DwmExtendedFrameBounds, out visibleFrame, Marshal.SizeOf<RECT>()) == 0 &&
                visibleFrame.Right > visibleFrame.Left && visibleFrame.Bottom > visibleFrame.Top;
        } catch (DllNotFoundException) {
            return false;
        } catch (EntryPointNotFoundException) {
            return false;
        }
    }

    /// <summary>
    /// Places a window so its visible frame covers the target. A minimised or maximised window is
    /// restored to the target first, without activating it.
    /// </summary>
    internal void PlaceVisibleFrame(WindowInfo window, RECT visibleTarget, RECT monitorBounds, string operation) {
        WindowDpiContext.Execute(() => {
            IntPtr handle = window.Handle;
            if (MonitorNativeMethods.IsIconic(handle) || IsZoomed(handle)) {
                WorkspaceWindowPlacement.RestoreToNormalBounds(handle, visibleTarget);
            }

            RECT target = TryGetVisibleFrame(handle, out RECT windowRect, out RECT frame)
                ? ExpandForInvisibleFrame(visibleTarget, windowRect, frame)
                : visibleTarget;
            if (!IsPerMonitorDpiAware(handle)) {
                target = KeepOriginOnMonitor(target, monitorBounds);
            }
            SetWindowPosOrThrow(handle, IntPtr.Zero, target.Left, target.Top, target.Right - target.Left, target.Bottom - target.Top,
                MonitorNativeMethods.SWP_NOZORDER | MonitorNativeMethods.SWP_NOACTIVATE, operation);
        });
    }

    /// <summary>The visible bounds of a window: its DWM frame when available, otherwise its window rectangle.</summary>
    private static RECT GetVisibleBounds(IntPtr handle) {
        if (TryGetVisibleFrame(handle, out RECT windowRect, out RECT frame)) {
            return frame;
        }

        if (!MonitorNativeMethods.GetWindowRect(handle, out windowRect)) {
            throw CreateWindowOperationException(handle, "Read the window position");
        }

        return windowRect;
    }

    /// <summary>The window's current monitor, read fresh so its work area matches the current DPI context.</summary>
    private Monitor ResolveLiveMonitor(WindowInfo window) {
        List<Monitor> monitors = _monitors.GetMonitors(connectedOnly: true, refresh: true);
        if (window.MonitorIndex >= 0) {
            Monitor? requested = monitors.FirstOrDefault(monitor => monitor.Index == window.MonitorIndex);
            if (requested != null) {
                return requested;
            }
        }

        if (MonitorNativeMethods.GetWindowRect(window.Handle, out RECT rect)) {
            var probe = new WindowInfo { Handle = window.Handle, Left = rect.Left, Top = rect.Top, Right = rect.Right, Bottom = rect.Bottom };
            Monitor? current = WindowMonitorResolver.Resolve(probe, monitors);
            if (current != null) {
                return current;
            }
        }

        return monitors.FirstOrDefault(monitor => monitor.IsPrimary) ?? monitors.FirstOrDefault()
            ?? throw new InvalidOperationException("No monitor found for window placement.");
    }

    /// <summary>Finds the freshly enumerated copy of a monitor the caller may have listed in another DPI context.</summary>
    private Monitor RefreshMonitor(Monitor monitor) {
        List<Monitor> monitors = _monitors.GetMonitors(refresh: true);
        return monitors.FirstOrDefault(candidate => !string.IsNullOrEmpty(monitor.DeviceName) &&
                string.Equals(candidate.DeviceName, monitor.DeviceName, StringComparison.OrdinalIgnoreCase)) ??
            monitors.FirstOrDefault(candidate => !string.IsNullOrEmpty(monitor.DeviceId) &&
                string.Equals(candidate.DeviceId, monitor.DeviceId, StringComparison.OrdinalIgnoreCase)) ??
            monitor;
    }

    /// <summary>A monitor's bounds as enumerated, read again only when the enumeration had none.</summary>
    internal static RECT GetBounds(Monitor monitor) {
        return monitor.Rect.Right > monitor.Rect.Left && monitor.Rect.Bottom > monitor.Rect.Top ? monitor.Rect : monitor.GetMonitorBounds();
    }

    private static RECT Rect(int left, int top, int right, int bottom) {
        return new RECT { Left = left, Top = top, Right = right, Bottom = bottom };
    }
}
