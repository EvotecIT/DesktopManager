using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
public class LayoutAndCaptureReliabilityTests {
    [TestMethod]
    public void LayoutPlan_DuplicateTitles_AreAmbiguousAndTargetsAreNotReused() {
        WindowPosition saved = Position(1, 100);
        var layout = new WindowLayout { Windows = new() { saved, Position(1, 200) } };
        WindowLayoutPlan ambiguous = WindowManager.BuildLayoutPlan(layout, new[] { Position(1, 10), Position(1, 20) });
        Assert.IsTrue(ambiguous.Windows.All(item => item.Status == WindowLayoutMatchStatus.Ambiguous));
        WindowLayoutPlan oneLive = WindowManager.BuildLayoutPlan(layout, new[] { Position(1, 10) });
        Assert.AreEqual(WindowLayoutMatchStatus.Matched, oneLive.Windows[0].Status);
        Assert.AreEqual(WindowLayoutMatchStatus.Missing, oneLive.Windows[1].Status);
    }

    [TestMethod]
    public void LayoutPlan_RestartMatching_RequiresDurableProcessAndClassIdentity() {
        var layout = new WindowLayout { Windows = new() { Position(1, 100) } };
        Assert.AreEqual(WindowLayoutMatchStatus.Missing,
            WindowManager.BuildLayoutPlan(layout, new[] { Position(2, 10) }).Windows[0].Status);
        layout.Windows[0].ProcessName = "editor";
        layout.Windows[0].ClassName = "EditorWindow";
        WindowPosition current = Position(2, 10);
        current.ProcessName = "other-app";
        current.ClassName = "EditorWindow";
        Assert.AreEqual(WindowLayoutMatchStatus.Missing,
            WindowManager.BuildLayoutPlan(layout, new[] { current }).Windows[0].Status);
        current.ProcessName = "editor";
        Assert.AreEqual(WindowLayoutMatchStatus.Matched,
            WindowManager.BuildLayoutPlan(layout, new[] { current }).Windows[0].Status);
    }

    [TestMethod]
    public void LayoutPlan_RejectsNullEntriesAndSkipsInvalidWindowStates() {
        var layout = new WindowLayout { Windows = new() { null! } };
        Assert.ThrowsExactly<ArgumentException>(() => WindowManager.BuildLayoutPlan(layout, Array.Empty<WindowPosition>()));
        WindowPosition saved = Position(1, 100);
        saved.State = (WindowState)999;
        layout.Windows[0] = saved;
        Assert.AreEqual(WindowLayoutMatchStatus.Invalid,
            WindowManager.BuildLayoutPlan(layout, new[] { Position(1, 10) }).Windows[0].Status);
    }

    [TestMethod]
    public void DisplayResult_Restart_IsNotReportedAsImmediatelyApplied() {
        DisplayModeApplyResult result = MonitorService.InterpretDisplayChange(DisplayChangeConfirmation.Restart, "owned test request");
        Assert.IsTrue(result.RestartRequired);
        Assert.IsFalse(result.Applied);
        Assert.IsTrue(MonitorService.InterpretDisplayChange(DisplayChangeConfirmation.Successful, "owned test request").Applied);
        Assert.ThrowsExactly<InvalidOperationException>(() => MonitorService.InterpretDisplayChange(DisplayChangeConfirmation.BadMode, "owned test request"));
    }

    [TestMethod]
    public async Task WindowWaitAsync_InfiniteWait_CanBeCanceled() {
        using var canceled = new CancellationTokenSource();
        canceled.CancelAfter(100);
        await Assert.ThrowsAsync<OperationCanceledException>(() => new WindowManager().WaitWindowAsync(
            "no-window-" + Guid.NewGuid().ToString("N"), cancellationToken: canceled.Token));
    }

