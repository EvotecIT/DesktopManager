using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowTitleMatchingTests.
/// </summary>
public class WindowTitleMatchingTests
{
    [TestMethod]
    /// <summary>
    /// Test for GetWindows_TitleMatchingIsCaseInsensitive.
    /// </summary>
    public void GetWindows_TitleMatchingIsCaseInsensitive()
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
        string titleUpper = window.Title.ToUpperInvariant();
        string titleLower = window.Title.ToLowerInvariant();

        Assert.IsTrue(manager.GetWindows(titleUpper).Any(w => w.Handle == window.Handle));
        Assert.IsTrue(manager.GetWindows(titleLower).Any(w => w.Handle == window.Handle));
    }
}
