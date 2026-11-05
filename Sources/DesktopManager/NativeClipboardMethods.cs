using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>Clipboard listener and format declarations not already covered by <see cref="MonitorNativeMethods"/>.</summary>
internal static class NativeClipboardMethods {
    internal const uint WM_CLIPBOARDUPDATE = 0x031D;
    internal const uint CF_DIB = 8;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool AddClipboardFormatListener(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RemoveClipboardFormatListener(IntPtr window);

    [DllImport("user32.dll")]
    internal static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetClipboardOwner();

    [DllImport("user32.dll", EntryPoint = "RegisterClipboardFormatW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint RegisterClipboardFormat(string format);

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    internal static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
