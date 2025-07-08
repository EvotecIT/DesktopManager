using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows")]
/// <summary>Tests for monitor power events.</summary>
public class MonitorWatcherPowerTests {
    [TestMethod]
    /// <summary>Verify MonitorPoweredOff is raised.</summary>
    public void MonitorPoweredOff_EventRaised() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        using var watcher = new MonitorWatcher();
        bool raised = false;
        watcher.MonitorPoweredOff += (_, _) => raised = true;
        watcher.ProcessPowerBroadcast(0);
        Assert.IsTrue(raised);
    }

    [TestMethod]
    /// <summary>Verify MonitorPoweredOn is raised.</summary>
    public void MonitorPoweredOn_EventRaised() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        using var watcher = new MonitorWatcher();
        bool raised = false;
        watcher.MonitorPoweredOn += (_, _) => raised = true;
        watcher.ProcessPowerBroadcast(1);
        Assert.IsTrue(raised);
    }
}

