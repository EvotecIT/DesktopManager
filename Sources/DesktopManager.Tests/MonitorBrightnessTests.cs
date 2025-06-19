using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorBrightnessTests {
    [TestMethod]
    public void GetAndSetBrightness_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var monitors = new Monitors().GetMonitorsConnected();
        if (monitors.Count == 0) {
            Assert.Inconclusive("No monitors found");
        }

        var monitor = monitors.First();
        var service = new Monitors();
        int current = service.GetMonitorBrightness(monitor.DeviceId);
        service.SetMonitorBrightness(monitor.DeviceId, current);
    }
}
