using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DesktopManager;

public partial class WindowManager {
    private const int ErrorAccessDenied = 5;
    private const int ErrorInvalidWindowHandle = 1400;
    private const int ErrorTimeout = 1460;

    private static void SetWindowPosOrThrow(IntPtr handle, IntPtr insertAfter, int left, int top, int width, int height, int flags, string operation) {
        if (!MonitorNativeMethods.SetWindowPos(handle, insertAfter, left, top, width, height, flags)) {
            throw CreateWindowOperationException(handle, operation);
        }
    }

    /// <summary>Builds the failure for a window call that just failed, reading its Win32 error first.</summary>
    private static InvalidOperationException CreateWindowOperationException(IntPtr handle, string operation) {
        int error = Marshal.GetLastWin32Error();
        return CreateWindowOperationException(handle, operation, error);
    }

    private static InvalidOperationException CreateWindowOperationException(IntPtr handle, string operation, int error) {
        MonitorNativeMethods.GetWindowThreadProcessId(handle, out uint processId);
        bool? targetElevated = processId == 0 ? null : TryGetProcessElevation(processId);
        string message = DescribeWindowOperationFailure(operation, error, targetElevated, PrivilegeChecker.IsElevated);
        return new InvalidOperationException(message, new Win32Exception(error));
    }

    /// <summary>
    /// Explains a failed window call. Windows blocks a non-elevated process from changing an elevated
    /// process's windows (User Interface Privilege Isolation), which otherwise shows up as a bare
    /// access-denied error or a message that seems to time out.
    /// </summary>
    internal static string DescribeWindowOperationFailure(string operation, int error, bool? targetElevated, bool callerElevated) {
        if (!callerElevated && (targetElevated == true || error == ErrorAccessDenied)) {
            string owner = targetElevated == true
                ? "the window belongs to an elevated (administrator) process"
                : "Windows denied access to the window, which usually means it belongs to an elevated (administrator) process";
            return $"{operation} failed: {owner} and this process is not elevated. Run elevated to manage it (Win32 error {error}).";
        }

        if (error == 0) {
            return $"{operation} failed; Windows reported no error code.";
        }

        return $"{operation} failed: {new Win32Exception(error).Message} (Win32 error {error}).";
    }
}
