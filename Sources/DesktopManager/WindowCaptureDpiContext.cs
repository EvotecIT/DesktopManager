using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>Keeps window capture geometry and pixels in the same physical coordinate space.</summary>
internal static class WindowCaptureDpiContext {
    internal static T Execute<T>(Func<T> capture) where T : class, IDisposable {
        IntPtr previous;
        try {
            // Per-monitor V1 also supports Windows 10 1607 and Server 2016.
            previous = MonitorNativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-3));
        } catch (EntryPointNotFoundException) {
            // Preserve capture support on older Windows without changing process awareness.
            return capture();
        }
        if (previous == IntPtr.Zero) { throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enter the window capture DPI context."); }
        T? result = null;
        bool completed = false;
        try {
            result = capture();
            completed = true;
            return result;
        } finally {
            if (MonitorNativeMethods.SetThreadDpiAwarenessContext(previous) == IntPtr.Zero) {
                int error = Marshal.GetLastWin32Error();
                if (completed) {
                    // A failure after capture must not leak the image that can no longer be returned.
                    result?.Dispose();
                    throw new Win32Exception(error, "Could not restore the calling thread's DPI context after window capture.");
                }
                DesktopManagerDiagnostics.Report($"Could not restore the calling thread's DPI context after a capture failure: {error}");
            }
        }
    }
}
