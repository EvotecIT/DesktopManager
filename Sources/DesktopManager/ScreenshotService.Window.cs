using System.Drawing;

namespace DesktopManager;

public static partial class ScreenshotService {
    /// <summary>
    /// Captures a screenshot of a window.
    /// </summary>
    /// <param name="hwnd">Window handle.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureWindow(IntPtr hwnd) {
        return CaptureWindowDetailed(hwnd, new WindowCaptureOptions { AllowDesktopFallback = true }).Bitmap;
    }

    /// <summary>Captures a window with provenance. By default, desktop fallback is disabled.</summary>
    /// <param name="hwnd">Target window handle.</param>
    /// <param name="options">Capture policy, including explicit desktop fallback.</param>
    /// <returns>A disposable image with physical pixel dimensions, physical screen bounds, backend and fallback information.</returns>
    /// <remarks>Temporarily uses per-monitor DPI awareness on Windows 10 1607 and later, restoring the calling thread before returning or throwing.</remarks>
    public static WindowCaptureResult CaptureWindowDetailed(IntPtr hwnd, WindowCaptureOptions? options = null) {
        return WindowCaptureDpiContext.Execute(() => CaptureWindowCore(hwnd, options));
    }

    private static WindowCaptureResult CaptureWindowCore(IntPtr hwnd, WindowCaptureOptions? options) {
        if (hwnd == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle", nameof(hwnd));
        }

        if (!MonitorNativeMethods.GetWindowRect(hwnd, out RECT rect)) {
            throw new InvalidOperationException("Failed to get window bounds");
        }

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) {
            throw new InvalidOperationException("The target window does not have a visible size.");
        }

        Bitmap? bitmap = TryPrintWindow(hwnd, width, height);
        try {
            bool dark = bitmap != null && LooksSuspiciouslyBlack(bitmap);
            if (bitmap != null && (!dark || options?.AllowDesktopFallback != true)) {
                return new WindowCaptureResult(bitmap, WindowCaptureBackend.PrintWindow,
                    new Rectangle(rect.Left, rect.Top, width, height), null, dark);
            }
            if (options?.AllowDesktopFallback != true) {
                throw new InvalidOperationException("PrintWindow did not supply an image and desktop fallback is disabled.");
            }
            Bitmap? fallback = CaptureRegion(rect.Left, rect.Top, width, height);
            try {
                bool fallbackDark = LooksSuspiciouslyBlack(fallback);
                if (bitmap != null && fallbackDark) {
                    return new WindowCaptureResult(bitmap, WindowCaptureBackend.PrintWindow,
                        new Rectangle(rect.Left, rect.Top, width, height), null, dark);
                }
                Rectangle desktop = GetVirtualScreenBounds();
                var result = new WindowCaptureResult(fallback, WindowCaptureBackend.Desktop,
                    new Rectangle(Math.Max(rect.Left, desktop.Left), Math.Max(rect.Top, desktop.Top), fallback.Width, fallback.Height),
                    bitmap == null ? "PrintWindowFailed" : "PrintWindowWasDark", fallbackDark);
                bitmap?.Dispose();
                fallback = null;
                return result;
            } finally {
                fallback?.Dispose();
            }
        } catch {
            bitmap?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Captures a screenshot of a window control.
    /// </summary>
    /// <param name="hwnd">Control handle.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureControl(IntPtr hwnd) {
        return CaptureWindow(hwnd);
    }

}
