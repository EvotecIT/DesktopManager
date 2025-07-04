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
}
