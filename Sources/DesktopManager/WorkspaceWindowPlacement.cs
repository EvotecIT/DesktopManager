using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DesktopManager;

internal static class WorkspaceWindowPlacement {
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { internal int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Placement {
        internal int Length, Flags, ShowCommand;
        internal Point Minimum, Maximum;
        internal RECT Normal;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(IntPtr window, ref Placement placement);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPlacement(IntPtr window, ref Placement placement);
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref RECT rectangle, uint flags);

    internal static void Capture(WindowPosition window) {
        Placement placement = Read(window.Handle);
        window.RestoreToMaximized = (placement.Flags & 2) != 0;
        if (window.State == WindowState.Minimize || window.State == WindowState.Maximize) {
            RECT normal = ConvertCoordinates(window.Handle, placement.Normal, toScreen: true);
            window.Left = normal.Left; window.Top = normal.Top; window.Right = normal.Right; window.Bottom = normal.Bottom;
        }
    }

    internal static WindowLayoutApplyResult Apply(WindowManager manager, WindowLayoutMatch match) {
        try {
            WindowPosition target = match.Window!;
            Placement placement = Read(target.Handle);
            placement.Normal = ConvertCoordinates(target.Handle, new RECT {
                Left = match.SavedWindow.Left, Top = match.SavedWindow.Top,
                Right = match.SavedWindow.Right, Bottom = match.SavedWindow.Bottom
            }, toScreen: false);
            placement.Flags = match.SavedWindow.RestoreToMaximized ? 2 : 0;
            placement.ShowCommand = match.SavedWindow.State == WindowState.Minimize ? 7 :
                match.SavedWindow.State == WindowState.Maximize ? 3 : 4;
            if (!SetWindowPlacement(target.Handle, ref placement)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            WindowPosition actual = manager.GetWindowPosition(target);
            Capture(actual);
            if (actual.Left != match.SavedWindow.Left || actual.Top != match.SavedWindow.Top ||
                    actual.Width != match.SavedWindow.Width || actual.Height != match.SavedWindow.Height ||
                    (match.SavedWindow.State.HasValue && actual.State != match.SavedWindow.State)) {
                throw new InvalidOperationException("The target did not accept the saved normal bounds and window state.");
            }
            return new WindowLayoutApplyResult(match, true, null);
        } catch (Exception ex) { return new WindowLayoutApplyResult(match, false, ex.Message); }
    }

    private static Placement Read(IntPtr window) {
        var result = new Placement { Length = Marshal.SizeOf<Placement>() };
        if (!GetWindowPlacement(window, ref result)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
        return result;
    }

    private static RECT ConvertCoordinates(IntPtr window, RECT rectangle, bool toScreen) {
        // WINDOWPLACEMENT uses workspace coordinates except for tool windows; persisted layouts use screen coordinates.
        if ((MonitorNativeMethods.GetWindowLongPtr(window, MonitorNativeMethods.GWL_EXSTYLE).ToInt64() & 0x80) != 0) { return rectangle; }
        IntPtr monitor = toScreen ? MonitorNativeMethods.MonitorFromWindow(window, MonitorNativeMethods.MONITOR_DEFAULTTONEAREST) :
            MonitorFromRect(ref rectangle, MonitorNativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        if (!MonitorNativeMethods.GetMonitorInfo(monitor, ref info)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
        int sign = toScreen ? 1 : -1;
        int x = sign * (info.rcWork.Left - info.rcMonitor.Left), y = sign * (info.rcWork.Top - info.rcMonitor.Top);
        rectangle.Left += x; rectangle.Right += x; rectangle.Top += y; rectangle.Bottom += y;
        return rectangle;
    }
}
