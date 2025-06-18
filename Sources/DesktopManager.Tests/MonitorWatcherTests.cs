using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows")]
public class MonitorWatcherTests {
    [TestMethod]
    public void MonitorWatcher_CanBeCreated() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        using var watcher = new MonitorWatcher();
        Assert.IsNotNull(watcher);
    }
}
