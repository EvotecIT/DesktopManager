using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests reusable reliable window placement behavior shared by app, CLI, and PowerShell hosts.
/// </summary>
public class WindowPlacementServiceTests {
    [TestMethod]
    /// <summary>
    /// Side-by-side monitors with different heights should still resolve as one row.
    /// </summary>
    public void GroupMonitorRows_SideBySideDifferentHeights_KeepsSingleRow() {
        Monitor left = CreateMonitor(1, 0, 0, 1920, 1080);
        Monitor right = CreateMonitor(2, 1920, 0, 3840, 1440);

        IReadOnlyList<IReadOnlyList<Monitor>> rows = WindowPlacementService.GroupMonitorRows(new[] { left, right });

        Assert.AreEqual(1, rows.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rows[0].Select(monitor => monitor.Index).ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Semantic window placements should use the monitor work area so taskbars are not covered.
    /// </summary>
    public void GetPlacementBounds_WorkAreaAvailable_UsesWorkArea() {
        Monitor monitor = CreateMonitor(
            1,
            left: 0,
            top: 0,
            right: 1920,
            bottom: 1080,
            workLeft: 0,
            workTop: 0,
            workRight: 1920,
            workBottom: 1040);

        RECT bounds = WindowPlacementService.GetPlacementBounds(monitor);

        Assert.AreEqual(0, bounds.Left);
        Assert.AreEqual(0, bounds.Top);
        Assert.AreEqual(1920, bounds.Right);
        Assert.AreEqual(1040, bounds.Bottom);
    }

    [TestMethod]
    /// <summary>
    /// Monitors without work-area metadata fall back to the full monitor bounds.
    /// </summary>
    public void GetPlacementBounds_WorkAreaMissing_UsesMonitorBounds() {
        Monitor monitor = CreateMonitor(1, 0, 0, 1920, 1080);

        RECT bounds = WindowPlacementService.GetPlacementBounds(monitor);

        Assert.AreEqual(0, bounds.Left);
        Assert.AreEqual(0, bounds.Top);
        Assert.AreEqual(1920, bounds.Right);
        Assert.AreEqual(1080, bounds.Bottom);
    }

    [TestMethod]
    /// <summary>
    /// Child/session-surface handles are normalized to the root window before placement.
    /// </summary>
    public void Apply_ChildHandleExactRectangle_MovesRootWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Placement Service Harness",
            form => {
                Button button = new() {
                    Text = "Hosted Session Surface",
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(button);
            });

        Control child = harness.Form.Controls[0];
        Assert.AreNotEqual(IntPtr.Zero, child.Handle);

        var manager = new WindowManager();
        WindowPosition original = manager.GetWindowPosition(harness.Window);
        int left = original.Left + 17;
        int top = original.Top + 19;
        const int width = 420;
        const int height = 260;

        try {
            var service = new WindowPlacementService();
            WindowPlacementResult result = service.Apply(new WindowPlacementRequest {
                TargetWindowHandle = child.Handle,
                Placement = WindowPlacementKind.ExactRectangle,
                ExactLeft = left,
                ExactTop = top,
                ExactWidth = width,
                ExactHeight = height,
                VerifyAfterAction = true,
                GeometryTolerancePixels = 12,
                VerificationTimeoutMilliseconds = 1000,
                VerificationIntervalMilliseconds = 25
            });

            Assert.IsTrue(result.Verified);
            Assert.AreEqual(harness.Form.Handle, result.ResolvedHandle);
            Assert.AreEqual(harness.Form.Handle, result.Window.Handle);
            Assert.IsTrue(result.Snapshots.Count >= 4);
            Assert.IsTrue(Math.Abs(result.Window.Left - left) <= 12);
            Assert.IsTrue(Math.Abs(result.Window.Top - top) <= 12);
            Assert.IsTrue(Math.Abs(result.Window.Width - width) <= 12);
            Assert.IsTrue(Math.Abs(result.Window.Height - height) <= 12);
        } finally {
            manager.SetWindowPosition(harness.Window, original.Left, original.Top, original.Right - original.Left, original.Bottom - original.Top);
        }
    }

    [TestMethod]
    /// <summary>
    /// DesktopAutomationService exposes the same reusable placement engine.
    /// </summary>
    public void ApplyWindowPlacement_AutomationService_UsesPlacementEngine() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Automation Placement Harness");

        var manager = new WindowManager();
        WindowPosition original = manager.GetWindowPosition(harness.Window);

        try {
            var automation = new DesktopAutomationService();
            WindowPlacementResult result = automation.ApplyWindowPlacement(new WindowPlacementRequest {
                TargetWindowHandle = harness.Window.Handle,
                Placement = WindowPlacementKind.Restore,
                VerifyAfterAction = true
            });

            Assert.IsTrue(result.Verified);
            Assert.AreEqual(harness.Window.Handle, result.ResolvedHandle);
        } finally {
            manager.SetWindowPosition(harness.Window, original.Left, original.Top, original.Right - original.Left, original.Bottom - original.Top);
        }
    }

    private static Monitor CreateMonitor(
        int index,
        int left,
        int top,
        int right,
        int bottom,
        int? workLeft = null,
        int? workTop = null,
        int? workRight = null,
        int? workBottom = null) {
        string deviceId = $"DISPLAY{index}";
        RECT rect = new() {
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom
        };
        RECT workArea = new() {
            Left = workLeft ?? 0,
            Top = workTop ?? 0,
            Right = workRight ?? 0,
            Bottom = workBottom ?? 0
        };

        return new Monitor(new MonitorService(new StubDesktopManager(new Dictionary<string, RECT> {
            [deviceId] = rect
        }))) {
            Index = index,
            DeviceId = deviceId,
            DeviceName = $"\\\\.\\DISPLAY{index}",
            StateFlags = DisplayDeviceStateFlags.AttachedToDesktop,
            Rect = rect,
            WorkArea = workArea
        };
    }
}
