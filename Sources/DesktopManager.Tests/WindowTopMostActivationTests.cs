using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class WindowTopMostActivationTests
{
    [TestMethod]
    public void SetWindowTopMost_TogglesState()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0)
        {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var originalStyle = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE);
        bool wasTop = (originalStyle & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        manager.SetWindowTopMost(window, !wasTop);
        var toggled = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE);
        Assert.AreEqual(!wasTop, (toggled & MonitorNativeMethods.WS_EX_TOPMOST) != 0);

        manager.SetWindowTopMost(window, wasTop);
        var reverted = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE);
        Assert.AreEqual(wasTop, (reverted & MonitorNativeMethods.WS_EX_TOPMOST) != 0);
    }

    [TestMethod]
    public void ActivateWindow_BringsToFront()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0)
        {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        manager.ActivateWindow(window);
        var foreground = MonitorNativeMethods.GetForegroundWindow();

        Assert.AreEqual(window.Handle, foreground);
    }
}
