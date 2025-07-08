using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Service providing access to taskbar manipulation per monitor.
/// </summary>
public class TaskbarService {
    private readonly Monitors _monitors;

    /// <summary>Initializes a new instance of the <see cref="TaskbarService"/> class.</summary>
    public TaskbarService() {
        _monitors = new Monitors();
    }

    /// <summary>Gets taskbars on the system.</summary>
    /// <returns>List of taskbar information.</returns>
    public List<TaskbarInfo> GetTaskbars() {
        List<TaskbarInfo> list = new List<TaskbarInfo>();

        IntPtr primary = MonitorNativeMethods.FindWindow("Shell_TrayWnd", null);
        if (primary != IntPtr.Zero) {
            list.Add(new TaskbarInfo { Handle = primary, MonitorIndex = GetMonitorIndex(primary) });
        }

        MonitorNativeMethods.EnumWindows((hWnd, l) => {
            StringBuilder sb = new StringBuilder(64);
            MonitorNativeMethods.GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString() == "Shell_SecondaryTrayWnd") {
                list.Add(new TaskbarInfo { Handle = hWnd, MonitorIndex = GetMonitorIndex(hWnd) });
            }
            return true;
        }, IntPtr.Zero);

        return list;
    }

    private int GetMonitorIndex(IntPtr hWnd) {
        IntPtr hMon = MonitorNativeMethods.MonitorFromWindow(hWnd, MonitorNativeMethods.MONITOR_DEFAULTTONEAREST);
        MONITORINFOEX info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        if (MonitorNativeMethods.GetMonitorInfo(hMon, ref info)) {
            foreach (var m in _monitors.GetMonitors()) {
                if (m.Rect.Left == info.rcMonitor.Left && m.Rect.Top == info.rcMonitor.Top &&
                    m.Rect.Right == info.rcMonitor.Right && m.Rect.Bottom == info.rcMonitor.Bottom) {
                    return m.Index;
                }
            }
        }
        return -1;
    }

    /// <summary>Shows or hides the taskbar on the specified monitor.</summary>
    /// <param name="monitorIndex">Index of the monitor.</param>
    /// <param name="visible">True to show, false to hide.</param>
    public void SetTaskbarVisibility(int monitorIndex, bool visible) {
        foreach (var tb in GetTaskbars()) {
            if (tb.MonitorIndex == monitorIndex) {
                MonitorNativeMethods.ShowWindow(tb.Handle, visible ? MonitorNativeMethods.SW_SHOW : MonitorNativeMethods.SW_HIDE);
            }
        }
    }

    /// <summary>Moves the taskbar on the specified monitor to the given edge.</summary>
    /// <param name="monitorIndex">Index of the monitor.</param>
    /// <param name="position">Target taskbar position.</param>
    public void SetTaskbarPosition(int monitorIndex, TaskbarPosition position) {
        foreach (var tb in GetTaskbars()) {
            if (tb.MonitorIndex == monitorIndex) {
                var monitor = _monitors.GetMonitors(index: monitorIndex).FirstOrDefault();
                if (monitor == null) {
                    continue;
                }
                RECT bounds = monitor.GetMonitorBounds();
                APPBARDATA abd = new APPBARDATA {
                    cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
                    hWnd = tb.Handle,
                    uEdge = (int)position,
                    rc = bounds
                };
                MonitorNativeMethods.SHAppBarMessage(MonitorNativeMethods.ABM_QUERYPOS, ref abd);
                MonitorNativeMethods.SHAppBarMessage(MonitorNativeMethods.ABM_SETPOS, ref abd);
                MonitorNativeMethods.SetWindowPos(tb.Handle, IntPtr.Zero, abd.rc.Left, abd.rc.Top,
                    abd.rc.Right - abd.rc.Left, abd.rc.Bottom - abd.rc.Top, MonitorNativeMethods.SWP_NOZORDER);
            }
        }
    }
}
