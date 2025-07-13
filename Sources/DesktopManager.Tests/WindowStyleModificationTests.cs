using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for window style manipulation methods.
/// </summary>
public class WindowStyleModificationTests {
    [TestMethod]
    /// <summary>
    /// Ensures GetWindowStyle matches native API call.
    /// </summary>
    public void GetWindowStyle_MatchesNative() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        long managed = manager.GetWindowStyle(window);
        long native = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_STYLE).ToInt64();
        Assert.AreEqual(native, managed);
    }

    [TestMethod]
    /// <summary>
    /// Ensures SetWindowStyle toggles the topmost flag.
    /// </summary>
    public void SetWindowStyle_TogglesTopMost() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        long original = manager.GetWindowStyle(window, true);
        bool isTop = (original & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        manager.SetWindowStyle(window, MonitorNativeMethods.WS_EX_TOPMOST, !isTop, true);
        long toggled = manager.GetWindowStyle(window, true);
        Assert.AreEqual(!isTop, (toggled & MonitorNativeMethods.WS_EX_TOPMOST) != 0);

        manager.SetWindowStyle(window, MonitorNativeMethods.WS_EX_TOPMOST, isTop, true);
    }
}

