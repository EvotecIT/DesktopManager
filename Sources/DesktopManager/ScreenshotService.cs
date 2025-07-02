using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Provides methods for capturing screenshots of the desktop.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ScreenshotService {
    /// <summary>
    /// Captures a screenshot of the entire virtual screen.
    /// </summary>
    /// <returns>A <see cref="Bitmap"/> containing the screenshot.</returns>
    public static Bitmap CaptureScreen() {
        Monitors monitors = new();
        var rects = monitors.GetMonitors(connectedOnly: true)
                            .Select(m => m.GetMonitorBounds())
                            .ToList();

        if (!rects.Any()) {
            throw new InvalidOperationException("No monitors detected");
        }

        int left = rects.Min(r => r.Left);
        int top = rects.Min(r => r.Top);
        int right = rects.Max(r => r.Right);
        int bottom = rects.Max(r => r.Bottom);

        return CaptureRegion(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// Captures a screenshot of the specified monitor.
    /// </summary>
    /// <param name="index">Monitor index starting at 0.</param>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="deviceName">Monitor device name.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureMonitor(int? index = null, string deviceId = null, string deviceName = null) {
        Monitors monitors = new();
        var monitor = monitors.GetMonitors(index: index, deviceId: deviceId, deviceName: deviceName).FirstOrDefault();
        if (monitor == null) {
            string requested = !string.IsNullOrEmpty(deviceId)
                ? $"DeviceId '{deviceId}'"
                : !string.IsNullOrEmpty(deviceName)
                    ? $"DeviceName '{deviceName}'"
                    : "the specified criteria";
            throw new ArgumentException($"Monitor not found for {requested}");
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
        if (width <= 0 || height <= 0) {
            throw new ArgumentException("Width and height must be greater than zero");
        }

        Bitmap bitmap = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(left, top, 0, 0, new Size(width, height));
        return bitmap;
    }
}
