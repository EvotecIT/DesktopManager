using System.IO;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class WindowLayoutTests {
    [TestMethod]
    public void SaveAndLoadLayout_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var path = System.IO.Path.GetTempFileName();
        manager.SaveLayout(path);
        Assert.IsTrue(File.Exists(path));
        manager.LoadLayout(path);
        File.Delete(path);
    }
}
