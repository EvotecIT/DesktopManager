using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorServiceAdditionalTests {
    [TestMethod]
    public void GetMonitorPosition_ThrowsWhenMonitorMissing() {
        var service = new MonitorService(new FakeDesktopManager());
        Assert.ThrowsException<ArgumentException>(() => service.GetMonitorPosition("missing"));
    }

    [TestMethod]
    public void GetMonitorsConnected_ReturnsEmptyWhenNoDeviceIds() {
        var fake = new FakeDesktopManager { DevicePathCount = 2 };
        var service = new MonitorService(fake);

        var result = service.GetMonitorsConnected();

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetMonitorBrightness_ThrowsWhenMonitorMissing() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FakeDesktopManager());
        Assert.ThrowsException<InvalidOperationException>(() => service.GetMonitorBrightness("missing"));
    }

    [TestMethod]
    public void SetMonitorBrightness_ThrowsWhenMonitorMissing() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FakeDesktopManager());
        Assert.ThrowsException<InvalidOperationException>(() => service.SetMonitorBrightness("missing", 50));
    }
}