    [TestMethod]
    public void WindowWait_RejectsNegativeTimeout() {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new WindowManager().WaitWindow("absent", -1));
    }

    [TestMethod]
    public void SparseBitmapReads_HandleTheSameBitmapAndPreserveComparisonMetrics() {
        using var image = new Bitmap(80, 80);
        using Graphics graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Black);
        Assert.IsTrue(ScreenshotService.LooksSuspiciouslyBlack(image));
        DesktopVisualDifferenceMetrics same = ScreenshotService.CompareBitmaps(image, image);
        Assert.AreEqual(0, same.ChangedSampleCount);
        using var changed = new Bitmap(80, 80);
        using (Graphics changedGraphics = Graphics.FromImage(changed)) { changedGraphics.Clear(Color.White); }
        DesktopVisualDifferenceMetrics different = ScreenshotService.CompareBitmaps(image, changed);
        Assert.AreEqual(different.SampleCount, different.ChangedSampleCount);
        Assert.AreEqual(255.0, different.AverageDifference);
    }

#if NET8_0_OR_GREATER
    [TestMethod]
    public void SparseBitmapComparison_DoesNotAllocateCopiesOfFourKImages() {
        using var first = new Bitmap(3840, 2160);
        using var second = new Bitmap(3840, 2160);
        ScreenshotService.CompareBitmaps(first, second);
        long before = GC.GetAllocatedBytesForCurrentThread();
        ScreenshotService.CompareBitmaps(first, second);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsTrue(allocated < 1024 * 1024, $"Sampled comparison allocated {allocated} bytes.");
    }
#endif

    [TestMethod]
    [TestCategory("UITest")]
    public void StrictCapture_LeavesDarkWindowPixelsIntactWhenAnotherOwnedWindowCoversIt() {
        TestHelper.RequireOwnedWindowUiTests();
        using Form target = new NonActivatingTestForm() {
            FormBorderStyle = FormBorderStyle.None, BackColor = Color.Black,
            StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(40, 40, 160, 100), ShowInTaskbar = false
        };
        using Form cover = new NonActivatingTestForm() {
            FormBorderStyle = FormBorderStyle.None, BackColor = Color.Red,
            StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(0, 0, 500, 400), ShowInTaskbar = false, TopMost = true
        };
        target.Show();
        cover.Show();
        cover.Refresh();
        Application.DoEvents();
        DwmFlush();
        using WindowCaptureResult strict = ScreenshotService.CaptureWindowDetailed(target.Handle);
        Assert.AreEqual(WindowCaptureBackend.PrintWindow, strict.Backend);
        Assert.IsTrue(strict.IsDark);
        Assert.IsNull(strict.FallbackReason);
        using WindowCaptureResult fallback = ScreenshotService.CaptureWindowDetailed(target.Handle,
            new WindowCaptureOptions { AllowDesktopFallback = true });
        Assert.AreEqual(WindowCaptureBackend.Desktop, fallback.Backend);
        Assert.AreEqual("PrintWindowWasDark", fallback.FallbackReason);
        Assert.IsTrue(fallback.Bitmap.GetPixel(80, 50).R > 200);
        Assert.AreEqual(Color.Red.ToArgb(), fallback.Bitmap.GetPixel(0, 0).ToArgb());
        Assert.AreEqual(Color.Red.ToArgb(), fallback.Bitmap.GetPixel(fallback.Bitmap.Width - 1, fallback.Bitmap.Height - 1).ToArgb());
        string? artifacts = Environment.GetEnvironmentVariable("DESKTOPMANAGER_TEST_ARTIFACTS");
        if (!string.IsNullOrEmpty(artifacts)) {
            Directory.CreateDirectory(artifacts);
            strict.Bitmap.Save(Path.Combine(artifacts, "strict-black-window.png"));
            fallback.Bitmap.Save(Path.Combine(artifacts, "owned-red-cover-fallback.png"));
        }
    }

    private static WindowPosition Position(uint processId, int left) {
        return new WindowPosition { Handle = new IntPtr(left), ProcessId = processId, Title = "Document",
            Left = left, Top = 0, Right = left + 100, Bottom = 100, State = WindowState.Normal };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();
}
