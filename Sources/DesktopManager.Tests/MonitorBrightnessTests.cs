using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorBrightnessTests.
/// </summary>
public class MonitorBrightnessTests {
    [TestMethod]
    /// <summary>
    /// Test for GetAndSetBrightness_DoesNotThrow.
    /// </summary>
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
        try {
            int current = service.GetMonitorBrightness(monitor.DeviceId);
            service.SetMonitorBrightness(monitor.DeviceId, current);
        } catch (InvalidOperationException ex) {
            Assert.Inconclusive($"Brightness not supported: {ex.Message}");
        }
    }
}
