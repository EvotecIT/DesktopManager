using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowTopMostActivationTests.
/// </summary>
public class WindowTopMostActivationTests
{
    [TestMethod]
    /// <summary>
    /// Test for SetWindowTopMost_TogglesState.
    /// </summary>
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
        long originalStyle = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        bool wasTop = (originalStyle & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

        manager.SetWindowTopMost(window, !wasTop);
        long toggled = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        Assert.AreEqual(!wasTop, (toggled & MonitorNativeMethods.WS_EX_TOPMOST) != 0);

        manager.SetWindowTopMost(window, wasTop);
        long reverted = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_EXSTYLE).ToInt64();
        Assert.AreEqual(wasTop, (reverted & MonitorNativeMethods.WS_EX_TOPMOST) != 0);
    }

    [TestMethod]
    /// <summary>
    /// Test for ActivateWindow_BringsToFront.
    /// </summary>
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
