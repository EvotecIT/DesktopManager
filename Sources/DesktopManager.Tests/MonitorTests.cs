using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorTests.
/// </summary>
public class MonitorTests {
    private static void SetId(Monitor m, string id) {
        typeof(Monitor).GetProperty("DeviceId")!.SetValue(m, id);
    }

    [TestMethod]
    /// <summary>
    /// Test for SetWallpaper_ForwardsCall.
    /// </summary>
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
    /// <summary>
    /// Test for GetWallpaper_ForwardsCallAndReturnsValue.
    /// </summary>
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

    [TestMethod]
    /// <summary>
    /// Position reads for partially identified monitors should use the cached rectangle instead of requiring a device id.
    /// </summary>
    public void GetMonitorPosition_EmptyDeviceId_ReturnsCachedRectangle() {
        var service = new MonitorService(new FakeDesktopManager());
        var monitor = new Monitor(service) {
            Rect = new RECT {
                Left = -100,
                Top = 20,
                Right = 1180,
                Bottom = 740
            }
        };

        MonitorPosition position = monitor.GetMonitorPosition();

        Assert.AreEqual(-100, position.Left);
        Assert.AreEqual(20, position.Top);
        Assert.AreEqual(1180, position.Right);
        Assert.AreEqual(740, position.Bottom);
    }
}
