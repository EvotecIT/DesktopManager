using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using DesktopManager;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for ScreenshotServiceTests.
/// </summary>
public class ScreenshotServiceTests {
    private const int StatusTimeoutMilliseconds = 5000;

    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    /// <summary>
    /// Test for CaptureRegion_InvalidDimensions_Throws.
    /// </summary>
    public void CaptureRegion_InvalidDimensions_Throws() {
        Assert.ThrowsException<ArgumentException>(() => ScreenshotService.CaptureRegion(0, 0, 0, 0));
    }

    [TestMethod]
    /// <summary>
    /// Test for CaptureRegion_OutOfBounds_Throws.
    /// </summary>
    public void CaptureRegion_OutOfBounds_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        Rectangle bounds;
#if NETFRAMEWORK
        bounds = SystemInformation.VirtualScreen;
#else
        bounds = new Rectangle(
            MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_XVIRTUALSCREEN),
            MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_YVIRTUALSCREEN),
            MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CXVIRTUALSCREEN),
            MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CYVIRTUALSCREEN));
#endif
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => ScreenshotService.CaptureRegion(bounds.Right + 1, bounds.Bottom + 1, 10, 10));
    }

    [TestMethod]
    /// <summary>
    /// Test for CaptureScreen_ReturnsBitmap.
    /// </summary>
    public void CaptureScreen_ReturnsBitmap() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();

        using var bmp = ScreenshotService.CaptureScreen();
        Assert.IsNotNull(bmp);
        Assert.IsTrue(bmp.Width > 0);
        Assert.IsTrue(bmp.Height > 0);
    }

    [TestMethod]
    /// <summary>
    /// Test for CaptureMonitor_InvalidIndex_Throws.
    /// </summary>
    public void CaptureMonitor_InvalidIndex_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        Assert.ThrowsException<ArgumentException>(() => ScreenshotService.CaptureMonitor(index: 999));
    }

    [TestMethod]
    /// <summary>
    /// Test for CaptureMonitor_ByIndex_ReturnsBitmap.
    /// </summary>
    public void CaptureMonitor_ByIndex_ReturnsBitmap() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();

        using var bmp = ScreenshotService.CaptureMonitor(index: 0);
        Assert.IsNotNull(bmp);
        Assert.IsTrue(bmp.Width > 0);
        Assert.IsTrue(bmp.Height > 0);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// CaptureWindow size matches window bounds.
    /// </summary>
    public void CaptureWindow_SizeMatchesBounds() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Screenshot Window Harness");

        Assert.IsTrue(MonitorNativeMethods.GetWindowRect(harness.Window.Handle, out RECT rect));
        using var bmp = ScreenshotService.CaptureWindow(harness.Window.Handle);
        Assert.AreEqual(rect.Right - rect.Left, bmp.Width);
        Assert.AreEqual(rect.Bottom - rect.Top, bmp.Height);
    }

    [TestMethod]
    /// <summary>
    /// Suspicious black-frame detection should distinguish an obviously black frame from visible content.
    /// </summary>
    public void LooksSuspiciouslyBlack_DetectsBlackFrames() {
        using Bitmap blackBitmap = new(80, 80);
        using (Graphics graphics = Graphics.FromImage(blackBitmap)) {
            graphics.Clear(Color.Black);
        }

        using Bitmap brightBitmap = new(80, 80);
        using (Graphics graphics = Graphics.FromImage(brightBitmap)) {
            graphics.Clear(Color.White);
        }

        Assert.IsTrue(ScreenshotService.LooksSuspiciouslyBlack(blackBitmap));
        Assert.IsFalse(ScreenshotService.LooksSuspiciouslyBlack(brightBitmap));
    }

    [TestMethod]
    /// <summary>
    /// Bitmap comparison should report no visible change when the sampled pixels match.
    /// </summary>
    public void CompareBitmaps_IdenticalBitmaps_ReportNoChange() {
        using Bitmap baseline = new(48, 48);
        using (Graphics graphics = Graphics.FromImage(baseline)) {
            graphics.Clear(Color.White);
        }

        using Bitmap current = new(48, 48);
        using (Graphics graphics = Graphics.FromImage(current)) {
            graphics.Clear(Color.White);
        }

        DesktopVisualDifferenceMetrics metrics = ScreenshotService.CompareBitmaps(baseline, current);

        Assert.AreEqual(48 * 48, metrics.SampleCount);
        Assert.AreEqual(0, metrics.ChangedSampleCount);
        Assert.AreEqual(0d, metrics.ChangedSampleRatio, 0.0001d);
        Assert.AreEqual(0d, metrics.AverageDifference, 0.0001d);
        Assert.IsFalse(metrics.SizeChanged);
    }

    [TestMethod]
    /// <summary>
    /// Bitmap comparison should report visible change when the sampled pixels differ materially.
    /// </summary>
    public void CompareBitmaps_ChangedBitmaps_ReportChangedSamples() {
        using Bitmap baseline = new(32, 32);
        using (Graphics graphics = Graphics.FromImage(baseline)) {
            graphics.Clear(Color.Black);
        }

        using Bitmap current = new(32, 32);
        using (Graphics graphics = Graphics.FromImage(current)) {
            graphics.Clear(Color.White);
        }

        DesktopVisualDifferenceMetrics metrics = ScreenshotService.CompareBitmaps(baseline, current, differenceThreshold: 24);

        Assert.AreEqual(32 * 32, metrics.SampleCount);
        Assert.AreEqual(metrics.SampleCount, metrics.ChangedSampleCount);
        Assert.AreEqual(1d, metrics.ChangedSampleRatio, 0.0001d);
        Assert.IsTrue(metrics.AverageDifference >= 200d, "A black-to-white change should produce a large average per-channel difference.");
        Assert.IsFalse(metrics.SizeChanged);
    }

    [TestMethod]
    /// <summary>
    /// Bitmap comparison should treat a size change as an immediate visible change.
    /// </summary>
    public void CompareBitmaps_DifferentSizes_ReportSizeChange() {
        using Bitmap baseline = new(32, 32);
        using Bitmap current = new(48, 32);

        DesktopVisualDifferenceMetrics metrics = ScreenshotService.CompareBitmaps(baseline, current);

        Assert.AreEqual(1, metrics.SampleCount);
        Assert.AreEqual(1, metrics.ChangedSampleCount);
        Assert.AreEqual(1d, metrics.ChangedSampleRatio, 0.0001d);
        Assert.AreEqual(255d, metrics.AverageDifference, 0.0001d);
        Assert.IsTrue(metrics.SizeChanged);
    }

    [TestMethod]
    /// <summary>
    /// Template matching should return the embedded offset when the searched image contains the saved region unchanged.
    /// </summary>
    public void FindBestBitmapMatch_EmbeddedTemplate_ReturnsExpectedLocation() {
        using Bitmap template = new(12, 10);
        using (Graphics graphics = Graphics.FromImage(template)) {
            graphics.Clear(Color.White);
            graphics.FillRectangle(Brushes.Red, 0, 0, 4, 10);
            graphics.FillRectangle(Brushes.Blue, 4, 0, 4, 10);
            graphics.FillRectangle(Brushes.Green, 8, 0, 4, 10);
        }

        using Bitmap search = new(48, 36);
        using (Graphics graphics = Graphics.FromImage(search)) {
            graphics.Clear(Color.Black);
            graphics.DrawImageUnscaled(template, 17, 9);
        }

        DesktopVisualBitmapMatch match = ScreenshotService.FindBestBitmapMatch(
            template,
            search,
            differenceThreshold: 24,
            scanStep: 5);

        Assert.AreEqual(17, match.RelativeX);
        Assert.AreEqual(9, match.RelativeY);
        Assert.AreEqual(template.Width, match.Width);
        Assert.AreEqual(template.Height, match.Height);
        Assert.IsTrue(match.EvaluatedPositionCount > 0);
        Assert.AreEqual(0d, match.Metrics.AverageDifference, 0.0001d);
        Assert.AreEqual(0d, match.Metrics.ChangedSampleRatio, 0.0001d);
    }

    [TestMethod]
    /// <summary>
    /// Template matching should surface an obvious mismatch when the searched image does not contain the template content.
    /// </summary>
    public void FindBestBitmapMatch_MissingTemplate_ReturnsHighDifferenceMetrics() {
        using Bitmap template = new(10, 10);
        using (Graphics graphics = Graphics.FromImage(template)) {
            graphics.Clear(Color.White);
        }

        using Bitmap search = new(40, 30);
        using (Graphics graphics = Graphics.FromImage(search)) {
            graphics.Clear(Color.Black);
        }

        DesktopVisualBitmapMatch match = ScreenshotService.FindBestBitmapMatch(
            template,
            search,
            differenceThreshold: 24,
            scanStep: 6);

        Assert.IsTrue(match.EvaluatedPositionCount > 0);
        Assert.IsTrue(match.Metrics.AverageDifference >= 200d);
        Assert.AreEqual(1d, match.Metrics.ChangedSampleRatio, 0.0001d);
    }

    [TestMethod]
    /// <summary>
    /// OCR should read high-contrast generated text from a bitmap.
    /// </summary>
    public void ReadText_HighContrastBitmap_ReturnsExpectedText() {
#if NET472
        Assert.Inconclusive("OCR bitmap tests run only on the modern Windows targets.");
#endif
        using Bitmap bitmap = CreateHighContrastTextBitmap("APPLY");

        DesktopOcrReadResult result = ScreenshotService.ReadText(bitmap, "en-US");

        Assert.IsTrue(result.Lines.Count >= 1);
        Assert.IsTrue(result.Lines.Any(line => string.Equals(line.Text, "APPLY", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(result.Lines.SelectMany(line => line.Words).Any(word => string.Equals(word.Text, "APPLY", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    /// <summary>
    /// OCR text resolution should prefer an exact word match over a larger containing line match.
    /// </summary>
    public void ResolveWindowTextMatch_ContainsQuery_PrefersExactWordBounds() {
        WindowInfo window = CreateSyntheticWindow();
        var readResult = new DesktopWindowTextReadResult {
            Window = window,
            Geometry = CreateSyntheticGeometry(window),
            CaptureScreenX = 100,
            CaptureScreenY = 200,
            LanguageTag = "en-US",
            Lines = new[] {
                new DesktopOcrLine {
                    Text = "SEND PROMPT",
                    X = 12,
                    Y = 24,
                    Width = 180,
                    Height = 40,
                    Words = new[] {
                        new DesktopOcrWord {
                            Text = "SEND",
                            X = 12,
                            Y = 24,
                            Width = 64,
                            Height = 40
                        },
                        new DesktopOcrWord {
                            Text = "PROMPT",
                            X = 88,
                            Y = 24,
                            Width = 104,
                            Height = 40
                        }
                    }
                }
            }
        };

        DesktopWindowTextResolveResult result = DesktopAutomationService.ResolveWindowTextMatch(readResult, "SEND", contains: true);

        Assert.IsTrue(result.Matched);
        Assert.AreEqual("word", result.MatchKind);
        Assert.AreEqual("SEND", result.MatchedText);
        Assert.AreEqual(12, result.RelativeX);
        Assert.AreEqual(24, result.RelativeY);
        Assert.AreEqual(64, result.Width);
        Assert.AreEqual(40, result.Height);
        Assert.AreEqual(112, result.ScreenX);
        Assert.AreEqual(224, result.ScreenY);
        Assert.AreEqual(144, result.ActionX);
        Assert.AreEqual(244, result.ActionY);
        Assert.AreEqual(2, result.CandidateCount);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Window capture should not return a black hosted-surface region when the same region is visibly present on screen.
    /// </summary>
    public void CaptureWindow_CommandBarSurface_DoesNotReturnBlackHostedRegion() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireExternalDesktopApplicationTests();
        using DesktopManagerTestAppSession session = DesktopManagerTestAppSession.Start(
            "screenshot-commandbar-parity",
            initialText: "capture-parity",
            surface: "commandbar");
        session.RequestFocusCommandBar();
        Thread.Sleep(150);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => candidate.CommandBarHostBounds.Width > 0 &&
                candidate.CommandBarHostBounds.Height > 0 &&
                string.Equals(candidate.ActiveSurface, "commandbar", StringComparison.OrdinalIgnoreCase),
            StatusTimeoutMilliseconds,
            "The DesktopManager test app did not publish the command bar hosted-surface bounds in time.");

        DesktopAutomationService automation = new();
        DesktopWindowGeometry geometry = automation.GetWindowGeometry(session.WindowHandle);
        using Bitmap windowCapture = ScreenshotService.CaptureWindow(session.WindowHandle);
        using Bitmap desktopCapture = ScreenshotService.CaptureRegion(
            geometry.WindowLeft,
            geometry.WindowTop,
            geometry.WindowWidth,
            geometry.WindowHeight);

        Rectangle commandBarRegion = CreateWindowRelativeBounds(status.CommandBarHostBounds, geometry);
        using Bitmap windowCommandBarBitmap = CropBitmap(windowCapture, commandBarRegion);
        using Bitmap desktopCommandBarBitmap = CropBitmap(desktopCapture, commandBarRegion);

        Assert.IsFalse(
            ScreenshotService.LooksSuspiciouslyBlack(desktopCommandBarBitmap),
            "The desktop crop for the hosted WPF command bar region was unexpectedly black, so parity could not be certified.");
        Assert.IsFalse(
            ScreenshotService.LooksSuspiciouslyBlack(windowCommandBarBitmap),
            "The window capture returned a suspiciously black hosted-surface region even though the desktop crop showed visible content.");
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Window capture should not return a black WebView2 region when the same region is visibly present on screen.
    /// </summary>
    public void CaptureWindow_WebView2Surface_DoesNotReturnBlackHostedRegion() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireExternalDesktopApplicationTests();
        using DesktopManagerTestAppSession session = DesktopManagerTestAppSession.Start(
            "screenshot-webview-parity",
            initialText: "webview-parity",
            surface: "webview");
        session.RequestFocusWebView();
        Thread.Sleep(250);

        DesktopManagerTestAppStatus status = session.WaitForStatus(
            candidate => candidate.WebViewClientBounds.Width > 0 &&
                candidate.WebViewClientBounds.Height > 0 &&
                (candidate.WebViewReady || candidate.WebViewStatusText.StartsWith("WebView2 initialization failed:", StringComparison.Ordinal)),
            StatusTimeoutMilliseconds * 2,
            "The DesktopManager test app did not publish the WebView2 hosted-surface bounds in time.");
        if (!status.WebViewReady) {
            Assert.Inconclusive(status.WebViewStatusText);
        }

        DesktopAutomationService automation = new();
        DesktopWindowGeometry geometry = automation.GetWindowGeometry(session.WindowHandle);
        using Bitmap windowCapture = ScreenshotService.CaptureWindow(session.WindowHandle);
        using Bitmap desktopCapture = ScreenshotService.CaptureRegion(
            geometry.WindowLeft,
            geometry.WindowTop,
            geometry.WindowWidth,
            geometry.WindowHeight);

        Rectangle webViewRegion = CreateClientRelativeBounds(status.WebViewClientBounds, geometry);
        using Bitmap windowWebViewBitmap = CropBitmap(windowCapture, webViewRegion);
        using Bitmap desktopWebViewBitmap = CropBitmap(desktopCapture, webViewRegion);

        Assert.IsFalse(
            ScreenshotService.LooksSuspiciouslyBlack(desktopWebViewBitmap),
            "The desktop crop for the WebView2 hosted region was unexpectedly black, so parity could not be certified.");
        Assert.IsFalse(
            ScreenshotService.LooksSuspiciouslyBlack(windowWebViewBitmap),
            "The window capture returned a suspiciously black WebView2 region even though the desktop crop showed visible content.");
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Window capture should not return a black WinUI editor region when the same region is visibly present on screen.
    /// </summary>
    public void CaptureWindow_WinUiHarnessEditor_DoesNotReturnBlackHostedRegion() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireExternalDesktopApplicationTests();
        using DesktopManagerWinUiHarnessSession session = DesktopManagerWinUiHarnessSession.Start(
            "screenshot-winui-parity",
            initialText: "winui-parity");

        DesktopManagerWinUiHarnessStatus status = session.WaitForStatus(
            candidate => !string.IsNullOrWhiteSpace(candidate.WindowTitle) &&
                string.Equals(candidate.SelectedOption, "Alpha", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish its initial status snapshot in time.");

        DesktopAutomationService automation = new();
        WindowControlTargetInfo editor = WaitForSingleWinUiHarnessControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "ModernEditor",
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            "The WinUI harness editor was not discoverable before capture parity verification.");

        DesktopWindowGeometry geometry = automation.GetWindowGeometry(session.WindowHandle);
        using Bitmap windowCapture = ScreenshotService.CaptureWindow(session.WindowHandle);
        using Bitmap desktopCapture = ScreenshotService.CaptureRegion(
            geometry.WindowLeft,
            geometry.WindowTop,
            geometry.WindowWidth,
            geometry.WindowHeight);

        Rectangle editorRegion = CreateWindowRelativeBounds(editor.Control.Left, editor.Control.Top, editor.Control.Width, editor.Control.Height, geometry);
        using Bitmap windowEditorBitmap = CropBitmap(windowCapture, editorRegion);
        using Bitmap desktopEditorBitmap = CropBitmap(desktopCapture, editorRegion);

        Assert.IsFalse(
            ScreenshotService.LooksSuspiciouslyBlack(desktopEditorBitmap),
            "The desktop crop for the WinUI editor region was unexpectedly black, so parity could not be certified.");
        Assert.IsFalse(
            ScreenshotService.LooksSuspiciouslyBlack(windowEditorBitmap),
            "The window capture returned a suspiciously black WinUI editor region even though the desktop crop showed visible content.");
        Assert.AreEqual("Alpha", status.SelectedOption);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// CaptureControl size matches control bounds.
    /// </summary>
    public void CaptureControl_SizeMatchesBounds() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using TextBox textBox = new() { Text = "Capture" };
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Screenshot Control Harness", form => form.Controls.Add(textBox));
        textBox.CreateControl();
        Application.DoEvents();

        var enumerator = new ControlEnumerator();
        var controls = enumerator.EnumerateControls(harness.Window.Handle);
        var edit = controls.FirstOrDefault(c => c.Handle == textBox.Handle);
        Assert.IsNotNull(edit, "Edit control not found");

        Assert.IsTrue(MonitorNativeMethods.GetWindowRect(edit.Handle, out RECT rect));
        using var bmp = ScreenshotService.CaptureControl(edit.Handle);
        Assert.AreEqual(rect.Right - rect.Left, bmp.Width);
        Assert.AreEqual(rect.Bottom - rect.Top, bmp.Height);
    }

    private static Rectangle CreateWindowRelativeBounds(DesktopManagerTestAppControlBounds bounds, DesktopWindowGeometry geometry) {
        Rectangle relativeBounds = new(
            bounds.Left - geometry.WindowLeft,
            bounds.Top - geometry.WindowTop,
            bounds.Width,
            bounds.Height);

        Rectangle windowBounds = new(0, 0, geometry.WindowWidth, geometry.WindowHeight);
        Rectangle intersectedBounds = Rectangle.Intersect(relativeBounds, windowBounds);
        if (intersectedBounds.Width <= 0 || intersectedBounds.Height <= 0) {
            throw new AssertInconclusiveException("The hosted-surface bounds were outside the captured window region.");
        }

        return intersectedBounds;
    }

    private static Rectangle CreateClientRelativeBounds(DesktopManagerTestAppControlBounds bounds, DesktopWindowGeometry geometry) {
        Rectangle relativeBounds = new(
            geometry.ClientOffsetLeft + bounds.Left,
            geometry.ClientOffsetTop + bounds.Top,
            bounds.Width,
            bounds.Height);

        Rectangle windowBounds = new(0, 0, geometry.WindowWidth, geometry.WindowHeight);
        Rectangle intersectedBounds = Rectangle.Intersect(relativeBounds, windowBounds);
        if (intersectedBounds.Width <= 0 || intersectedBounds.Height <= 0) {
            throw new AssertInconclusiveException("The hosted-surface client-relative bounds were outside the captured window region.");
        }

        return intersectedBounds;
    }

    private static Rectangle CreateWindowRelativeBounds(int left, int top, int width, int height, DesktopWindowGeometry geometry) {
        Rectangle relativeBounds = new(
            left - geometry.WindowLeft,
            top - geometry.WindowTop,
            width,
            height);

        Rectangle windowBounds = new(0, 0, geometry.WindowWidth, geometry.WindowHeight);
        Rectangle intersectedBounds = Rectangle.Intersect(relativeBounds, windowBounds);
        if (intersectedBounds.Width <= 0 || intersectedBounds.Height <= 0) {
            throw new AssertInconclusiveException("The requested screen-relative bounds were outside the captured window region.");
        }

        return intersectedBounds;
    }

    private static WindowControlTargetInfo WaitForSingleWinUiHarnessControl(
        DesktopAutomationService automation,
        DesktopManagerWinUiHarnessSession session,
        WindowControlQueryOptions controlQuery,
        string failureMessage) {
        session.WaitForStatus(
            _ => {
                IReadOnlyList<WindowControlTargetInfo> controls = automation.GetControls(session.CreateWindowQuery(), controlQuery, allWindows: false, allControls: true);
                return controls.Count == 1 && controls[0].Control.Width > 0 && controls[0].Control.Height > 0;
            },
            StatusTimeoutMilliseconds,
            failureMessage);

        IReadOnlyList<WindowControlTargetInfo> resolvedControls = automation.GetControls(session.CreateWindowQuery(), controlQuery, allWindows: false, allControls: true);
        Assert.AreEqual(1, resolvedControls.Count, failureMessage);
        return resolvedControls.Single();
    }

    private static Bitmap CropBitmap(Bitmap bitmap, Rectangle bounds) {
        Rectangle bitmapBounds = new(0, 0, bitmap.Width, bitmap.Height);
        Rectangle intersectedBounds = Rectangle.Intersect(bounds, bitmapBounds);
        if (intersectedBounds.Width <= 0 || intersectedBounds.Height <= 0) {
            throw new AssertInconclusiveException("The requested crop region was outside the captured bitmap.");
        }

        return bitmap.Clone(intersectedBounds, bitmap.PixelFormat);
    }

    private static Bitmap CreateHighContrastTextBitmap(string text) {
        Bitmap bitmap = new(900, 220);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
        using Font font = new("Arial", 96, FontStyle.Bold, GraphicsUnit.Pixel);
        graphics.DrawString(text, font, Brushes.Black, new PointF(24, 40));
        return bitmap;
    }

    private static WindowInfo CreateSyntheticWindow() {
        return new WindowInfo {
            Title = "Synthetic OCR Window",
            Handle = new IntPtr(0x1234),
            ProcessId = 100,
            ThreadId = 200,
            Left = 40,
            Top = 60,
            Right = 840,
            Bottom = 660,
            IsVisible = true,
            MonitorDeviceName = "Synthetic"
        };
    }

    private static DesktopWindowGeometry CreateSyntheticGeometry(WindowInfo window) {
        return new DesktopWindowGeometry {
            Window = window,
            WindowLeft = window.Left,
            WindowTop = window.Top,
            WindowWidth = window.Width,
            WindowHeight = window.Height,
            ClientLeft = window.Left + 10,
            ClientTop = window.Top + 30,
            ClientWidth = window.Width - 20,
            ClientHeight = window.Height - 40,
            ClientOffsetLeft = 10,
            ClientOffsetTop = 30
        };
    }
}
