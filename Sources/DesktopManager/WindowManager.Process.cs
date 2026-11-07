using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Window process utilities.
/// </summary>
public partial class WindowManager {
    /// <summary>
    /// Retrieves process details for the specified window.
    /// </summary>
    /// <param name="windowInfo">The window information.</param>
    /// <returns>Process details for the window.</returns>
    public WindowProcessInfo GetWindowProcessInfo(WindowInfo windowInfo) {
        ValidateWindowInfo(windowInfo);
        return BuildWindowProcessInfo(windowInfo.Handle);
    }

    /// <summary>
    /// Retrieves process details for the owner of the specified window.
    /// </summary>
    /// <param name="windowInfo">The window information.</param>
    /// <returns>Process details for the owner window, or null if not owned.</returns>
    public WindowProcessInfo? GetOwnerProcessInfo(WindowInfo windowInfo) {
        ValidateWindowInfo(windowInfo);
        var ownerHandle = windowInfo.OwnerHandle;
        if (ownerHandle == IntPtr.Zero) {
            ownerHandle = MonitorNativeMethods.GetWindow(windowInfo.Handle, MonitorNativeMethods.GW_OWNER);
        }
        if (ownerHandle == IntPtr.Zero) {
            return null;
        }
        return BuildWindowProcessInfo(ownerHandle);
    }

    /// <summary>
    /// Retrieves the thread ID that owns the specified window.
    /// </summary>
    /// <param name="windowInfo">The window information.</param>
    /// <returns>The thread ID.</returns>
    public uint GetWindowThreadId(WindowInfo windowInfo) {
        ValidateWindowInfo(windowInfo);
        uint processId;
        return MonitorNativeMethods.GetWindowThreadProcessId(windowInfo.Handle, out processId);
    }

    /// <summary>
    /// Retrieves the module path for the process owning the specified window.
    /// </summary>
    /// <param name="windowInfo">The window information.</param>
    /// <returns>Process module path if available; otherwise null.</returns>
    public string? GetWindowModulePath(WindowInfo windowInfo) {
        return GetWindowProcessInfo(windowInfo).ProcessPath;
    }

    /// <summary>
    /// Determines whether the process owning the window is elevated.
    /// </summary>
    /// <param name="windowInfo">The window information.</param>
    /// <returns>True if elevated, false if not elevated, or null if unknown.</returns>
    public bool? IsWindowProcessElevated(WindowInfo windowInfo) {
        return GetWindowProcessInfo(windowInfo).IsElevated;
    }

    /// <summary>
    /// Determines whether the owner process is elevated.
    /// </summary>
    /// <param name="windowInfo">The window information.</param>
    /// <returns>True if elevated, false if not elevated, or null if unknown.</returns>
    public bool? IsOwnerProcessElevated(WindowInfo windowInfo) {
        var ownerInfo = GetOwnerProcessInfo(windowInfo);
        return ownerInfo?.IsElevated;
    }

    private static WindowProcessInfo BuildWindowProcessInfo(IntPtr handle) {
        uint processId;
        uint threadId = MonitorNativeMethods.GetWindowThreadProcessId(handle, out processId);
        return BuildWindowProcessInfo(processId, threadId);
    }

    private static WindowProcessInfo BuildWindowProcessInfo(uint processId, uint threadId) {
        var info = new WindowProcessInfo {
            ProcessId = processId,
            ThreadId = threadId
        };

        if (processId == 0) {
            return info;
        }

        PopulateProcessMetadata(processId, info);
        info.IsElevated = TryGetProcessElevation(processId);
        info.IsWow64 = TryGetProcessWow64(processId);

        return info;
    }

    private static void PopulateProcessMetadata(uint processId, WindowProcessInfo info) {
        info.ProcessPath = TryGetProcessPath(processId);

        try {
            using var process = Process.GetProcessById((int)processId);
            info.ProcessName = process.ProcessName ?? string.Empty;

            if (string.IsNullOrEmpty(info.ProcessPath)) {
                try {
                    info.ProcessPath = process.MainModule?.FileName;
                } catch {
                    // Ignore access denied for module path
                }
            }
        } catch {
            // Ignore process lookup failures
        }

        if (string.IsNullOrEmpty(info.ProcessName) && !string.IsNullOrWhiteSpace(info.ProcessPath)) {
            info.ProcessName = Path.GetFileNameWithoutExtension(info.ProcessPath) ?? string.Empty;
        }
    }

    internal static string? TryGetProcessPath(uint processId) {
        IntPtr handle = MonitorNativeMethods.OpenProcess(
            MonitorNativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
            false,
            processId);
        if (handle == IntPtr.Zero) {
            return null;
        }

        try {
            int capacity = 1024;
            var builder = new StringBuilder(capacity);
            if (MonitorNativeMethods.QueryFullProcessImageName(handle, 0, builder, ref capacity)) {
                return builder.ToString();
            }
        } finally {
            MonitorNativeMethods.CloseHandle(handle);
        }

        return null;
    }

    private static bool? TryGetProcessElevation(uint processId) {
        IntPtr processHandle = MonitorNativeMethods.OpenProcess(
            MonitorNativeMethods.PROCESS_QUERY_INFORMATION,
            false,
            processId);
        if (processHandle == IntPtr.Zero) {
            processHandle = MonitorNativeMethods.OpenProcess(
                MonitorNativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
                false,
                processId);
            if (processHandle == IntPtr.Zero) {
                return null;
            }
        }

        try {
            if (!MonitorNativeMethods.OpenProcessToken(processHandle, MonitorNativeMethods.TOKEN_QUERY, out var tokenHandle)) {
                return null;
            }

            try {
                int size = Marshal.SizeOf<MonitorNativeMethods.TOKEN_ELEVATION>();
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try {
                    if (!MonitorNativeMethods.GetTokenInformation(
                        tokenHandle,
                        MonitorNativeMethods.TOKEN_INFORMATION_CLASS.TokenElevation,
                        buffer,
                        size,
                        out _)) {
                        return null;
                    }

                    var elevation = Marshal.PtrToStructure<MonitorNativeMethods.TOKEN_ELEVATION>(buffer);
                    return elevation.TokenIsElevated != 0;
                } finally {
                    Marshal.FreeHGlobal(buffer);
                }
            } finally {
                MonitorNativeMethods.CloseHandle(tokenHandle);
            }
        } finally {
            MonitorNativeMethods.CloseHandle(processHandle);
        }
    }

    private static bool? TryGetProcessWow64(uint processId) {
        if (!Environment.Is64BitOperatingSystem) {
            return false;
        }

        IntPtr handle = MonitorNativeMethods.OpenProcess(
            MonitorNativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
            false,
            processId);
        if (handle == IntPtr.Zero) {
            return null;
        }

        try {
            bool isWow64;
            if (MonitorNativeMethods.IsWow64Process(handle, out isWow64)) {
                return isWow64;
            }
        } finally {
            MonitorNativeMethods.CloseHandle(handle);
        }

        return null;
    }
}
