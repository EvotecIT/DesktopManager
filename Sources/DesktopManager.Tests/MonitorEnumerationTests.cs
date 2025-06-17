using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorEnumerationTests
{
    [TestMethod]
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
}
