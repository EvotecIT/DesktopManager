using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>Keyboard layout, key state and hook declarations used by session typing.</summary>
internal static class SessionTypingNative {
    internal const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    internal const uint MAPVK_VK_TO_VSC_EX = 4;
    internal const uint KLF_NOTELLSHELL = 0x00000080;
    internal const uint ToUnicodeDoNotChangeKeyboardState = 0x4;
    internal const int WH_MOUSE_LL = 14;
    internal const uint LLKHF_INJECTED = 0x10;
    internal const uint LLMHF_INJECTED = 0x01;
    internal const int VK_CAPITAL = 0x14;

    internal const int WM_LBUTTONDOWN = 0x0201;
    internal const int WM_RBUTTONDOWN = 0x0204;
    internal const int WM_MBUTTONDOWN = 0x0207;
    internal const int WM_MOUSEWHEEL = 0x020A;
    internal const int WM_XBUTTONDOWN = 0x020B;
    internal const int WM_MOUSEHWHEEL = 0x020E;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT {
        internal int X;
        internal int Y;
        internal uint MouseData;
        internal uint Flags;
        internal uint Time;
        internal IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", EntryPoint = "LoadKeyboardLayoutW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr LoadKeyboardLayout(string keyboardLayoutId, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnloadKeyboardLayout(IntPtr keyboardLayout);

    [DllImport("user32.dll")]
    internal static extern int GetKeyboardLayoutList(int count, [Out] IntPtr[]? layouts);

    [DllImport("user32.dll", EntryPoint = "MapVirtualKeyExW")]
    internal static extern uint MapVirtualKeyEx(uint code, uint mapType, IntPtr keyboardLayout);

    [DllImport("user32.dll", EntryPoint = "ToUnicodeEx", CharSet = CharSet.Unicode)]
    internal static extern int ToUnicodeEx(uint virtualKey, uint scanCode, byte[] keyState, [Out] char[] buffer, int bufferLength, uint flags, IntPtr keyboardLayout);

    [DllImport("user32.dll")]
    internal static extern short GetKeyState(int virtualKey);

    [DllImport("kernel32.dll")]
    internal static extern IntPtr GetCurrentProcess();

    internal static IntPtr[] GetKeyboardLayouts() {
        int count = GetKeyboardLayoutList(0, null);
        if (count <= 0) {
            return Array.Empty<IntPtr>();
        }

        var layouts = new IntPtr[count];
        int written = GetKeyboardLayoutList(count, layouts);
        if (written < count) {
            Array.Resize(ref layouts, Math.Max(0, written));
        }

        return layouts;
    }
}
