namespace DesktopManager;

/// <summary>Windows boundary for snapshot enumeration and thread-local DPI queries.</summary>
internal class MonitorSnapshotNative {
    internal virtual IntPtr SetThreadDpiAwarenessContext(IntPtr context) {
        try {
            return MonitorNativeMethods.SetThreadDpiAwarenessContext(context);
        } catch (EntryPointNotFoundException ex) {
            throw new PlatformNotSupportedException("Monitor snapshots require Windows 10 version 1607 or Windows Server 2016 or later.", ex);
        } catch (DllNotFoundException ex) {
            throw new PlatformNotSupportedException("Monitor snapshots require Windows.", ex);
        }
    }

    internal virtual List<MonitorService.DisplayPathSnapshot> EnumerateDisplayPaths() {
        return MonitorService.EnumerateDisplayPaths();
    }

    internal virtual bool TryGetDpi(RECT bounds, out uint dpiX, out uint dpiY) {
        dpiX = dpiY = 0;
        if (bounds.Right <= bounds.Left || bounds.Bottom <= bounds.Top) {
            return false;
        }

        try {
            IntPtr monitor = MonitorNativeMethods.MonitorFromRect(ref bounds, MonitorNativeMethods.MONITOR_DEFAULTTONULL);
            return monitor != IntPtr.Zero &&
                MonitorNativeMethods.GetDpiForMonitor(monitor, 0, out dpiX, out dpiY) == 0 && dpiX > 0 && dpiY > 0;
        } catch (EntryPointNotFoundException) {
            return false;
        } catch (DllNotFoundException) {
            return false;
        }
    }
}
