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
            list.Add(CreateTaskbarInfo(primary));
        }

        MonitorNativeMethods.EnumWindows((hWnd, l) => {
            StringBuilder sb = new StringBuilder(64);
            MonitorNativeMethods.GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString() == "Shell_SecondaryTrayWnd") {
                list.Add(CreateTaskbarInfo(hWnd));
            }
            return true;
        }, IntPtr.Zero);

        return list;
    }

    /// <summary>Gets whether Windows taskbar auto-hide is enabled.</summary>
    /// <returns><c>true</c> when auto-hide is enabled.</returns>
    public bool GetTaskbarAutoHide() {
        APPBARDATA data = CreateAppBarData(IntPtr.Zero);
        uint state = MonitorNativeMethods.SHAppBarMessage(MonitorNativeMethods.ABM_GETSTATE, ref data);
        return (state & MonitorNativeMethods.ABS_AUTOHIDE) != 0;
    }

    /// <summary>Enables or disables the Windows taskbar auto-hide setting.</summary>
    /// <param name="enabled">The explicit auto-hide state.</param>
    public void SetTaskbarAutoHide(bool enabled) {
        APPBARDATA data = CreateAppBarData(IntPtr.Zero);
        uint current = MonitorNativeMethods.SHAppBarMessage(MonitorNativeMethods.ABM_GETSTATE, ref data);
        int state = (int)(current & MonitorNativeMethods.ABS_ALWAYSONTOP);
        if (enabled) {
            state |= MonitorNativeMethods.ABS_AUTOHIDE;
        }

        data.lParam = new IntPtr(state);
        MonitorNativeMethods.SHAppBarMessage(MonitorNativeMethods.ABM_SETSTATE, ref data);
        if (GetTaskbarAutoHide() != enabled) {
            throw new InvalidOperationException($"Windows did not apply taskbar auto-hide {enabled}.");
        }
    }

    private int GetMonitorIndex(IntPtr hWnd) {
        IntPtr hMon = MonitorNativeMethods.MonitorFromWindow(hWnd, MonitorNativeMethods.MONITOR_DEFAULTTONEAREST);
        MONITORINFOEX info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        if (MonitorNativeMethods.GetMonitorInfo(hMon, ref info)) {
            foreach (var m in _monitors.GetMonitors()) {
                if (string.Equals(m.DeviceName, info.szDevice, StringComparison.OrdinalIgnoreCase) ||
                    (m.Rect.Left == info.rcMonitor.Left && m.Rect.Top == info.rcMonitor.Top &&
                    m.Rect.Right == info.rcMonitor.Right && m.Rect.Bottom == info.rcMonitor.Bottom)) {
                    return m.Index;
                }
            }
        }
        return -1;
    }

    private TaskbarInfo CreateTaskbarInfo(IntPtr handle) {
        MonitorPosition bounds = new(0, 0, 0, 0);
        if (MonitorNativeMethods.GetWindowRect(handle, out RECT rectangle)) {
            bounds = new MonitorPosition(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        return new TaskbarInfo {
            Handle = handle,
            MonitorIndex = GetMonitorIndex(handle),
            IsVisible = MonitorNativeMethods.IsWindowVisible(handle),
            Position = GetTaskbarPosition(handle, bounds),
            Bounds = bounds
        };
    }

    private TaskbarPosition GetTaskbarPosition(IntPtr handle, MonitorPosition bounds) {
        APPBARDATA data = CreateAppBarData(handle);
        if (MonitorNativeMethods.SHAppBarMessage(MonitorNativeMethods.ABM_GETTASKBARPOS, ref data) != 0 &&
            data.uEdge >= (int)TaskbarPosition.Left &&
            data.uEdge <= (int)TaskbarPosition.Bottom) {
            return (TaskbarPosition)data.uEdge;
        }

        int monitorIndex = GetMonitorIndex(handle);
        Monitor? monitor = _monitors.GetMonitors(index: monitorIndex).FirstOrDefault();
        if (monitor == null) {
            return TaskbarPosition.Bottom;
        }
        MonitorPosition monitorBounds = monitor.Position;
        if (bounds.Left <= monitorBounds.Left && bounds.Right < monitorBounds.Right) {
            return TaskbarPosition.Left;
        }
        if (bounds.Top <= monitorBounds.Top && bounds.Bottom < monitorBounds.Bottom) {
            return TaskbarPosition.Top;
        }
        if (bounds.Right >= monitorBounds.Right && bounds.Left > monitorBounds.Left) {
            return TaskbarPosition.Right;
        }
        return TaskbarPosition.Bottom;
    }

    private static APPBARDATA CreateAppBarData(IntPtr handle) {
        return new APPBARDATA {
            cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
            hWnd = handle
        };
    }

    /// <summary>Shows or hides the taskbar on the specified monitor.</summary>
    /// <param name="monitorIndex">Index of the monitor.</param>
    /// <param name="visible">True to show, false to hide.</param>
    public void SetTaskbarVisibility(int monitorIndex, bool visible) {
        bool matched = false;
        foreach (TaskbarInfo tb in GetTaskbars()) {
            if (tb.MonitorIndex == monitorIndex) {
                matched = true;
                MonitorNativeMethods.ShowWindow(tb.Handle, visible ? MonitorNativeMethods.SW_SHOW : MonitorNativeMethods.SW_HIDE);
                if (MonitorNativeMethods.IsWindowVisible(tb.Handle) != visible) {
                    throw new InvalidOperationException($"Windows did not apply taskbar visibility {visible} on monitor {monitorIndex}.");
                }
            }
        }
        if (!matched) {
            throw new InvalidOperationException($"No taskbar was found on monitor {monitorIndex}.");
        }
    }

    /// <summary>Moves the taskbar on the specified monitor to the given edge.</summary>
    /// <param name="monitorIndex">Index of the monitor.</param>
    /// <param name="position">Target taskbar position.</param>
    public void SetTaskbarPosition(int monitorIndex, TaskbarPosition position) {
        if (!Enum.IsDefined(typeof(TaskbarPosition), position)) {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        bool matched = false;
        foreach (TaskbarInfo tb in GetTaskbars()) {
            if (tb.MonitorIndex == monitorIndex) {
                matched = true;
                Monitor monitor = _monitors.GetMonitors(index: monitorIndex).FirstOrDefault()
                    ?? throw new InvalidOperationException($"Monitor {monitorIndex} was not found.");
                RECT bounds = monitor.GetMonitorBounds();
                int width = Math.Max(1, tb.Bounds.Right - tb.Bounds.Left);
                int height = Math.Max(1, tb.Bounds.Bottom - tb.Bounds.Top);
                RECT requested = CreateTaskbarBounds(bounds, position, width, height);
                APPBARDATA abd = new APPBARDATA {
                    cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
                    hWnd = tb.Handle,
                    uEdge = (int)position,
                    rc = requested
                };
                MonitorNativeMethods.SHAppBarMessage(MonitorNativeMethods.ABM_QUERYPOS, ref abd);
                ConstrainTaskbarThickness(ref abd.rc, position, width, height);
                if (MonitorNativeMethods.SHAppBarMessage(MonitorNativeMethods.ABM_SETPOS, ref abd) == 0) {
                    throw new InvalidOperationException($"Windows rejected taskbar position {position} on monitor {monitorIndex}.");
                }
                if (!MonitorNativeMethods.SetWindowPos(tb.Handle, IntPtr.Zero, abd.rc.Left, abd.rc.Top,
                    abd.rc.Right - abd.rc.Left, abd.rc.Bottom - abd.rc.Top, MonitorNativeMethods.SWP_NOZORDER)) {
                    throw new InvalidOperationException($"Windows did not move the taskbar on monitor {monitorIndex}.");
                }
            }
        }
        if (!matched) {
            throw new InvalidOperationException($"No taskbar was found on monitor {monitorIndex}.");
        }
        TaskbarInfo? applied = GetTaskbars().FirstOrDefault(taskbar => taskbar.MonitorIndex == monitorIndex);
        if (applied == null || applied.Position != position) {
            throw new InvalidOperationException($"Windows did not report taskbar position {position} on monitor {monitorIndex} after the request.");
        }
    }

    internal static RECT CreateTaskbarBounds(RECT monitor, TaskbarPosition position, int width, int height) {
        return position switch {
            TaskbarPosition.Left => new RECT {
                Left = monitor.Left, Top = monitor.Top, Right = monitor.Left + width, Bottom = monitor.Bottom
            },
            TaskbarPosition.Top => new RECT {
                Left = monitor.Left, Top = monitor.Top, Right = monitor.Right, Bottom = monitor.Top + height
            },
            TaskbarPosition.Right => new RECT {
                Left = monitor.Right - width, Top = monitor.Top, Right = monitor.Right, Bottom = monitor.Bottom
            },
            TaskbarPosition.Bottom => new RECT {
                Left = monitor.Left, Top = monitor.Bottom - height, Right = monitor.Right, Bottom = monitor.Bottom
            },
            _ => throw new ArgumentOutOfRangeException(nameof(position))
        };
    }

    internal static void ConstrainTaskbarThickness(ref RECT bounds, TaskbarPosition position, int width, int height) {
        switch (position) {
            case TaskbarPosition.Left:
                bounds.Right = bounds.Left + width;
                break;
            case TaskbarPosition.Top:
                bounds.Bottom = bounds.Top + height;
                break;
            case TaskbarPosition.Right:
                bounds.Left = bounds.Right - width;
                break;
            case TaskbarPosition.Bottom:
                bounds.Top = bounds.Bottom - height;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(position));
        }
    }
}
