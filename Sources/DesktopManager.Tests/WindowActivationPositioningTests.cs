using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class WindowActivationPositioningTests {
    [TestMethod]
    public void SetWindowPosition_ResizesWindow() {
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

        int newWidth = original.Width + 10;
        int newHeight = original.Height + 10;
        manager.SetWindowPosition(window, original.Left, original.Top, newWidth, newHeight);
        var resized = manager.GetWindowPosition(window);

        Assert.AreEqual(newWidth, resized.Width);
        Assert.AreEqual(newHeight, resized.Height);

        manager.SetWindowPosition(window, original.Left, original.Top, original.Width, original.Height);
    }

    [TestMethod]
    public void ActivateWindow_InvalidHandle_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var dummy = new WindowInfo { Handle = IntPtr.Zero };
        Assert.ThrowsException<InvalidOperationException>(() => manager.ActivateWindow(dummy));
    }
}
