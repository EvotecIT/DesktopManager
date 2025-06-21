using Microsoft.VisualStudio.TestTools.UnitTesting;

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
}

