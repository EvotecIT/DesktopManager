using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Native power management related platform invocations.
/// </summary>
public static partial class MonitorNativeMethods {
    /// <summary>Registers for power setting notifications.</summary>
    /// <param name="hRecipient">Window handle receiving notifications.</param>
    /// <param name="powerSettingGuid">Power setting GUID.</param>
    /// <param name="flags">Notification flags.</param>
    /// <returns>Notification handle.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid powerSettingGuid, uint flags);

    /// <summary>Unregisters power setting notifications.</summary>
    /// <param name="handle">Notification handle.</param>
    /// <returns><c>true</c> if successful.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterPowerSettingNotification(IntPtr handle);

    /// <summary>Notification is sent to a window handle.</summary>
    public const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

    /// <summary>Indicates a power setting change.</summary>
    public const int PBT_POWERSETTINGCHANGE = 0x8013;

    /// <summary>Parent handle for message-only windows.</summary>
    public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

    /// <summary>
    /// Data structure for power broadcast settings.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POWERBROADCAST_SETTING {
        /// <summary>Power setting GUID.</summary>
        public Guid PowerSetting;
        /// <summary>Length of the data in bytes.</summary>
        public uint DataLength;
        /// <summary>Data value.</summary>
        public int Data;
    }

    /// <summary>Window procedure delegate.</summary>
    public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>Message structure.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MSG {
        /// <summary>Window handle.</summary>
        public IntPtr hwnd;

        /// <summary>Message identifier.</summary>
        public uint message;

        /// <summary>Additional information.</summary>
        public IntPtr wParam;

        /// <summary>Additional information.</summary>
        public IntPtr lParam;

        /// <summary>Timestamp for this message.</summary>
        public uint time;

        /// <summary>Cursor position.</summary>
        public POINT pt;
    }

    /// <summary>Point structure.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        /// <summary>X coordinate.</summary>
        public int x;

        /// <summary>Y coordinate.</summary>
        public int y;
    }

    /// <summary>
    /// Creates a window with an extended style.
    /// </summary>
    /// <param name="dwExStyle">The extended window style.</param>
    /// <param name="lpClassName">Window class name.</param>
    /// <param name="lpWindowName">Window caption.</param>
    /// <param name="dwStyle">The window style.</param>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="nWidth">Window width.</param>
    /// <param name="nHeight">Window height.</param>
    /// <param name="hWndParent">Parent window handle.</param>
    /// <param name="hMenu">Menu handle.</param>
    /// <param name="hInstance">Module instance.</param>
    /// <param name="lpParam">Additional parameters.</param>
    /// <returns>Handle to the created window.</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateWindowExW(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    /// <summary>Destroys the specified window.</summary>
    /// <param name="hWnd">Window handle.</param>
    /// <returns>True if the window was destroyed.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyWindow(IntPtr hWnd);

    /// <summary>
    /// Calls the default window procedure.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="msg">Message identifier.</param>
    /// <param name="wParam">Additional information.</param>
    /// <param name="lParam">Additional information.</param>
    /// <returns>Result of message processing.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Retrieves a message from the calling thread's message queue.
    /// </summary>
    /// <param name="lpMsg">Message structure.</param>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="wMsgFilterMin">First message to retrieve.</param>
    /// <param name="wMsgFilterMax">Last message to retrieve.</param>
    /// <returns>Non-zero if a message other than WM_QUIT is retrieved.</returns>
    [DllImport("user32.dll")]
    public static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    /// <summary>
    /// Translates virtual-key messages.
    /// </summary>
    /// <param name="lpMsg">Message to translate.</param>
    /// <returns>True if the message was translated.</returns>
    [DllImport("user32.dll")]
    public static extern bool TranslateMessage(ref MSG lpMsg);

    /// <summary>
    /// Dispatches a message to a window procedure.
    /// </summary>
    /// <param name="lpMsg">Message to dispatch.</param>
    /// <returns>Result of message processing.</returns>
    [DllImport("user32.dll")]
    public static extern IntPtr DispatchMessage(ref MSG lpMsg);

    /// <summary>
    /// Changes an attribute of the specified window.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="nIndex">Zero-based offset to the value.</param>
    /// <param name="dwNewLong">The replacement value.</param>
    /// <returns>The previous value of the specified attribute.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    /// <summary>Offset to retrieve the window procedure.</summary>
    public const int GWLP_WNDPROC = -4;

    /// <summary>Message posted to quit the message loop.</summary>
    public const uint WM_QUIT = 0x0012;
}

