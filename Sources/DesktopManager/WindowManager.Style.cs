using System;

namespace DesktopManager;

public partial class WindowManager {
    /// <summary>
    /// Retrieves window style bits.
    /// </summary>
    /// <param name="windowInfo">Target window.</param>
    /// <param name="extended">True to get extended style.</param>
    /// <returns>Style flags as a long.</returns>
    public long GetWindowStyle(WindowInfo windowInfo, bool extended = false) {
        if (windowInfo.Handle == IntPtr.Zero) {
            throw new InvalidOperationException("Invalid window handle");
        }

        int index = extended ? MonitorNativeMethods.GWL_EXSTYLE : MonitorNativeMethods.GWL_STYLE;
        return MonitorNativeMethods.GetWindowLongPtr(windowInfo.Handle, index).ToInt64();
    }

    /// <summary>
    /// Adds or removes style bits on a window.
    /// </summary>
    /// <param name="windowInfo">Target window.</param>
    /// <param name="flags">Style flags to modify.</param>
    /// <param name="enable">True to set bits; false to clear them.</param>
    /// <param name="extended">True to modify extended style.</param>
    public void SetWindowStyle(WindowInfo windowInfo, long flags, bool enable, bool extended = false) {
        if (windowInfo.Handle == IntPtr.Zero) {
            throw new InvalidOperationException("Invalid window handle");
        }

        int index = extended ? MonitorNativeMethods.GWL_EXSTYLE : MonitorNativeMethods.GWL_STYLE;
        long style = GetWindowStyle(windowInfo, extended);
        long newStyle = enable ? (style | flags) : (style & ~flags);
        MonitorNativeMethods.SetWindowLongPtr(windowInfo.Handle, index, new IntPtr(newStyle));

        if (extended && (flags & MonitorNativeMethods.WS_EX_TOPMOST) != 0) {
            const int SWP_NOMOVE = 0x0002;
            const int SWP_NOSIZE = 0x0001;
            var insertAfter = enable ? MonitorNativeMethods.HWND_TOPMOST : MonitorNativeMethods.HWND_NOTOPMOST;
            MonitorNativeMethods.SetWindowPos(windowInfo.Handle, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
    }
}

