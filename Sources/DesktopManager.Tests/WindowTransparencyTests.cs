using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for verifying window transparency adjustments.
/// </summary>
public class WindowTransparencyTests {
    [TestMethod]
    /// <summary>
    /// Ensures SetWindowTransparency applies layered style and alpha.
    /// </summary>
    public void SetWindowTransparency_AppliesLayeredStyleAndAlpha() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        long originalStyle = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        uint key; byte origAlpha; uint flags;
        MonitorNativeMethods.GetLayeredWindowAttributes(window.Handle, out key, out origAlpha, out flags);

        manager.SetWindowTransparency(window, 128);
        long layeredStyle = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        MonitorNativeMethods.GetLayeredWindowAttributes(window.Handle, out key, out byte newAlpha, out flags);

        Assert.IsTrue((layeredStyle & MonitorNativeMethods.WS_EX_LAYERED) != 0);
        Assert.AreEqual((byte)128, newAlpha);

        MonitorNativeMethods.SetLayeredWindowAttributes(window.Handle, 0, origAlpha, MonitorNativeMethods.LWA_ALPHA);
        MonitorNativeMethods.SetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE, new IntPtr(originalStyle));
    }
}
