using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowActivationPositioningTests.
/// </summary>
public class WindowActivationPositioningTests {
    [TestMethod]
    /// <summary>
    /// Test for SetWindowPosition_ResizesWindow.
    /// </summary>
    public void SetWindowPosition_ResizesWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Position Harness");

        var manager = new WindowManager();
        var original = manager.GetWindowPosition(harness.Window);

        int newWidth = original.Width + 10;
        int newHeight = original.Height + 10;
        manager.SetWindowPosition(harness.Window, original.Left, original.Top, newWidth, newHeight);
        Application.DoEvents();
        var resized = manager.GetWindowPosition(harness.Window);

        int widthTolerance = Math.Abs(newWidth - resized.Width);
        int heightTolerance = Math.Abs(newHeight - resized.Height);

        Assert.IsTrue(widthTolerance <= 20,
            $"Width resize failed. Expected: {newWidth}, Actual: {resized.Width}, Tolerance: {widthTolerance}");
        Assert.IsTrue(heightTolerance <= 20,
            $"Height resize failed. Expected: {newHeight}, Actual: {resized.Height}, Tolerance: {heightTolerance}");
    }

    [TestMethod]
    /// <summary>
    /// Test for ActivateWindow_InvalidHandle_Throws.
    /// </summary>
    public void ActivateWindow_InvalidHandle_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var dummy = new WindowInfo { Handle = IntPtr.Zero };
        Assert.ThrowsExactly<InvalidOperationException>(() => manager.ActivateWindow(dummy));
    }
}
