using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Native window-related platform invocations.
/// </summary>
public static partial class MonitorNativeMethods
{
    /// <summary>
    /// Gets the shell window handle.
    /// </summary>
    /// <returns>The handle of the shell window.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetShellWindow();

    /// <summary>
    /// Callback invoked for each top‑level window during enumeration.
    /// </summary>
    /// <param name="hWnd">The handle to the window.</param>
    /// <param name="lParam">Application-defined value passed from <see cref="EnumWindows"/>.</param>
    /// <returns><c>true</c> to continue enumeration; otherwise <c>false</c>.</returns>
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>
    /// Enumerates all top-level windows.
    /// </summary>
    /// <param name="enumFunc">The callback function to invoke for each window.</param>
    /// <param name="lParam">Application-defined value to pass to the callback function.</param>
    /// <returns><c>true</c> if the enumeration completes; otherwise <c>false</c>.</returns>
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
    /// Sets the transparency attributes of a layered window.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="crKey">Transparency color key.</param>
    /// <param name="bAlpha">Alpha value.</param>
    /// <param name="dwFlags">Layered window attributes flags.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    /// <summary>
    /// Retrieves the transparency attributes of a layered window.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="pcrKey">Transparency color key.</param>
    /// <param name="pbAlpha">Alpha value.</param>
    /// <param name="pdwFlags">Layered window attributes flags.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetLayeredWindowAttributes(IntPtr hWnd, out uint pcrKey, out byte pbAlpha, out uint pdwFlags);

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
    /// Sends a message with a timeout.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="Msg">Message identifier.</param>
    /// <param name="wParam">First message parameter.</param>
    /// <param name="lParam">Second message parameter.</param>
    /// <param name="fuFlags">Timeout flags.</param>
    /// <param name="uTimeout">Timeout in milliseconds.</param>
    /// <param name="lpdwResult">Result of the message.</param>
    /// <returns>Pointer to the result.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    /// <summary>
    /// Sends a message with a timeout using a string buffer parameter.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        StringBuilder lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    /// <summary>
    /// Sends simulated input events to the system.
    /// </summary>
    /// <param name="nInputs">The number of structures in the array.</param>
    /// <param name="pInputs">Array of <see cref="INPUT"/> structures.</param>
    /// <param name="cbSize">Size of an <see cref="INPUT"/> structure.</param>
    /// <returns>The number of events inserted.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// Opens the clipboard for modification.
    /// </summary>
    /// <param name="hWndNewOwner">Handle to new clipboard owner.</param>
    /// <returns>True if the clipboard was opened.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);

    /// <summary>
    /// Closes the clipboard.
    /// </summary>
    /// <returns>True if the clipboard was closed.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool CloseClipboard();

    /// <summary>
    /// Empties the clipboard.
    /// </summary>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EmptyClipboard();

    /// <summary>
    /// Places data on the clipboard.
    /// </summary>
    /// <param name="uFormat">Clipboard format.</param>
    /// <param name="hMem">Handle to the data.</param>
    /// <returns>Handle to the data on success.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    /// <summary>
    /// Retrieves data from the clipboard.
    /// </summary>
    /// <param name="uFormat">Clipboard format.</param>
    /// <returns>Handle to the data.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetClipboardData(uint uFormat);

    /// <summary>
    /// Allocates global memory.
    /// </summary>
    /// <param name="uFlags">Allocation flags.</param>
    /// <param name="dwBytes">Number of bytes.</param>
    /// <returns>Handle to the allocated memory.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    /// <summary>
    /// Locks a global memory block and returns a pointer to it.
    /// </summary>
    /// <param name="hMem">Handle to the memory.</param>
    /// <returns>Pointer to the locked memory.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalLock(IntPtr hMem);

    /// <summary>
    /// Unlocks a global memory block.
    /// </summary>
    /// <param name="hMem">Handle to the memory.</param>
    /// <returns>True if successful.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GlobalUnlock(IntPtr hMem);

