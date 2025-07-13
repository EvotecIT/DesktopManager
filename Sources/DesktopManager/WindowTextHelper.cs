using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Provides methods to retrieve window titles with cross-bitness fallback.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowTextHelper {
    /// <summary>
    /// Gets the window title for the specified handle.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <returns>Window title or empty string.</returns>
    public static string GetWindowText(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return string.Empty;
        }

        int length = MonitorNativeMethods.GetWindowTextLength(handle);
        if (length <= 0) {
            return GetViaMessage(handle, 256);
        }

        var sb = new StringBuilder(length + 1);
        if (MonitorNativeMethods.GetWindowText(handle, sb, sb.Capacity) > 0 && sb.Length == length) {
            return sb.ToString();
        }

        return GetViaMessage(handle, Math.Max(length + 1, 256));
    }

    private static string GetViaMessage(IntPtr handle, int capacity) {
        if (capacity <= 0) {
            capacity = 256;
        }

        if (BitnessMismatch(handle)) {
            IntPtr buffer = Marshal.AllocHGlobal(capacity * 2);
            try {
                IntPtr result;
                IntPtr res = MonitorNativeMethods.SendMessageTimeout(
                    handle,
                    MonitorNativeMethods.WM_GETTEXT,
                    new IntPtr(capacity),
                    buffer,
                    MonitorNativeMethods.SMTO_ABORTIFHUNG,
                    1000,
                    out result);
                if (res != IntPtr.Zero) {
                    return Marshal.PtrToStringUni(buffer) ?? string.Empty;
                }
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        } else {
            var sb = new StringBuilder(capacity);
            IntPtr result;
            IntPtr res = MonitorNativeMethods.SendMessageTimeout(
                handle,
                MonitorNativeMethods.WM_GETTEXT,
                new IntPtr(sb.Capacity),
                sb,
                MonitorNativeMethods.SMTO_ABORTIFHUNG,
                1000,
                out result);
            if (res != IntPtr.Zero) {
                return sb.ToString();
            }
        }

        return string.Empty;
    }

    private static bool BitnessMismatch(IntPtr handle) {
        if (!Environment.Is64BitOperatingSystem) {
            return false;
        }

        MonitorNativeMethods.GetWindowThreadProcessId(handle, out uint pid);
        using var target = Process.GetProcessById((int)pid);
        bool currentWow64 = false;
        bool targetWow64 = false;
        MonitorNativeMethods.IsWow64Process(Process.GetCurrentProcess().Handle, out currentWow64);
        MonitorNativeMethods.IsWow64Process(target.Handle, out targetWow64);
        return currentWow64 != targetWow64;
    }
}
