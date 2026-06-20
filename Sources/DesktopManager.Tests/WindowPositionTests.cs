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
}
