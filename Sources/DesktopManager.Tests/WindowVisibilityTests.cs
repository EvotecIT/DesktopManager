using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for showing and hiding windows.
/// </summary>
public class WindowVisibilityTests {
    [TestMethod]
    /// <summary>
    /// ShowWindow toggles visibility.
    /// </summary>
    public void ShowWindow_TogglesVisibility() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager Visibility Harness");

        var manager = new WindowManager();
        bool wasVisible = MonitorNativeMethods.IsWindowVisible(harness.Window.Handle);

        manager.ShowWindow(harness.Window, !wasVisible);
        Application.DoEvents();
        bool toggled = MonitorNativeMethods.IsWindowVisible(harness.Window.Handle);
        Assert.AreEqual(!wasVisible, toggled);

        manager.ShowWindow(harness.Window, wasVisible);
        Application.DoEvents();
        bool reverted = MonitorNativeMethods.IsWindowVisible(harness.Window.Handle);
        Assert.AreEqual(wasVisible, reverted);
    }

    [TestMethod]
    /// <summary>
    /// ShowWindow throws on invalid handle.
    /// </summary>
    public void ShowWindow_InvalidHandle_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var dummy = new WindowInfo { Handle = IntPtr.Zero };
        Assert.ThrowsExactly<InvalidOperationException>(() => manager.ShowWindow(dummy, true));
    }

    [TestMethod]
    /// <summary>
    /// GetWindows includes hidden windows when requested.
    /// </summary>
    public void GetWindows_IncludesHidden() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var all = manager.GetWindows(includeHidden: true);
        var visible = manager.GetWindows();

        Assert.IsTrue(all.Count >= visible.Count);
        if (!all.Any(w => !w.IsVisible)) {
            Assert.Inconclusive("No hidden windows found");
        }
    }
}
