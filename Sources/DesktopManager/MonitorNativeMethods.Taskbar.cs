using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Native taskbar-related platform invocations.
/// </summary>
public static partial class MonitorNativeMethods {
    /// <summary>Finds a window by class name.</summary>
    /// <param name="lpClassName">Class name.</param>
    /// <param name="lpWindowName">Window title.</param>
    /// <returns>Window handle or IntPtr.Zero.</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    /// <summary>Retrieves the class name of a window.</summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="lpClassName">Buffer for the class name.</param>
    /// <param name="nMaxCount">Maximum characters.</param>
    /// <returns>Length of the class name.</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    /// <summary>Retrieves a handle to the display monitor from a window.</summary>
    /// <param name="hwnd">Window handle.</param>
    /// <param name="dwFlags">Monitor options.</param>
    /// <returns>The monitor handle.</returns>
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    /// <summary>Shows or hides a window.</summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="nCmdShow">Show command.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>Sends an appbar message.</summary>
    /// <param name="dwMessage">The message to send.</param>
    /// <param name="pData">Appbar data.</param>
    /// <returns>Result of the call.</returns>
    [DllImport("shell32.dll", SetLastError = true)]
    public static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    /// <summary>Hide window command.</summary>
    public const int SW_HIDE = 0;
    /// <summary>Show window command.</summary>
    public const int SW_SHOW = 5;

    /// <summary>Appbar message to set position.</summary>
    public const uint ABM_SETPOS = 0x00000003;
    /// <summary>Appbar message to query position.</summary>
    public const uint ABM_QUERYPOS = 0x00000002;
}

/// <summary>Data structure used with <c>SHAppBarMessage</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct APPBARDATA {
    /// <summary>Structure size.</summary>
    public uint cbSize;
    /// <summary>Window handle.</summary>
    public IntPtr hWnd;
    /// <summary>Callback message ID.</summary>
    public uint uCallbackMessage;
    /// <summary>Screen edge.</summary>
    public int uEdge;
    /// <summary>Bounding rectangle.</summary>
    public RECT rc;
    /// <summary>Custom parameter.</summary>
    public IntPtr lParam;
}
