using System.Text;

namespace DesktopManager;

/// <summary>
/// Provides native methods for interacting with display devices and settings.
/// </summary>
public static class MonitorNativeMethods {
    /// <summary>
    /// Enumerates the display settings for a specified display device.
    /// </summary>
    /// <param name="lpszDeviceName">The name of the display device.</param>
    /// <param name="iModeNum">The type of information to retrieve.</param>
    /// <param name="lpDevMode">A pointer to a <see cref="DEVMODE"/> structure that receives the information.</param>
    /// <returns>True if the function succeeds; otherwise, false.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    /// <summary>
    /// Enumerates the display devices that are currently available.
    /// </summary>
    /// <param name="lpDevice">The device name.</param>
    /// <param name="iDevNum">The index of the display device.</param>
    /// <param name="lpDisplayDevice">A pointer to a <see cref="DISPLAY_DEVICE"/> structure that receives the information.</param>
    /// <param name="dwFlags">Set this flag to 0.</param>
    /// <returns>True if the function succeeds; otherwise, false.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    /// <summary>
    /// Changes the settings of the specified display device to the specified graphics mode.
    /// </summary>
    /// <param name="lpszDeviceName">The name of the display device.</param>
    /// <param name="lpDevMode">A pointer to a <see cref="DEVMODE"/> structure that describes the new graphics mode.</param>
    /// <param name="hwnd">Reserved; must be IntPtr.Zero.</param>
    /// <param name="dwflags">Indicates how the graphics mode should be changed.</param>
    /// <param name="lParam">Reserved; must be IntPtr.Zero.</param>
    /// <returns>A <see cref="DisplayChangeConfirmation"/> value indicating the result of the operation.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern DisplayChangeConfirmation ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, ChangeDisplaySettingsFlags dwflags, IntPtr lParam);

    /// <summary>
    /// Changes the settings of the specified display device to the specified graphics mode.
    /// </summary>
    /// <param name="lpszDeviceName">The name of the display device.</param>
    /// <param name="lpDevMode">A pointer to a <see cref="DEVMODE"/> structure that describes the new graphics mode, or IntPtr.Zero.</param>
    /// <param name="hwnd">Reserved; must be IntPtr.Zero.</param>
    /// <param name="dwflags">Indicates how the graphics mode should be changed.</param>
    /// <param name="lParam">Reserved; must be IntPtr.Zero.</param>
    /// <returns>A <see cref="DisplayChangeConfirmation"/> value indicating the result of the operation.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    // A signature for ChangeDisplaySettingsEx with a DEVMODE struct as the second parameter won't allow you to pass in IntPtr.Zero, so create an overload
    public static extern DisplayChangeConfirmation ChangeDisplaySettingsEx(string lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, ChangeDisplaySettingsFlags dwflags, IntPtr lParam);

    /// <summary>
    /// Gets the shell window handle.
    /// </summary>
    /// <returns>The handle of the shell window.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetShellWindow();

    /// <summary>
    /// Enumerates all top-level windows.
    /// </summary>
    /// <param name="lpEnumFunc">The callback function to invoke for each window.</param>
    /// <param name="lParam">Application-defined value to pass to the callback function.</param>
    /// <returns>True if the enumeration completes, false if it was cancelled.</returns>
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);

    /// <summary>
    /// Gets the window text length.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <returns>The length of the window text.</returns>
    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    /// <summary>
    /// Gets the window text.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="lpString">The buffer to receive the text.</param>
    /// <param name="nMaxCount">The maximum number of characters to copy.</param>
    /// <returns>The number of characters copied.</returns>
    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    /// <summary>
    /// Gets the window thread process ID.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="lpdwProcessId">Receives the process ID.</param>
    /// <returns>The thread ID.</returns>
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Checks if a window is visible.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <returns>True if the window is visible.</returns>
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>
    /// Gets the window rectangle.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="lpRect">Receives the window rectangle.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// Sets the window position.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="hWndInsertAfter">The window to insert this window after.</param>
    /// <param name="X">The new X coordinate.</param>
    /// <param name="Y">The new Y coordinate.</param>
    /// <param name="cx">The new width.</param>
    /// <param name="cy">The new height.</param>
    /// <param name="uFlags">Window sizing and positioning flags.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

    /// <summary>
    /// Brings the specified window to the foreground.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Gets the handle of the foreground window.
    /// </summary>
    /// <returns>The foreground window handle.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Sends a message to a window.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="Msg">The message to send.</param>
    /// <param name="wParam">Additional parameter.</param>
    /// <param name="lParam">Additional parameter.</param>
    /// <returns>The result of processing the message.</returns>
    [DllImport("user32.dll")]
    public static extern uint SendMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

    /// <summary>
    /// Gets information about the specified window.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="nIndex">The zero-based offset to the value to be retrieved.</param>
    /// <returns>The requested value.</returns>
    // On 32-bit systems GetWindowLong is used while on 64-bit systems
    // the operating system exposes GetWindowLongPtr. Define both and
    // call the appropriate version at runtime.

    /// <summary>
    /// 32-bit variant of <c>GetWindowLongPtr</c>.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="nIndex">The value index to retrieve.</param>
    /// <returns>The requested value as a pointer.</returns>
    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    /// <summary>
    /// 64-bit variant of <c>GetWindowLongPtr</c>.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="nIndex">The value index to retrieve.</param>
    /// <returns>The requested value as a pointer.</returns>
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    /// <summary>
    /// Retrieves information about the specified window in a platform agnostic manner.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="nIndex">The zero-based offset to the value to be retrieved.</param>
    /// <returns>The requested value as a pointer.</returns>
    /// <remarks>
    /// When running under a 64-bit process, <see cref="GetWindowLongPtr64"/> is invoked.
    /// Otherwise <see cref="GetWindowLong32"/> is used. The caller should convert the
    /// returned <see cref="IntPtr"/> to the appropriate numeric type.
    /// </remarks>
    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : GetWindowLong32(hWnd, nIndex);
    }

    /// <summary>
    /// Indexes for GetWindowLong
    /// </summary>
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;

    /// <summary>
    /// Window style values
    /// </summary>
    public const int WS_MINIMIZE = 0x20000000;
    public const int WS_MAXIMIZE = 0x01000000;
    public const int WS_EX_TOPMOST = 0x00000008;

    /// <summary>
    /// Window insert after handles.
    /// </summary>
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    /// Window position flags
    /// </summary>
    public const int SWP_NOZORDER = 0x0004;
    public const int SWP_NOSIZE = 0x0001;

    /// <summary>
    /// Retrieves the specified system metric or system configuration setting.
    /// </summary>
    /// <param name="nIndex">The system metric to be retrieved.</param>
    /// <returns>The requested system metric value.</returns>
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    /// <summary>
    /// System metric indexes used with <see cref="GetSystemMetrics"/>.
    /// </summary>
    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;
}
