using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class WindowLayoutTests {
    [TestMethod]
    public void SaveAndLoadLayout_RoundTrips() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0) {
            Assert.Inconclusive("No windows found to test");
        }

        var window = windows.First();
        var original = manager.GetWindowPosition(window);
        var path = System.IO.Path.GetTempFileName();

        manager.SaveLayout(path);
        manager.SetWindowPosition(window, original.Left + 20, original.Top + 20);
        manager.LoadLayout(path);
        var restored = manager.GetWindowPosition(window);

        Assert.AreEqual(original.Left, restored.Left);
        Assert.AreEqual(original.Top, restored.Top);

        System.IO.File.Delete(path);
    }
}
