using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Controls whether touch swipes in from a screen edge open Start, notifications or widgets
/// while a window fills its screen.
/// </summary>
/// <remarks>
/// A full-screen window on a touch display can keep edge swipes for itself, so a drag that starts
/// near an edge does not open shell UI by accident. Windows applies the setting only while the
/// window covers its whole monitor.
/// </remarks>
[SupportedOSPlatform("windows")]
public static class WindowEdgeGestures {
    private static readonly Guid PropertyStoreId = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");

    // PKEY_EdgeGesture_DisableTouchWhenFullscreen.
    private static readonly Guid EdgeGestureFormatId = new("32CE38B2-2C9A-41B1-9BC5-B3784394AA44");
    private const uint DisableTouchWhenFullscreen = 2;
    private const ushort VariantBool = 11;

    /// <summary>
    /// Keeps edge swipes on the window while it is full screen, or gives them back to Windows.
    /// </summary>
    /// <param name="windowHandle">The top-level window.</param>
    /// <param name="disabled">True to keep edge swipes on the window; false to restore the Windows behaviour.</param>
    /// <returns>True when Windows stored the setting; false when the window has no property store or refused it.</returns>
    /// <exception cref="ArgumentException"><paramref name="windowHandle"/> is zero.</exception>
    public static bool SetDisabled(IntPtr windowHandle, bool disabled) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("A window handle is required.", nameof(windowHandle));
        }

        if (MonitorNativeMethods.SHGetPropertyStoreForWindow(windowHandle, in PropertyStoreId, out IntPtr pointer) != 0 || pointer == IntPtr.Zero) {
            return false;
        }

        IWindowPropertyStore store;
        try {
            store = NativeComActivation.FromPointer<IWindowPropertyStore>(pointer);
        } finally {
            Marshal.Release(pointer);
        }

        try {
            var key = new WindowPropertyKey { FormatId = EdgeGestureFormatId, PropertyId = DisableTouchWhenFullscreen };
            // VARIANT_TRUE is -1; the low 16 bits are the VT_BOOL payload.
            var value = new WindowPropertyBoolValue { Type = VariantBool, Value = disabled ? -1 : 0 };
            return store.SetValue(in key, in value) == 0 && store.Commit() == 0;
        } finally {
            NativeComActivation.Release(store);
        }
    }
}
