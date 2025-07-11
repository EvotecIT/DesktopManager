using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for WindowManager filtering features.
/// </summary>
public class WindowManagerFilterTests {
    [TestMethod]
    /// <summary>
    /// Ensures filtering by process name returns matching window.
    /// </summary>
    public void GetWindows_ProcessNameFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var proc = Process.GetProcessById((int)window.ProcessId);
        var filtered = manager.GetWindows(processName: proc.ProcessName);
        Assert.IsTrue(filtered.Any(w => w.Handle == window.Handle));
    }

    [TestMethod]
    /// <summary>
    /// Ensures filtering by class name returns matching window.
    /// </summary>
    public void GetWindows_ClassNameFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var sb = new StringBuilder(256);
        MonitorNativeMethods.GetClassName(window.Handle, sb, sb.Capacity);
        var filtered = manager.GetWindows(className: sb.ToString());
        Assert.IsTrue(filtered.Any(w => w.Handle == window.Handle));
    }

    [TestMethod]
    /// <summary>
    /// Ensures regex filtering returns matching window.
    /// </summary>
    public void GetWindows_RegexFilter_ReturnsWindow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var regex = new Regex(Regex.Escape(window.Title), RegexOptions.IgnoreCase);
        var filtered = manager.GetWindows(regex: regex);
        Assert.IsTrue(filtered.Any(w => w.Handle == window.Handle));
    }
}

