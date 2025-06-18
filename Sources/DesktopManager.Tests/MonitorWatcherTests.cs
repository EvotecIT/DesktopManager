using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorWatcherTests {
    [TestMethod]
    public void MonitorWatcher_CanBeCreated() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        using var watcher = new MonitorWatcher();
        Assert.IsNotNull(watcher);
    }
}
