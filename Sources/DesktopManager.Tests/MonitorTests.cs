using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorTests {
    private static void SetId(Monitor m, string id) {
        typeof(Monitor).GetProperty("DeviceId")!.SetValue(m, id);
    }

    [TestMethod]
    public void SetWallpaper_ForwardsCall() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        var monitor = new Monitor(service);
        SetId(monitor, "id");

        monitor.SetWallpaper("path");

        Assert.AreEqual(1, fake.SetWallpaperCalls.Count);
        Assert.AreEqual(("id", "path"), fake.SetWallpaperCalls[0]);
    }

    [TestMethod]
    public void GetWallpaper_ForwardsCallAndReturnsValue() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        var monitor = new Monitor(service);
        SetId(monitor, "x");

        string result = monitor.GetWallpaper();

        Assert.AreEqual(1, fake.GetWallpaperIds.Count);
        Assert.AreEqual("x", fake.GetWallpaperIds[0]);
        Assert.AreEqual("wall", result);
    }
}
