using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for WindowManager.WaitWindow.
/// </summary>
public class WindowManagerWaitTests {
    [TestMethod]
    /// <summary>
    /// WaitWindow returns when a window exists.
    /// </summary>
    public void WaitWindow_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var target = windows.First();
        var result = manager.WaitWindow(target.Title, 1000);
        Assert.AreEqual(target.Handle, result.Handle);
    }

    [TestMethod]
    /// <summary>
    /// WaitWindow throws when the timeout is reached.
    /// </summary>
    public void WaitWindow_Timeout_Throws() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        Assert.ThrowsException<TimeoutException>(() => manager.WaitWindow("__nonexistent__" + Guid.NewGuid(), 200));
    }
}
