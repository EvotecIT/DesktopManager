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
}
