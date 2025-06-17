using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

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

        return CaptureRegion(left, top, width, height);
    }

    /// <summary>
    /// Captures a screenshot of the specified monitor by index.
    /// </summary>
    /// <param name="index">Monitor index starting at 0.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureMonitor(int index) {
        Monitors monitors = new();
        var monitor = monitors.GetMonitors(index: index).FirstOrDefault();
        if (monitor == null) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var rect = monitor.GetMonitorBounds();
        return CaptureRegion(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    /// <summary>
    /// Captures a screenshot of an arbitrary region of the desktop.
    /// </summary>
    /// <param name="region">Rectangle describing region to capture.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureRegion(Rectangle region) {
        return CaptureRegion(region.Left, region.Top, region.Width, region.Height);
    }

    /// <summary>
    /// Captures a screenshot of an arbitrary region.
    /// </summary>
    /// <param name="left">Left coordinate.</param>
    /// <param name="top">Top coordinate.</param>
    /// <param name="width">Width of the region.</param>
    /// <param name="height">Height of the region.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureRegion(int left, int top, int width, int height) {
        Bitmap bitmap = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(left, top, 0, 0, new Size(width, height));
        return bitmap;
    }
}
