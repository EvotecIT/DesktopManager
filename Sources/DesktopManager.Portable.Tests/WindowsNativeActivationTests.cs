using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager.Portable.Tests;

[TestClass]
[SupportedOSPlatform("windows")]
public sealed class WindowsNativeActivationTests {
    [TestMethod]
    public void MonitorDiscovery_WorksWhenAnotherCallerAlreadyActivatedTheNativeComObject() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !Environment.UserInteractive) {
            Assert.Inconclusive("This read-only COM identity regression requires an interactive native Windows session.");
        }

        var existing = (IDesktopManager)Activator.CreateInstance(Type.GetTypeFromCLSID(
            new Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD"))!)!;
        var expected = Enumerable.Range(0, (int)existing.GetMonitorDevicePathCount())
            .Select(index => existing.GetMonitorDevicePathAt((uint)index)).FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
        if (expected == null) {
            Assert.Inconclusive("An addressable native wallpaper monitor is required for discovery qualification.");
        }
        var actual = new Monitors().GetMonitors();

        Assert.IsTrue(actual.Any(monitor => string.Equals(monitor.DeviceId, expected, StringComparison.OrdinalIgnoreCase)),
            "Monitor discovery must reuse the native COM identity through its interface, even when another caller created its RCW first.");
        GC.KeepAlive(existing);
    }
}
