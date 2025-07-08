using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows")]
/// <summary>
/// Test class for MonitorWatcherTests.
/// </summary>
public class MonitorWatcherTests {
    [TestMethod]
    /// <summary>
    /// Test for MonitorWatcher_CanBeCreated.
    /// </summary>
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

    [TestMethod]
    /// <summary>
    /// Ensures finalizer does not throw after disposal.
    /// </summary>
    public void MonitorWatcher_FinalizerSafeAfterDispose() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        var watcher = new MonitorWatcher();
        watcher.Dispose();
        watcher = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.IsTrue(true);
    }
}
