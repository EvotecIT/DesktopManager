using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorServiceCaseInsensitiveTests.
/// </summary>
public class MonitorServiceCaseInsensitiveTests {
    [TestMethod]
    /// <summary>
    /// Test for GetMonitorPosition_IsCaseInsensitive.
    /// </summary>
    public void GetMonitorPosition_IsCaseInsensitive() {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var fake = new FakeDesktopManager();
        fake.DevicePaths[0] = "MON1";
        var service = new MonitorService(fake);

        var pos = service.GetMonitorPosition("mon1");

        Assert.AreEqual(0, pos.Left);
        Assert.AreEqual(0, pos.Top);
        Assert.AreEqual(10, pos.Right);
        Assert.AreEqual(10, pos.Bottom);
    }
}
