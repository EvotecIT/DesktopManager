using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for window state helper methods.
/// </summary>
public class WindowStateHelpersTests {
    [TestMethod]
    /// <summary>
    /// Ensures MinimizeWindows and RestoreWindows toggle state.
    /// </summary>
    public void MinimizeAndRestoreWindows_TogglesState() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireForegroundWindowUiTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager State Harness");

        var manager = new WindowManager();
        bool initialMinimized = IsMinimized(harness.Window.Handle);

        manager.MinimizeWindows(new[] { harness.Window }, ignoreErrors: false);
        Application.DoEvents();
        Assert.IsTrue(IsMinimized(harness.Window.Handle) || initialMinimized);

        manager.RestoreWindows(new[] { harness.Window }, ignoreErrors: false);
        Application.DoEvents();
        Assert.IsFalse(IsMinimized(harness.Window.Handle));
    }

    [TestMethod]
    /// <summary>
    /// Ensures EnsureWindowOnScreen moves a window back to a visible monitor.
    /// </summary>
    public void EnsureWindowOnScreen_MovesOffscreenWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowMutationTests();

        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("DesktopManager OnScreen Harness");

        var manager = new WindowManager();
        manager.SetWindowPosition(harness.Window, -5000, -5000);

        bool moved = manager.EnsureWindowOnScreen(harness.Window);
        Application.DoEvents();
        Assert.IsTrue(moved);

        Assert.IsTrue(MonitorNativeMethods.GetWindowRect(harness.Window.Handle, out RECT rect));
        var monitors = new Monitors().GetMonitors();
        if (monitors.Count == 0) {
            Assert.Inconclusive("No monitors found for verification");
        }

        Assert.IsTrue(IsRectOnAnyMonitor(rect, monitors));
    }

    private static bool IsMinimized(IntPtr handle) {
        IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_STYLE);
        long style = stylePtr.ToInt64();
        return (style & MonitorNativeMethods.WS_MINIMIZE) != 0;
    }

    private static bool IsRectOnAnyMonitor(RECT rect, IEnumerable<Monitor> monitors) {
        foreach (var monitor in monitors) {
            var bounds = monitor.GetMonitorBounds();
            if (rect.Right <= bounds.Left || rect.Left >= bounds.Right ||
                rect.Bottom <= bounds.Top || rect.Top >= bounds.Bottom) {
                continue;
            }
            return true;
        }
        return false;
    }
}
