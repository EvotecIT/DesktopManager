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

    /// <summary>
    /// Gets at most the requested number of window-text characters.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <param name="maxLength">Maximum number of characters to return.</param>
    /// <param name="isTruncated">Whether the native text is longer than the returned value.</param>
    /// <returns>Bounded window text or an empty string.</returns>
    public static string GetWindowText(IntPtr handle, int maxLength, out bool isTruncated) {
        if (maxLength < 1 || maxLength > DesktopTextObservationOptions.MaximumTextLength) {
            throw new ArgumentOutOfRangeException(nameof(maxLength), $"maxLength must be between 1 and {DesktopTextObservationOptions.MaximumTextLength}.");
        }

        isTruncated = false;
        if (handle == IntPtr.Zero) {
            return string.Empty;
        }

        int reportedLength = MonitorNativeMethods.GetWindowTextLength(handle);
        int boundedLength = reportedLength > 0 ? Math.Min(reportedLength, maxLength) : maxLength;
        int capacity = boundedLength == int.MaxValue ? int.MaxValue : boundedLength + 1;
        var builder = new StringBuilder(capacity);
        int copied = MonitorNativeMethods.GetWindowText(handle, builder, builder.Capacity);
        string value = copied > 0
            ? builder.ToString()
            : GetViaMessage(handle, capacity);
        if (value.Length > maxLength) {
            value = value.Substring(0, maxLength);
        }

        isTruncated = reportedLength > maxLength || (reportedLength <= 0 && value.Length >= maxLength);
        return value;
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

        try {
            MonitorNativeMethods.GetWindowThreadProcessId(handle, out uint pid);
            using var target = Process.GetProcessById((int)pid);
            bool currentWow64 = false;
            bool targetWow64 = false;
            MonitorNativeMethods.IsWow64Process(Process.GetCurrentProcess().Handle, out currentWow64);
            
            // Try to get target process handle, but handle access denied gracefully
            try {
                MonitorNativeMethods.IsWow64Process(target.Handle, out targetWow64);
            } catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5) {
                // Access denied - assume no bitness mismatch to allow fallback to SendMessage
                return false;
            }
            
            return currentWow64 != targetWow64;
        } catch {
            // If we can't determine bitness, assume no mismatch
            return false;
        }
    }
}
