using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Makes a window take clicks and touches without activating, so the window the user is working
/// in keeps focus. Suits touch strips, overlays and on-screen palettes.
/// </summary>
/// <remarks>
/// The styles alone do not stop every activation: a UI framework may still focus its window on
/// mouse-button messages. Such windows should also answer <c>WM_MOUSEACTIVATE</c> with
/// <c>MA_NOACTIVATE</c> and <c>WM_POINTERACTIVATE</c> with <c>PA_NOACTIVATE</c>, and may call
/// <see cref="RouteMouseThroughPointerMessages"/>.
/// </remarks>
[SupportedOSPlatform("windows")]
public static class NonActivatingWindow {
    /// <summary>
    /// Adds <c>WS_EX_NOACTIVATE</c>, and optionally <c>WS_EX_TOOLWINDOW</c> to keep the window
    /// out of the taskbar and Alt+Tab.
    /// </summary>
    /// <param name="windowHandle">The top-level window.</param>
    /// <param name="hideFromTaskSwitcher">True to also add <c>WS_EX_TOOLWINDOW</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="windowHandle"/> is zero.</exception>
    /// <exception cref="Win32Exception">Windows did not accept the style change.</exception>
    /// <remarks>Apply the tool-window style before the window is first shown for the taskbar to honour it.</remarks>
    public static void Apply(IntPtr windowHandle, bool hideFromTaskSwitcher = true) {
        long flags = MonitorNativeMethods.WS_EX_NOACTIVATE | (hideFromTaskSwitcher ? MonitorNativeMethods.WS_EX_TOOLWINDOW : 0);
        Update(windowHandle, flags, enable: true);
    }

    /// <summary>
    /// Removes <c>WS_EX_NOACTIVATE</c>, and optionally <c>WS_EX_TOOLWINDOW</c>, restoring ordinary activation.
    /// </summary>
    /// <param name="windowHandle">The top-level window.</param>
    /// <param name="showInTaskSwitcher">True to also remove <c>WS_EX_TOOLWINDOW</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="windowHandle"/> is zero.</exception>
    /// <exception cref="Win32Exception">Windows did not accept the style change.</exception>
    public static void Remove(IntPtr windowHandle, bool showInTaskSwitcher = true) {
        long flags = MonitorNativeMethods.WS_EX_NOACTIVATE | (showInTaskSwitcher ? MonitorNativeMethods.WS_EX_TOOLWINDOW : 0);
        Update(windowHandle, flags, enable: false);
    }

    /// <summary>Gets whether the window currently has <c>WS_EX_NOACTIVATE</c>.</summary>
    /// <param name="windowHandle">The window.</param>
    /// <returns>True when the window does not activate on click.</returns>
    /// <exception cref="ArgumentException"><paramref name="windowHandle"/> is zero.</exception>
    /// <exception cref="Win32Exception">The window's style could not be read.</exception>
    public static bool IsApplied(IntPtr windowHandle) {
        return (ReadExStyle(windowHandle) & MonitorNativeMethods.WS_EX_NOACTIVATE) != 0;
    }

    /// <summary>
    /// Delivers mouse input to this process as pointer messages, as touch and pen already are.
    /// </summary>
    /// <returns>True when Windows enabled it; false when it was already set for this process or is unavailable.</returns>
    /// <remarks>
    /// This is process-wide and cannot be undone. Call it before the first window is created. Pointer
    /// messages carry mouse input without the legacy button messages on which some UI frameworks
    /// focus their window, so a non-activating window can decline activation.
    /// </remarks>
    public static bool RouteMouseThroughPointerMessages() {
        try {
            return MonitorNativeMethods.EnableMouseInPointer(true);
        } catch (EntryPointNotFoundException) {
            return false;
        }
    }

    private static void Update(IntPtr windowHandle, long flags, bool enable) {
        long current = ReadExStyle(windowHandle);
        long style = enable ? current | flags : current & ~flags;
        if (style == current) {
            return;
        }

        MonitorNativeMethods.SetWindowLongPtr(windowHandle, MonitorNativeMethods.GWL_EXSTYLE, new IntPtr(IntPtr.Size == 8 ? style : unchecked((int)style)));
        int error = Marshal.GetLastWin32Error();
        if ((ReadExStyle(windowHandle) & flags) != (enable ? flags : 0)) {
            throw new Win32Exception(error, "Windows did not accept the window's activation style.");
        }
    }

    private static long ReadExStyle(IntPtr windowHandle) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("A window handle is required.", nameof(windowHandle));
        }

        if (!MonitorNativeMethods.TryGetWindowLongPtr(windowHandle, MonitorNativeMethods.GWL_EXSTYLE, out IntPtr value)) {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the window's extended style.");
        }

        return value.ToInt64();
    }
}
