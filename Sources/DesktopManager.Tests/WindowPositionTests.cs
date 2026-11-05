using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowPositionTests.
/// </summary>
public class WindowPositionTests {
    [TestMethod]
    /// <summary>
    /// Test for GetAndSetWindowPosition_RoundTrips.
    /// </summary>
    public void GetAndSetWindowPosition_RoundTrips() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Position RoundTrip Harness");

        var manager = new WindowManager();
        var original = manager.GetWindowPosition(harness.Window);

        manager.SetWindowPosition(harness.Window, original.Left, original.Top);
        Application.DoEvents();
        var updated = manager.GetWindowPosition(harness.Window);

        Assert.AreEqual(original.Left, updated.Left);
        Assert.AreEqual(original.Top, updated.Top);
    }

    [TestMethod]
    /// <summary>
    /// Test for MoveWindow_DoesNotChangeZOrder.
    /// </summary>
    public void MoveWindow_DoesNotChangeZOrder() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager ZOrder Harness");

        var manager = new WindowManager();
        var windowsBefore = manager.GetWindows(includeHidden: true);
        int indexBefore = windowsBefore.FindIndex(w => w.Handle == harness.Window.Handle);

        var original = manager.GetWindowPosition(harness.Window);
        manager.SetWindowPosition(harness.Window, original.Left + 1, original.Top + 1);
        Application.DoEvents();

        var windowsAfterMove = manager.GetWindows(includeHidden: true);
        int indexAfterMove = windowsAfterMove.FindIndex(w => w.Handle == harness.Window.Handle);

        Assert.AreEqual(indexBefore, indexAfterMove);
    }

    [TestMethod]
    /// <summary>
    /// Test for GetWindowPosition_InvalidHandle_Throws.
    /// </summary>
    public void GetWindowPosition_InvalidHandle_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var dummy = new WindowInfo { Handle = IntPtr.Zero };
        Assert.ThrowsExactly<InvalidOperationException>(() => manager.GetWindowPosition(dummy));
    }

    [TestMethod]
    /// <summary>
    /// Test for MoveWindowToMonitor_OnSameMonitor_ReturnsFalse.
    /// </summary>
    public void MoveWindowToMonitor_OnSameMonitor_ReturnsFalse() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Same Monitor Harness");

        var manager = new WindowManager();
        var monitors = new Monitors().GetMonitors(index: harness.Window.MonitorIndex);
        var monitor = monitors.FirstOrDefault();
        if (monitor == null) {
            Assert.Inconclusive("Monitor not found");
        }

        bool moved = manager.MoveWindowToMonitor(harness.Window, monitor);

        Assert.IsFalse(moved);
    }

    [TestMethod]
    /// <summary>
    /// Test for MoveWindowToMonitor_OnDifferentMonitor_ReturnsTrue.
    /// </summary>
    public void MoveWindowToMonitor_OnDifferentMonitor_ReturnsTrue() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        var monitors = new Monitors().GetMonitors();
        if (monitors.Count < 2) {
            Assert.Inconclusive("Need at least two monitors");
        }

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Different Monitor Harness");

        var manager = new WindowManager();
        var target = monitors.First(m => m.Index != harness.Window.MonitorIndex);

        bool moved = manager.MoveWindowToMonitor(harness.Window, target);

        Assert.IsTrue(moved);
    }
    [TestMethod]
    /// <summary>
    /// A window keeps its place relative to the work area, and a place that would leave it off the target screen is moved back on.
    /// </summary>
    public void PlaceOnMonitor_KeepsTheOffsetAndStaysInsideTheTargetWorkArea() {
        var from = new RECT { Left = 0, Top = 0, Right = 3840, Bottom = 2112 };
        var strip = new RECT { Left = 3840, Top = 1440, Right = 6400, Bottom = 2160 };
        var window = new RECT { Left = 200, Top = 100, Right = 1400, Bottom = 900 };

        RECT kept = WindowManager.PlaceOnMonitor(window, from, new RECT { Left = 3840, Top = 0, Right = 7680, Bottom = 2112 });
        Assert.AreEqual((4040, 100, 5240, 900), (kept.Left, kept.Top, kept.Right, kept.Bottom));

        // Near the far corner, the window is pulled back so it ends at the strip's edge.
        RECT clamped = WindowManager.PlaceOnMonitor(new RECT { Left = 3000, Top = 1800, Right = 3800, Bottom = 2100 }, from, strip);
        Assert.AreEqual((5600, 1860, 6400, 2160), (clamped.Left, clamped.Top, clamped.Right, clamped.Bottom));

        // An 800-high window can't fit on a 720-high strip: its top-left corner stays on screen.
        RECT oversized = WindowManager.PlaceOnMonitor(window, from, strip);
        Assert.AreEqual((4040, 1440), (oversized.Left, oversized.Top));
    }
}