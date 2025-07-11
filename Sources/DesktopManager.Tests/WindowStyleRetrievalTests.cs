using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for verifying window style retrieval on 64-bit processes.
/// </summary>
public class WindowStyleRetrievalTests {
    [TestMethod]
    /// <summary>
    /// Ensures GetWindowPosition returns the correct state when running on 64-bit.
    /// </summary>
    public void GetWindowPosition_ReturnsCorrectState_On64Bit() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        if (IntPtr.Size != 8) {
            Assert.Inconclusive("Test requires 64-bit process");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var position = manager.GetWindowPosition(window);
        long expectedStyle = MonitorNativeMethods.GetWindowLongPtr(window.Handle, MonitorNativeMethods.GWL_STYLE).ToInt64();
        var expectedState = WindowState.Normal;
        if ((expectedStyle & MonitorNativeMethods.WS_MINIMIZE) != 0) {
            expectedState = WindowState.Minimize;
        } else if ((expectedStyle & MonitorNativeMethods.WS_MAXIMIZE) != 0) {
            expectedState = WindowState.Maximize;
        }

        Assert.AreEqual(expectedState, position.State);
    }
}
