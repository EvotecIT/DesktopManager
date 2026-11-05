using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Native declarations behind the cursor fence, window event watcher and window behaviour helpers.
/// </summary>
public static partial class MonitorNativeMethods {
    /// <summary>Low-level mouse hook identifier.</summary>
    internal const int WH_MOUSE_LL = 14;

    /// <summary>Mouse move message delivered to a low-level mouse hook.</summary>
    internal const int WM_MOUSEMOVE_HOOK = 0x0200;

    /// <summary>Virtual key code for either Control key.</summary>
    internal const int VK_CONTROL_KEY = 0x11;

    /// <summary>Skips WinEvents raised by threads of the calling process.</summary>
    internal const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    /// <summary>The foreground window changed.</summary>
    internal const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

    /// <summary>A window move or resize operation started.</summary>
    internal const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;

    /// <summary>An object was destroyed.</summary>
    internal const uint EVENT_OBJECT_DESTROY = 0x8001;

    /// <summary>A hidden object was shown.</summary>
    internal const uint EVENT_OBJECT_SHOW = 0x8002;

    /// <summary>An object changed location, shape or size.</summary>
    internal const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

    /// <summary>WinEvent object identifier for the window itself.</summary>
    internal const int OBJID_WINDOW = 0;

    /// <summary>WinEvent child identifier for the object itself.</summary>
    internal const int CHILDID_SELF = 0;

    /// <summary>Extended style that keeps a window from activating when clicked.</summary>
    internal const long WS_EX_NOACTIVATE = 0x08000000;

    /// <summary>Extended style that keeps a window out of the taskbar and Alt+Tab.</summary>
    internal const long WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PeekMessage(out MSG message, IntPtr window, uint filterMin, uint filterMax, uint remove);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnableMouseInPointer([MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport("shell32.dll", ExactSpelling = true)]
    internal static extern int SHGetPropertyStoreForWindow(IntPtr window, in Guid interfaceId, out IntPtr store);

}
