using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorServiceTests {
    [TestMethod]
    public void Constructor_CallsEnable() {
        var fake = new FakeDesktopManager();
        _ = new MonitorService(fake);
        Assert.IsTrue(fake.EnableCalled);
    }

    [TestMethod]
    public void SetWallpaper_ForwardsCall() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.SetWallpaper("mon", "wall");
        Assert.AreEqual(("mon", "wall"), fake.SetWallpaperCalls[0]);
    }

    [TestMethod]
    public void GetWallpaper_ForwardsCall() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        var result = service.GetWallpaper("m");
        Assert.AreEqual("wall", result);
        Assert.AreEqual("m", fake.GetWallpaperIds[0]);
    }

    [TestMethod]
    public void SetWallpaper_ByIndex_UsesDevicePath() {
        var fake = new FakeDesktopManager();
        fake.DevicePaths[0] = "dev";
        var service = new MonitorService(fake);
        service.SetWallpaper(0, "w");
        Assert.AreEqual(("dev", "w"), fake.SetWallpaperCalls[0]);
    }
    [TestMethod]
    public void SetWallpaper_ByIndex_MissingPathUsesEmptyString() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.SetWallpaper(1, "img");
        Assert.AreEqual(("", "img"), fake.SetWallpaperCalls[0]);
    }


    [TestMethod]
    public void SetWallpaper_FromStream_DeletesTempFile() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        using var ms = new MemoryStream(new byte[] {1,2,3});
        service.SetWallpaper("mon", ms);
        string path = fake.SetWallpaperCalls[0].path;
        Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    public void SetWallpaper_StreamNull_Throws() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        Assert.ThrowsException<NullReferenceException>(() => service.SetWallpaper("id", (Stream)null));
    }

    [TestMethod]
    public void SetWallpaper_FromUrl_InvalidSchemeThrows() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        string temp = Path.GetTempFileName();
        File.WriteAllBytes(temp, new byte[] {1});
        try {
            Assert.ThrowsException<NotSupportedException>(() => service.SetWallpaperFromUrl("m", new Uri(temp).AbsoluteUri));
        } finally {
            File.Delete(temp);
        }
    }

    [TestMethod]
    public void GetWallpaper_ByIndex_ForwardsCall() {
        var fake = new FakeDesktopManager();
        fake.DevicePaths[0] = "d";
        var service = new MonitorService(fake);
        var res = service.GetWallpaper(0);
        Assert.AreEqual("wall", res);
        Assert.AreEqual("d", fake.GetWallpaperIds[0]);
    }

    [TestMethod]
    public void SetWallpaperPosition_Forwards() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.SetWallpaperPosition(DesktopWallpaperPosition.Span);
        Assert.AreEqual(DesktopWallpaperPosition.Span, fake.WallpaperPosition);
    }

    [TestMethod]
    public void GetWallpaperPosition_Forwards() {
        var fake = new FakeDesktopManager();
        fake.WallpaperPosition = DesktopWallpaperPosition.Tile;
        var service = new MonitorService(fake);
        Assert.AreEqual(DesktopWallpaperPosition.Tile, service.GetWallpaperPosition());
    }

    [TestMethod]
    public void SetBackgroundColor_Forwards() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.SetBackgroundColor(5);
        Assert.AreEqual((uint)5, fake.BackgroundColor);
    }

    [TestMethod]
    public void GetBackgroundColor_Forwards() {
        var fake = new FakeDesktopManager { BackgroundColor = 7 };
        var service = new MonitorService(fake);
        Assert.AreEqual((uint)7, service.GetBackgroundColor());
    }

    [TestMethod]
    public void StopWallpaperSlideshow_CallsSetSlideshowWithZero() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.StopWallpaperSlideshow();
        Assert.AreEqual(IntPtr.Zero, fake.SetSlideshowCalls[0]);
    }

    [TestMethod]
    public void AdvanceWallpaperSlide_ForwardsDirection() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.AdvanceWallpaperSlide(DesktopSlideshowDirection.Backward);
        Assert.AreEqual(DesktopSlideshowDirection.Backward, fake.LastAdvanceDirection);
    }

    [TestMethod]
    public void StartWallpaperSlideshow_ThrowsOnNull() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        Assert.ThrowsException<ArgumentNullException>(() => service.StartWallpaperSlideshow(null));
    }

    [TestMethod]
    public void GetMonitorDevicePathAt_Forwards() {
        var fake = new FakeDesktopManager();
        fake.DevicePaths[0] = "xx";
        var service = new MonitorService(fake);
        Assert.AreEqual("xx", service.GetMonitorDevicePathAt(0));
    }

    [TestMethod]
    public void GetMonitorBounds_Forwards() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        var rect = service.GetMonitorBounds("id");
        Assert.AreEqual(10, rect.Right);
    }
}
