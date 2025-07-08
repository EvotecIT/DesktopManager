using System;
using System.Linq;
using System.Runtime.InteropServices;

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

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var original = manager.GetWindowPosition(window);

        manager.SetWindowPosition(window, original.Left, original.Top);
        var updated = manager.GetWindowPosition(window);

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

        var manager = new WindowManager();
        var windowsBefore = manager.GetWindows();
        if (windowsBefore.Count < 1) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windowsBefore.First();
        int indexBefore = windowsBefore.FindIndex(w => w.Handle == window.Handle);

        var original = manager.GetWindowPosition(window);
        manager.SetWindowPosition(window, original.Left + 1, original.Top + 1);

        var windowsAfterMove = manager.GetWindows();
        int indexAfterMove = windowsAfterMove.FindIndex(w => w.Handle == window.Handle);

        manager.SetWindowPosition(window, original.Left, original.Top);

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
        Assert.ThrowsException<InvalidOperationException>(() => manager.GetWindowPosition(dummy));
    }

    [TestMethod]
    /// <summary>
    /// Test for MoveWindowToMonitor_OnSameMonitor_ReturnsFalse.
    /// </summary>
    public void MoveWindowToMonitor_OnSameMonitor_ReturnsFalse() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var monitors = new Monitors().GetMonitors(index: window.MonitorIndex);
        var monitor = monitors.FirstOrDefault();
        if (monitor == null) {
            Assert.Inconclusive("Monitor not found");
        }

        bool moved = manager.MoveWindowToMonitor(window, monitor);

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

        var monitors = new Monitors().GetMonitors();
        if (monitors.Count < 2) {
            Assert.Inconclusive("Need at least two monitors");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var target = monitors.First(m => m.Index != window.MonitorIndex);

        bool moved = manager.MoveWindowToMonitor(window, target);

        Assert.IsTrue(moved);
    }
}
