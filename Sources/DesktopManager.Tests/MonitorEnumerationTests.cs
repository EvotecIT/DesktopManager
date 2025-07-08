using System.Runtime.InteropServices;
using System.Linq;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorEnumerationTests.
/// </summary>
public class MonitorEnumerationTests
{
    [TestMethod]
    /// <summary>
    /// Test for GetMonitorsConnected_ReturnsAtLeastOne.
    /// </summary>
    public void GetMonitorsConnected_ReturnsAtLeastOne()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("Test requires Windows");
        }

        var monitors = new Monitors().GetMonitorsConnected();
        Assert.IsNotNull(monitors);
        Assert.IsTrue(monitors.Count > 0, "No monitors were returned");
    }

    [TestMethod]
    /// <summary>
    /// Ensures enumerated monitors provide a non-empty device name.
    /// </summary>
    public void GetMonitorsConnected_DeviceNamesAvailable()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("Test requires Windows");
        }

        var monitors = new Monitors().GetMonitorsConnected();
        Assert.IsTrue(monitors.All(m => !string.IsNullOrEmpty(m.DeviceName)), "DeviceName missing");
    }
}
