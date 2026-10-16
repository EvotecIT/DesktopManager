namespace DesktopManager;

public static partial class MonitorNativeMethods {
    /// <summary>Changes only the calling thread's DPI context and returns its previous context, or zero on failure.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

    /// <summary>Gets a monitor intersecting a physical desktop rectangle.</summary>
    [DllImport("user32.dll")]
    internal static extern IntPtr MonitorFromRect(ref RECT rect, uint flags);

    /// <summary>Reads monitor DPI; dpiType zero requests effective DPI including display scaling.</summary>
    [DllImport("shcore.dll")]
    internal static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);
}
