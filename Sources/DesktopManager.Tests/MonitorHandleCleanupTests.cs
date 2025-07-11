using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for handle tracking and cleanup in MonitorService.
/// </summary>
public class MonitorHandleCleanupTests {
    [TestMethod]
    /// <summary>
    /// GetMonitorBrightness releases monitor handles when completed.
    /// </summary>
    public void GetMonitorBrightness_ReleasesHandles() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FakeDesktopManager());
        var monitors = service.GetMonitorsConnected();
        if (monitors.Count == 0) {
            Assert.Inconclusive("No monitors found");
        }

        var field = typeof(MonitorService).GetField("_monitorHandles", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field);
        var list = (List<PHYSICAL_MONITOR>)field!.GetValue(service)!;

        string deviceId = monitors[0].DeviceId;
        try {
            service.GetMonitorBrightness(deviceId);
        } catch (Exception ex) {
            Console.WriteLine($"GetMonitorBrightness threw: {ex.Message}");
        }

        Assert.AreEqual(0, list.Count);
    }
}
