using System;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Provides helper methods for interacting with window controls.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowControlService {
    /// <summary>
    /// Clicks the specified control.
    /// </summary>
    /// <param name="control">Control to click.</param>
    /// <param name="button">Mouse button to use.</param>
    public static void ControlClick(WindowControlInfo control, MouseButton button) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        // Try to use BM_CLICK first
        MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.BM_CLICK, 0, 0);

        if (MonitorNativeMethods.GetWindowRect(control.Handle, out RECT rect)) {
            int x = (rect.Left + rect.Right) / 2;
            int y = (rect.Top + rect.Bottom) / 2;
            MouseInputService.MoveCursor(x, y);
            MouseInputService.Click(button);
        }
    }

    /// <summary>
    /// Retrieves the current text of the specified control.
    /// </summary>
    /// <param name="control">Control to query.</param>
    /// <returns>Control text.</returns>
    public static string GetControlText(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        int length = (int)MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.WM_GETTEXTLENGTH, 0u, 0u);
        if (length == 0) {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder(length + 1);
        MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.WM_GETTEXT, new IntPtr(sb.Capacity), sb);
        return sb.ToString();
    }

    /// <summary>
    /// Sets the text for the specified control.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="text">Text to assign.</param>
    public static void SetControlText(WindowControlInfo control, string text) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.WM_SETTEXT, IntPtr.Zero, text);
    }
}
