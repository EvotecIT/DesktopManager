using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class WindowPositionTests
{
    [TestMethod]
    public void GetAndSetWindowPosition_RoundTrips()
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
        var original = manager.GetWindowPosition(window);

        manager.SetWindowPosition(window, original.Left, original.Top);
        var updated = manager.GetWindowPosition(window);

        Assert.AreEqual(original.Left, updated.Left);
        Assert.AreEqual(original.Top, updated.Top);
    }
}
