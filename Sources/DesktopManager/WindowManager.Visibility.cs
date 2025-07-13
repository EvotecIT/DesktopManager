namespace DesktopManager;

public partial class WindowManager {
    /// <summary>
    /// Shows or hides the specified window using the Win32 ShowWindow API.
    /// </summary>
    /// <param name="window">Window to modify.</param>
    /// <param name="show">True to show the window; false to hide it.</param>
    public void ShowWindow(WindowInfo window, bool show) {
        if (window.Handle == IntPtr.Zero) {
            throw new InvalidOperationException("Invalid window handle");
        }

        int command = show ? MonitorNativeMethods.SW_SHOW : MonitorNativeMethods.SW_HIDE;
        MonitorNativeMethods.ShowWindow(window.Handle, command);
    }
}
