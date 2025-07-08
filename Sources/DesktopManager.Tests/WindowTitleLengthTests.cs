using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for verifying window titles are retrieved without truncation.
/// </summary>
public class WindowTitleLengthTests {
    [TestMethod]
    /// <summary>
    /// Ensures GetWindows returns complete window titles.
    /// </summary>
    public void GetWindows_TitleIsNotTruncated() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        int expectedLength = MonitorNativeMethods.GetWindowTextLength(window.Handle);
        Assert.AreEqual(expectedLength, window.Title.Length);
    }
}

