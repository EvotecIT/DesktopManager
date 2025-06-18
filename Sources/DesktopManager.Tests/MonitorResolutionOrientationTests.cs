using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorResolutionOrientationTests {
    [TestMethod]
    public void SetResolutionAndOrientation_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var monitors = new Monitors().GetMonitorsConnected();
        if (monitors.Count == 0) {
            Assert.Inconclusive("No monitors found to test");
        }

        var monitor = monitors.First();
        var position = monitor.GetMonitorPosition();
        int width = position.Right - position.Left;
        int height = position.Bottom - position.Top;

        var manager = new Monitors();
        manager.SetMonitorResolution(monitor.DeviceId, width, height);
        manager.SetMonitorOrientation(monitor.DeviceId, DisplayOrientation.Default);
    }
}

