using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Helper methods for working with the Windows clipboard.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ClipboardHelper {
    /// <summary>
    /// Places Unicode text on the clipboard.
    /// </summary>
    /// <param name="text">Text to place on the clipboard.</param>
    public static void SetText(string text) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        if (!MonitorNativeMethods.OpenClipboard(IntPtr.Zero)) {
            throw new InvalidOperationException("Unable to open clipboard.");
        }

        try {
            if (!MonitorNativeMethods.EmptyClipboard()) {
                throw new InvalidOperationException("Unable to empty clipboard.");
            }

            int bytes = (text.Length + 1) * 2;
            IntPtr hGlobal = MonitorNativeMethods.GlobalAlloc(MonitorNativeMethods.GMEM_MOVEABLE, (UIntPtr)bytes);
            if (hGlobal == IntPtr.Zero) {
                throw new InvalidOperationException("GlobalAlloc failed.");
            }

            IntPtr target = MonitorNativeMethods.GlobalLock(hGlobal);
            if (target == IntPtr.Zero) {
                MonitorNativeMethods.GlobalFree(hGlobal);
                throw new InvalidOperationException("GlobalLock failed.");
            }

            try {
                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                Marshal.WriteInt16(target, text.Length * 2, 0);
            } finally {
                MonitorNativeMethods.GlobalUnlock(hGlobal);
            }

            if (MonitorNativeMethods.SetClipboardData(MonitorNativeMethods.CF_UNICODETEXT, hGlobal) == IntPtr.Zero) {
                MonitorNativeMethods.GlobalFree(hGlobal);
                throw new InvalidOperationException("SetClipboardData failed.");
            }
        } finally {
            MonitorNativeMethods.CloseClipboard();
        }
    }
}

