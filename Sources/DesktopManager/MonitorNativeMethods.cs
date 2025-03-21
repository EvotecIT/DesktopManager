using System.Text;
using System.Runtime.InteropServices;

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
    public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

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
    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    /// <summary>
    /// Indexes for GetWindowLong
    /// </summary>
    public const int GWL_STYLE = -16;

    /// <summary>
    /// Window style values
    /// </summary>
    public const int WS_MINIMIZE = 0x20000000;
    public const int WS_MAXIMIZE = 0x01000000;

    /// <summary>
    /// Window messages
    /// </summary>
    public const uint WM_CHAR = 0x0102;
    public const uint WM_PASTE = 0x0302;
    public const uint WM_ACTIVATE = 0x0006;
    public const uint WM_SETTEXT = 0x000C;
    public const uint WM_GETTEXT = 0x000D;
    public const uint WM_GETTEXTLENGTH = 0x000E;

    /// <summary>
    /// Input event types
    /// </summary>
    public const int INPUT_KEYBOARD = 1;

    /// <summary>
    /// Keyboard input flags
    /// </summary>
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_UNICODE = 0x0004;

    /// <summary>
    /// Virtual key codes
    /// </summary>
    public const ushort VK_CONTROL = 0x11;
    public const ushort VK_V = 0x56;

    /// <summary>
    /// Clipboard formats
    /// </summary>
    public const uint CF_UNICODETEXT = 13;

    /// <summary>
    /// Global memory flags
    /// </summary>
    public const uint GMEM_MOVEABLE = 0x0002;

    /// <summary>
    /// Opens the clipboard for examination and prevents other applications from modifying the clipboard content.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);

    /// <summary>
    /// Closes the clipboard.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool CloseClipboard();

    /// <summary>
    /// Empties the clipboard and frees handles to data in the clipboard.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool EmptyClipboard();

    /// <summary>
    /// Retrieves data from the clipboard in a specified format.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr GetClipboardData(uint uFormat);

    /// <summary>
    /// Places data on the clipboard in a specified clipboard format.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    /// <summary>
    /// Locks a global memory block and returns a pointer to it.
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalLock(IntPtr hMem);

    /// <summary>
    /// Decrements the lock count associated with a memory object that was allocated with GMEM_MOVEABLE.
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern bool GlobalUnlock(IntPtr hMem);

    /// <summary>
    /// Allocates the specified number of bytes from the heap.
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalAlloc(uint uFlags, uint dwBytes);

    /// <summary>
    /// Frees the specified global memory object and invalidates its handle.
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalFree(IntPtr hMem);

    /// <summary>
    /// Sends input to the system.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// Posts a message to the message queue of the specified window.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

    /// <summary>
    /// Sends a message to a window or windows.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// INPUT structure for SendInput
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT {
        public int type;
        public INPUTUNION u;
    }

    /// <summary>
    /// INPUTUNION structure for SendInput
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    /// <summary>
    /// KEYBDINPUT structure for SendInput
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Brings the thread that created the specified window into the foreground and activates the window.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Sets the keyboard focus to the specified window.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern IntPtr SetFocus(IntPtr hWnd);

    /// <summary>
    /// Attaches or detaches the input processing mechanism of one thread to that of another thread.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    /// <summary>
    /// Retrieves the thread identifier of the calling thread.
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    /// <summary>
    /// Gets the handle to the foreground window.
    /// </summary>
    /// <returns>The handle to the foreground window.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
}
