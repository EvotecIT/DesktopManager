using System.Drawing;
using System.Drawing.Imaging;

namespace DesktopManager;

/// <summary>
/// Provides methods for capturing screenshots of the desktop.
/// </summary>
public static class ScreenshotService {
    /// <summary>
    /// Captures a screenshot of the entire virtual screen.
    /// </summary>
    /// <returns>A <see cref="Bitmap"/> containing the screenshot.</returns>
    public static Bitmap CaptureScreen() {
        int width = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CXVIRTUALSCREEN);
        int height = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CYVIRTUALSCREEN);
        int left = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_XVIRTUALSCREEN);
        int top = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_YVIRTUALSCREEN);

        Bitmap bitmap = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(left, top, 0, 0, new Size(width, height));
        return bitmap;
    }
}
