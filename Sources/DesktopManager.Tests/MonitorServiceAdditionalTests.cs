using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorServiceAdditionalTests.
/// </summary>
public class MonitorServiceAdditionalTests {
    [TestMethod]
    /// <summary>
    /// Test for GetMonitorPosition_ThrowsWhenMonitorMissing.
    /// </summary>
    public void GetMonitorPosition_ThrowsWhenMonitorMissing() {
        var service = new MonitorService(new FakeDesktopManager());
        Assert.ThrowsExactly<ArgumentException>(() => service.GetMonitorPosition("missing"));
    }

    [TestMethod]
    /// <summary>
    /// Test for GetMonitorsConnected_ReturnsEmptyWhenNoDeviceIds.
    /// </summary>
    public void GetMonitorsConnected_ReturnsEmptyWhenNoDeviceIds() {
        var fake = new FakeDesktopManager { DevicePathCount = 2 };
        var service = new MonitorService(fake);

        var result = service.GetMonitorsConnected();

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    /// <summary>
    /// Null device paths from the desktop wallpaper API should remain listable without invoking device-scoped calls.
    /// </summary>
    public void GetMonitors_NullDevicePath_ReturnsPlaceholderWithoutDeviceCalls() {
        var fake = new FakeDesktopManager { DevicePathCount = 1 };
        fake.DevicePaths[0] = null!;
        var service = new MonitorService(fake);

        var result = service.GetMonitors();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(string.Empty, result[0].DeviceId);
        Assert.AreEqual(0, fake.GetWallpaperIds.Count);
    }

    [TestMethod]
    /// <summary>
    /// Test for GetMonitorBrightness_ThrowsWhenMonitorMissing.
    /// </summary>
    public void GetMonitorBrightness_ThrowsWhenMonitorMissing() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FakeDesktopManager());
        Assert.ThrowsExactly<InvalidOperationException>(() => service.GetMonitorBrightness("missing"));
    }

    [TestMethod]
    /// <summary>
    /// Test for SetMonitorBrightness_ThrowsWhenMonitorMissing.
    /// </summary>
    public void SetMonitorBrightness_ThrowsWhenMonitorMissing() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FakeDesktopManager());
        Assert.ThrowsExactly<InvalidOperationException>(() => service.SetMonitorBrightness("missing", 50));
    }
}
