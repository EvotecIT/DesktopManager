using System;
using System.Drawing;

namespace DesktopManager;

public sealed partial class DesktopAutomationService {
    /// <summary>
    /// Captures the entire desktop.
    /// </summary>
    public DesktopCapture CaptureDesktop() {
        Rectangle bounds = ScreenshotService.GetVirtualScreenBounds();
        DesktopCapture capture = CaptureRegion(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        capture.Kind = "desktop";
        return capture;
    }

    /// <summary>
    /// Captures a monitor.
    /// </summary>
    public DesktopCapture CaptureMonitor(int? monitorIndex = null, string? deviceId = null, string? deviceName = null) {
        Monitor monitor = GetMonitor(index: monitorIndex, deviceId: deviceId, deviceName: deviceName)
            ?? throw new InvalidOperationException("No matching monitor was found.");

        DesktopCapture capture = CaptureRegion(monitor.PositionLeft, monitor.PositionTop,
            monitor.PositionRight - monitor.PositionLeft, monitor.PositionBottom - monitor.PositionTop);
        capture.Kind = "monitor";
        capture.MonitorIndex = monitor.Index;
        capture.MonitorDeviceName = monitor.DeviceName;
        return capture;
    }

    /// <summary>
    /// Captures a desktop region.
    /// </summary>
    public DesktopCapture CaptureRegion(int left, int top, int width, int height) {
        Bitmap bitmap = ScreenshotService.CaptureRegion(left, top, width, height, out Rectangle bounds);
        return new DesktopCapture {
            Kind = "region",
            Bitmap = bitmap,
            CaptureBackend = WindowCaptureBackend.Desktop,
            CapturedScreenBounds = bounds
        };
    }

    /// <summary>Captures a matching window with legacy desktop fallback and capture provenance.</summary>
    public DesktopCapture CaptureWindow(WindowQueryOptions options) {
        return CaptureWindow(options, new WindowCaptureOptions { AllowDesktopFallback = true });
    }

    /// <summary>Captures a matching window using an explicit capture policy.</summary>
    public DesktopCapture CaptureWindow(WindowQueryOptions options, WindowCaptureOptions? captureOptions) {
        return CaptureResolvedWindow(ResolveSingleWindow(options), captureOptions);
    }

    /// <summary>Captures a window handle with legacy desktop fallback and capture provenance.</summary>
    public DesktopCapture CaptureWindow(IntPtr windowHandle) {
        return CaptureWindow(windowHandle, new WindowCaptureOptions { AllowDesktopFallback = true });
    }

    /// <summary>Captures a window handle using an explicit capture policy.</summary>
    public DesktopCapture CaptureWindow(IntPtr windowHandle, WindowCaptureOptions? captureOptions) {
        return CaptureResolvedWindow(ResolveWindowByHandle(windowHandle), captureOptions);
    }

    private DesktopCapture CaptureResolvedWindow(WindowInfo window, WindowCaptureOptions? options) {
        DesktopWindowGeometry geometry = DescribeWindowGeometry(window);
        WindowCaptureResult result = ScreenshotService.CaptureWindowDetailed(window.Handle, options);
        return new DesktopCapture {
            Kind = "window", Bitmap = result.Bitmap, Window = window, Geometry = geometry,
            CaptureBackend = result.Backend, CapturedScreenBounds = result.ScreenBounds,
            CaptureFallbackReason = result.FallbackReason, IsDarkCapture = result.IsDark
        };
    }

    /// <summary>Captures a resolved native control with capture provenance.</summary>
    public DesktopCapture CaptureControl(WindowControlInfo control) {
        if (control == null) { throw new ArgumentNullException(nameof(control)); }
        EnsureControlSupportsNativeStateChange(control, "captured");
        WindowInfo window = ResolveParentWindow(control);
        DesktopWindowGeometry geometry = DescribeWindowGeometry(window);
        WindowCaptureResult result = ScreenshotService.CaptureWindowDetailed(control.Handle,
            new WindowCaptureOptions { AllowDesktopFallback = true });
        return new DesktopCapture {
            Kind = "control", Bitmap = result.Bitmap, Window = window, Control = control, Geometry = geometry,
            CaptureBackend = result.Backend, CapturedScreenBounds = result.ScreenBounds,
            CaptureFallbackReason = result.FallbackReason, IsDarkCapture = result.IsDark
        };
    }

    /// <summary>Captures a control resolved by its parent window and handle.</summary>
    public DesktopCapture CaptureControl(IntPtr windowHandle, IntPtr controlHandle, bool useUiAutomation = true, bool includeUiAutomation = true) {
        WindowControlInfo? control = GetControl(windowHandle, controlHandle, useUiAutomation, includeUiAutomation);
        if (control == null) { throw new InvalidOperationException("Failed to resolve the requested control."); }
        return CaptureControl(control);
    }

    /// <summary>Captures a window's client area, retaining full-window pixels if client bounds cannot be cropped.</summary>
    public DesktopCapture CaptureWindowClientArea(WindowQueryOptions options) {
        return CaptureClientArea(ResolveSingleWindow(options));
    }

    /// <summary>Captures a window handle's client area, retaining full-window pixels if client bounds cannot be cropped.</summary>
    public DesktopCapture CaptureWindowClientArea(IntPtr windowHandle) {
        return CaptureClientArea(ResolveWindowByHandle(windowHandle));
    }

    private DesktopCapture CaptureClientArea(WindowInfo window) {
        using DesktopCapture capture = CaptureResolvedWindow(window, new WindowCaptureOptions { AllowDesktopFallback = true });
        DesktopWindowGeometry geometry = capture.Geometry!;
        Bitmap? cropped = CreateClientAreaBitmap(capture.Bitmap, geometry, capture.CapturedScreenBounds);
        Rectangle bounds = capture.CapturedScreenBounds!.Value;
        Bitmap bitmap = cropped ?? (Bitmap)capture.Bitmap.Clone();
        if (cropped != null) {
            bounds = new Rectangle(Math.Max(bounds.Left, geometry.ClientLeft), Math.Max(bounds.Top, geometry.ClientTop),
                bitmap.Width, bitmap.Height);
        }
        return new DesktopCapture {
            Kind = "window-client", Bitmap = bitmap, Window = window, Geometry = geometry,
            CaptureBackend = capture.CaptureBackend, CapturedScreenBounds = bounds,
            CaptureFallbackReason = capture.CaptureFallbackReason, IsDarkCapture = capture.IsDarkCapture
        };
    }
}