    /// <summary>
    /// Frees a global memory block.
    /// </summary>
    /// <param name="hMem">Handle to the memory.</param>
    /// <returns>Handle to the memory.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalFree(IntPtr hMem);

    /// <summary>
    /// Determines whether the specified process is running under WOW64.
    /// </summary>
    /// <param name="hProcess">Process handle.</param>
    /// <param name="wow64Process">True if the process is WOW64.</param>
    /// <returns>True on success.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

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
    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);
    }

    /// <summary>
    /// Index for retrieving the window style via <see cref="GetWindowLongPtr"/>.
    /// </summary>
    public const int GWL_STYLE = -16;

    /// <summary>
    /// Index for retrieving the extended window style via <see cref="GetWindowLongPtr"/>.
    /// </summary>
    public const int GWL_EXSTYLE = -20;

    /// <summary>
    /// Window style value that indicates the window is minimized.
    /// </summary>
    public const int WS_MINIMIZE = 0x20000000;

    /// <summary>
    /// Window style value that indicates the window is maximized.
    /// </summary>
    public const int WS_MAXIMIZE = 0x01000000;

    /// <summary>
    /// Extended window style that marks a window as topmost.
    /// </summary>
    public const int WS_EX_TOPMOST = 0x00000008;

    /// <summary>
    /// Extended window style enabling layered window attributes.
    /// </summary>
    public const int WS_EX_LAYERED = 0x00080000;

    /// <summary>
    /// Layered window attribute flag for alpha values.
    /// </summary>
    public const uint LWA_ALPHA = 0x00000002;

    /// <summary>
    /// Handle used with <see cref="SetWindowPos"/> to place a window above all non-topmost windows.
    /// </summary>
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    /// <summary>
    /// Handle used with <see cref="SetWindowPos"/> to place a window above other windows without making it topmost.
    /// </summary>
    public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    /// <summary>
    /// Window position flag that retains the current Z order.
    /// </summary>
    public const int SWP_NOZORDER = 0x0004;

    /// <summary>
    /// Window position flag that retains the current size.
    /// </summary>
    public const int SWP_NOSIZE = 0x0001;

    /// <summary>
    /// Retrieves the specified system metric or system configuration setting.
    /// </summary>
    /// <param name="nIndex">The system metric to be retrieved.</param>
    /// <returns>The requested system metric value.</returns>
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    /// <summary>
    /// System metric index for the virtual screen X coordinate.
    /// </summary>
    public const int SM_XVIRTUALSCREEN = 76;

    /// <summary>
    /// System metric index for the virtual screen Y coordinate.
    /// </summary>
    public const int SM_YVIRTUALSCREEN = 77;

    /// <summary>
    /// System metric index for the virtual screen width.
    /// </summary>
    public const int SM_CXVIRTUALSCREEN = 78;

    /// <summary>
    /// System metric index for the virtual screen height.
    /// </summary>
    public const int SM_CYVIRTUALSCREEN = 79;

    /// <summary>
    /// Broadcast handle used with window messages.
    /// </summary>
    public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

    /// <summary>
    /// Message sent when a system-wide setting changes.
    /// </summary>
    public const uint WM_SETTINGCHANGE = 0x001A;

    /// <summary>
    /// Retrieves text from a window.
    /// </summary>
    public const uint WM_GETTEXT = 0x000D;

    /// <summary>
    /// Message used to paste data from the clipboard.
    /// </summary>
    public const uint WM_PASTE = 0x0302;

    /// <summary>
    /// Message used for key down events.
    /// </summary>
    public const uint WM_KEYDOWN = 0x0100;

    /// <summary>
    /// Message used for key up events.
    /// </summary>
    public const uint WM_KEYUP = 0x0101;

    /// <summary>
    /// Message used for character input events.
    /// </summary>
    public const uint WM_CHAR = 0x0102;

    /// <summary>
    /// Clipboard format for Unicode text.
    /// </summary>
    public const uint CF_UNICODETEXT = 13;

    /// <summary>
    /// Button message to programmatically click a control.
    /// </summary>
    public const uint BM_CLICK = 0x00F5;

    /// <summary>
    /// Button message to retrieve check state.
    /// </summary>
    public const uint BM_GETCHECK = 0x00F0;

    /// <summary>
    /// Button message to set check state.
    /// </summary>
    public const uint BM_SETCHECK = 0x00F1;
  
    /// <summary>
    /// SendMessageTimeout flag that aborts if the target window is hung.
    /// </summary>
    public const uint SMTO_ABORTIFHUNG = 0x0002;

    /// <summary>
    /// Memory allocation flag for movable memory.
    /// </summary>
    public const uint GMEM_MOVEABLE = 0x0002;

    /// <summary>
    /// Input type constant indicating mouse input.
    /// </summary>
    public const uint INPUT_MOUSE = 0;

    /// <summary>
    /// Input type constant indicating keyboard input.
    /// </summary>
    public const uint INPUT_KEYBOARD = 1;

    /// <summary>
    /// Mouse event flag for movement.
    /// </summary>
    public const uint MOUSEEVENTF_MOVE = 0x0001;

    /// <summary>
    /// Mouse event flag for left button press.
    /// </summary>
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;

    /// <summary>
    /// Mouse event flag for left button release.
    /// </summary>
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;

    /// <summary>
    /// Mouse event flag for right button press.
    /// </summary>
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;

    /// <summary>
    /// Mouse event flag for right button release.
    /// </summary>
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    /// <summary>
    /// Mouse event flag for vertical scrolling.
    /// </summary>
    public const uint MOUSEEVENTF_WHEEL = 0x0800;

    /// <summary>
    /// Mouse event flag indicating absolute coordinates.
    /// </summary>
    public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    /// <summary>
    /// Key event flag indicating key release.
    /// </summary>
    public const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>
    /// Key event flag indicating Unicode scan code.
    /// </summary>
    public const uint KEYEVENTF_UNICODE = 0x0004;

    /// <summary>
    /// Represents an INPUT structure used with <see cref="SendInput"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT {
        /// <summary>Type of the input event.</summary>
        public uint Type;
        /// <summary>Input data.</summary>
        public InputUnion Data;
    }

    /// <summary>
    /// Union representing keyboard, mouse or hardware input data.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion {
        /// <summary>Keyboard input data.</summary>
        [FieldOffset(0)] public KEYBDINPUT Keyboard;
        /// <summary>Mouse input data.</summary>
        [FieldOffset(0)] public MOUSEINPUT Mouse;
    }

    /// <summary>
    /// Defines mouse input for <see cref="SendInput"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT {
        /// <summary>Absolute or relative X coordinate.</summary>
        public int Dx;
        /// <summary>Absolute or relative Y coordinate.</summary>
        public int Dy;
        /// <summary>Mouse-specific data such as wheel movement.</summary>
        public uint MouseData;
        /// <summary>Flags specifying various aspects of mouse event.</summary>
        public uint DwFlags;
        /// <summary>Event timestamp.</summary>
        public uint Time;
        /// <summary>Additional information associated with the mouse event.</summary>
        public IntPtr ExtraInfo;
    }

    /// <summary>
    /// Defines keyboard input for <see cref="SendInput"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT {
        /// <summary>Virtual key code.</summary>
        public ushort Vk;
        /// <summary>Hardware scan code.</summary>
        public ushort Scan;
        /// <summary>Flags specifying various aspects of keystroke.</summary>
        public uint Flags;
        /// <summary>Event timestamp.</summary>
        public uint Time;
        /// <summary>Additional information associated with the keystroke.</summary>
        public IntPtr ExtraInfo;
    }

    /// <summary>
    /// Delegate for WinEvent callbacks.
    /// </summary>
    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    /// <summary>Hook flag to receive events out of context.</summary>
    public const uint WINEVENT_OUTOFCONTEXT = 0;

    /// <summary>Event fired when a window move/size operation ends.</summary>
    public const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;

    /// <summary>Installs an event hook.</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    /// <summary>Removes an event hook.</summary>
    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);
}
