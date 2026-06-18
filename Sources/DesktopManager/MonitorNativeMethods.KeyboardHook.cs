using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Native keyboard hook platform invocations.
/// </summary>
public static partial class MonitorNativeMethods
{
    /// <summary>
    /// Low-level keyboard hook identifier.
    /// </summary>
    public const int WH_KEYBOARD_LL = 13;

    /// <summary>
    /// Key down message.
    /// </summary>
    public const int WM_KEYDOWN_HOOK = 0x0100;

    /// <summary>
    /// Key up message.
    /// </summary>
    public const int WM_KEYUP_HOOK = 0x0101;

    /// <summary>
    /// System key down message.
    /// </summary>
    public const int WM_SYSKEYDOWN_HOOK = 0x0104;

    /// <summary>
    /// System key up message.
    /// </summary>
    public const int WM_SYSKEYUP_HOOK = 0x0105;

    /// <summary>
    /// Delegate invoked by a low-level keyboard hook.
    /// </summary>
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Information supplied to a low-level keyboard hook.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        /// <summary>Virtual key code.</summary>
        public uint vkCode;

        /// <summary>Hardware scan code.</summary>
        public uint scanCode;

        /// <summary>Event flags.</summary>
        public uint flags;

        /// <summary>Event timestamp.</summary>
        public uint time;

        /// <summary>Application-defined extra information.</summary>
        public IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Installs a Windows hook.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hmod, uint dwThreadId);

    /// <summary>
    /// Passes hook information to the next hook procedure.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Removes a Windows hook.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>
    /// Posts a message to a thread message queue.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Gets a module handle for the current process.
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);
}
