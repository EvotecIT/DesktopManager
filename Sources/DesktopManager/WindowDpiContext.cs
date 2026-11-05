using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Runs window geometry work in a temporary per-monitor-aware thread DPI context, so window rectangles,
/// monitor rectangles and DWM frame bounds are all physical pixels whatever the caller's DPI awareness.
/// </summary>
/// <remarks>
/// Only the calling thread changes, and only for the duration of the operation. Calls nest safely.
/// On Windows builds without thread DPI contexts the operation runs in the caller's context.
/// </remarks>
internal static class WindowDpiContext {
    // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE. Per-monitor V1 also supports Windows 10 1607 and Server 2016.
    private static readonly IntPtr PerMonitorAware = new(-3);

    internal static void Execute(Action operation) {
        Execute<object?>(() => {
            operation();
            return null;
        });
    }

    /// <param name="operation">The geometry work.</param>
    /// <param name="discard">Releases a result that cannot be returned because the caller's context could not be restored.</param>
    internal static T Execute<T>(Func<T> operation, Action<T>? discard = null) {
        IntPtr previous;
        try {
            previous = MonitorNativeMethods.SetThreadDpiAwarenessContext(PerMonitorAware);
        } catch (EntryPointNotFoundException) {
            return operation();
        }
        if (previous == IntPtr.Zero) {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enter a per-monitor DPI context for window geometry.");
        }

        T result = default!;
        bool completed = false;
        try {
            result = operation();
            completed = true;
            return result;
        } finally {
            if (MonitorNativeMethods.SetThreadDpiAwarenessContext(previous) == IntPtr.Zero) {
                int error = Marshal.GetLastWin32Error();
                if (completed) {
                    discard?.Invoke(result);
                    throw new Win32Exception(error, "Could not restore the calling thread's DPI context after a window operation.");
                }
                DesktopManagerDiagnostics.Report($"Could not restore the calling thread's DPI context after a failed window operation: {error}");
            }
        }
    }
}
