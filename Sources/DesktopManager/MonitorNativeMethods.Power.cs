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
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    /// <summary>Point structure.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateWindowExW(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    public static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    public static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    public const int GWLP_WNDPROC = -4;
    public const uint WM_QUIT = 0x0012;
}


