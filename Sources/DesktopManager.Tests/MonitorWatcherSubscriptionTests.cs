using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows")]
public class MonitorWatcherSubscriptionTests {
    [TestMethod]
    public void DisplaySettingsChanged_NotDuplicated_ForMultipleWatchers() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        using var w1 = new MonitorWatcher();
        using var w2 = new MonitorWatcher();
        int c1 = 0;
        int c2 = 0;
        w1.DisplaySettingsChanged += (_, _) => c1++;
        w2.DisplaySettingsChanged += (_, _) => c2++;
        var method = typeof(MonitorWatcher).GetMethod("OnDisplaySettingsChangedStatic", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        method.Invoke(null, new object?[] { null, EventArgs.Empty });
        Assert.AreEqual(1, c1);
        Assert.AreEqual(1, c2);
    }

    [TestMethod]
    public void DisplaySettingsChanged_NotRaised_WhenAllWatchersDisposed() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        var watcher = new MonitorWatcher();
        int count = 0;
        watcher.DisplaySettingsChanged += (_, _) => count++;
        watcher.Dispose();
        var method = typeof(MonitorWatcher).GetMethod("OnDisplaySettingsChangedStatic", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        method.Invoke(null, new object?[] { null, EventArgs.Empty });
        Assert.AreEqual(0, count);
    }
}
