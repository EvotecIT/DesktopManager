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
    /// Gets the active keyboard layout for a thread.
    /// </summary>
    /// <param name="idThread">The thread identifier.</param>
    /// <returns>The keyboard layout handle.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    /// <summary>
    /// Translates a character to the virtual key and modifier state for a keyboard layout.
    /// </summary>
    /// <param name="ch">The character to translate.</param>
    /// <param name="dwhkl">The keyboard layout handle.</param>
    /// <returns>A packed virtual-key and modifier-state result, or -1 when no mapping exists.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

    /// <summary>
    /// Maps a virtual key to a scan code.
    /// </summary>
    /// <param name="uCode">The value to translate.</param>
    /// <param name="uMapType">Translation mode.</param>
    /// <returns>The translated scan code.</returns>
    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    /// <summary>
    /// Checks if a window is visible.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <returns>True if the window is visible.</returns>
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>
    /// Checks whether a window/control is enabled.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <returns>True when the target is enabled.</returns>
    [DllImport("user32.dll")]
    public static extern bool IsWindowEnabled(IntPtr hWnd);

    /// <summary>
    /// Enables or disables a window/control.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="bEnable">True to enable; false to disable.</param>
    /// <returns>True when the target was previously disabled.</returns>
    [DllImport("user32.dll")]
    public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    /// <summary>
    /// Retrieves a handle to the parent of the specified window.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <returns>Handle to the parent window.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetParent(IntPtr hWnd);

    /// <summary>
    /// Retrieves a handle to a window with a specified relationship.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="uCmd">Relationship command.</param>
    /// <returns>Handle to the related window.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    /// <summary>
    /// Command for retrieving the owner window.
    /// </summary>
    public const uint GW_OWNER = 4;

    /// <summary>
    /// Retrieves a specified attribute value for a window.
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    /// <param name="dwAttribute">Attribute identifier.</param>
    /// <param name="pvAttribute">Receives the attribute value.</param>
    /// <param name="cbAttribute">Size of the attribute value.</param>
    /// <returns>HRESULT return code.</returns>
    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    /// <summary>
    /// Attribute identifier for cloaked windows.
    /// </summary>
    public const int DWMWA_CLOAKED = 14;

    /// <summary>
    /// Attempts to determine whether a window is cloaked by DWM.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="isCloaked">True if cloaked.</param>
    /// <returns>True if the attribute was retrieved; otherwise false.</returns>
    public static bool TryGetWindowCloaked(IntPtr hWnd, out bool isCloaked) {
        isCloaked = false;
        try {
            int cloaked = 0;
            int hr = DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out cloaked, sizeof(int));
            if (hr != 0) {
                return false;
            }
            isCloaked = cloaked != 0;
            return true;
        } catch (DllNotFoundException) {
            return false;
        } catch (EntryPointNotFoundException) {
            return false;
        }
    }

    /// <summary>
    /// Gets the window rectangle.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="lpRect">Receives the window rectangle.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// Copies a visual representation of the specified window into the provided device context.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="hdcBlt">Destination device context.</param>
    /// <param name="nFlags">Rendering flags.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    /// <summary>
    /// Gets the client rectangle of a window.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="lpRect">Receives the client rectangle.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// Converts a client-area point to screen coordinates.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="lpPoint">Point to convert.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    /// <summary>
    /// PrintWindow flag to capture only the client area.
    /// </summary>
    public const uint PW_CLIENTONLY = 0x00000001;

    /// <summary>
    /// PrintWindow flag to request full content rendering where supported.
    /// </summary>
    public const uint PW_RENDERFULLCONTENT = 0x00000002;

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
    /// Activates the specified window.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <returns>The previously active window.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr SetActiveWindow(IntPtr hWnd);

    /// <summary>
    /// Brings the specified window to the top of the Z order.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    /// <summary>
    /// Gets the handle of the foreground window.
    /// </summary>
    /// <returns>The foreground window handle.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Retrieves information about the active window and focused control for a GUI thread.
    /// </summary>
    /// <param name="idThread">The GUI thread identifier.</param>
    /// <param name="threadInfo">Receives the GUI thread information.</param>
    /// <returns>True when information is available; otherwise false.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO threadInfo);

    /// <summary>
    /// Retrieves the identifier of the calling thread.
    /// </summary>
    /// <returns>The current thread identifier.</returns>
    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    /// <summary>
    /// Describes the active window and caret state for a GUI thread.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GUITHREADINFO
    {
        /// <summary>Structure size.</summary>
        public int cbSize;

        /// <summary>Thread state flags.</summary>
        public uint flags;

        /// <summary>Active window handle.</summary>
        public IntPtr hwndActive;

        /// <summary>Focused window handle.</summary>
        public IntPtr hwndFocus;

        /// <summary>Capture window handle.</summary>
        public IntPtr hwndCapture;

        /// <summary>Menu owner handle.</summary>
        public IntPtr hwndMenuOwner;

        /// <summary>Move/size window handle.</summary>
        public IntPtr hwndMoveSize;

        /// <summary>Caret window handle.</summary>
        public IntPtr hwndCaret;

        /// <summary>Caret rectangle.</summary>
        public GUIRECT rcCaret;
    }

    /// <summary>
    /// Describes a native rectangle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GUIRECT
    {
        /// <summary>Left edge.</summary>
        public int Left;

        /// <summary>Top edge.</summary>
        public int Top;

        /// <summary>Right edge.</summary>
        public int Right;

        /// <summary>Bottom edge.</summary>
        public int Bottom;
    }

    /// <summary>
    /// Attaches or detaches the input processing mechanism of one thread to that of another thread.
    /// </summary>
    /// <param name="idAttach">Thread identifier to attach or detach.</param>
    /// <param name="idAttachTo">Thread identifier to attach to or detach from.</param>
    /// <param name="fAttach">True to attach; false to detach.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    /// <summary>
    /// Determines whether a window is minimized.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <returns>True when minimized; otherwise false.</returns>
    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

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
    /// Sends a string message to a window.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

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
    /// Sends a message with a timeout using a string parameter.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        string lParam,
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
    /// Window position flag that prevents activation.
    /// </summary>
    public const int SWP_NOACTIVATE = 0x0010;

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
    /// Sets text of a window.
    /// </summary>
    public const uint WM_SETTEXT = 0x000C;

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
    /// Message sent when the left mouse button is pressed.
    /// </summary>
    public const uint WM_LBUTTONDOWN = 0x0201;

    /// <summary>
    /// Message sent when the left mouse button is released.
    /// </summary>
    public const uint WM_LBUTTONUP = 0x0202;

    /// <summary>
    /// Message sent when the right mouse button is pressed.
    /// </summary>
    public const uint WM_RBUTTONDOWN = 0x0204;

    /// <summary>
    /// Message sent when the right mouse button is released.
    /// </summary>
    public const uint WM_RBUTTONUP = 0x0205;

    /// <summary>
    /// Edit control message to set selection.
    /// </summary>
    public const uint EM_SETSEL = 0x00B1;

    /// <summary>
    /// Edit control message to replace selection.
    /// </summary>
    public const uint EM_REPLACESEL = 0x00C2;
    
    /// <summary>
    /// Gets the handle to the window that has the keyboard focus.
    /// </summary>
    /// <returns>Handle to the window with keyboard focus.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetFocus();
    
    /// <summary>
    /// Sets the keyboard focus to the specified window.
    /// </summary>
    /// <param name="hWnd">Handle to the window to receive focus.</param>
    /// <returns>Handle to the window that previously had focus.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr SetFocus(IntPtr hWnd);
    
    /// <summary>
    /// Posts a message to a window's message queue.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="Msg">Message to post.</param>
    /// <param name="wParam">First message parameter.</param>
    /// <param name="lParam">Second message parameter.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

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
    /// Key event flag indicating a hardware scan code.
    /// </summary>
    public const uint KEYEVENTF_SCANCODE = 0x0008;

    /// <summary>
    /// Key event flag indicating Unicode scan code.
    /// </summary>
    public const uint KEYEVENTF_UNICODE = 0x0004;

    /// <summary>
    /// Key state flag indicating the left mouse button is pressed.
    /// </summary>
    public const uint MK_LBUTTON = 0x0001;

    /// <summary>
    /// Key state flag indicating the right mouse button is pressed.
    /// </summary>
    public const uint MK_RBUTTON = 0x0002;

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
