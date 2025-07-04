using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorServiceTests.
/// </summary>
public class MonitorServiceTests {
    [TestMethod]
    /// <summary>
    /// Test for Constructor_CallsEnable.
    /// </summary>
    public void Constructor_CallsEnable() {
        var fake = new FakeDesktopManager();
        _ = new MonitorService(fake);
        Assert.IsTrue(fake.EnableCalled);
    }

    [TestMethod]
    /// <summary>
    /// Test for SetWallpaper_ForwardsCall.
    /// </summary>
    public void SetWallpaper_ForwardsCall() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.SetWallpaper("mon", "wall");
        Assert.AreEqual(("mon", "wall"), fake.SetWallpaperCalls[0]);
    }

    [TestMethod]
    /// <summary>
    /// Test for GetWallpaper_ForwardsCall.
    /// </summary>
    public void GetWallpaper_ForwardsCall() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        var result = service.GetWallpaper("m");
        Assert.AreEqual("wall", result);
        Assert.AreEqual("m", fake.GetWallpaperIds[0]);
    }

    [TestMethod]
    /// <summary>
    /// Test for SetWallpaper_ByIndex_UsesDevicePath.
    /// </summary>
    public void SetWallpaper_ByIndex_UsesDevicePath() {
        var fake = new FakeDesktopManager();
        fake.DevicePaths[0] = "dev";
        var service = new MonitorService(fake);
        service.SetWallpaper(0, "w");
        Assert.AreEqual(("dev", "w"), fake.SetWallpaperCalls[0]);
    }
    [TestMethod]
    /// <summary>
    /// Test for SetWallpaper_ByIndex_MissingPathUsesEmptyString.
    /// </summary>
    public void SetWallpaper_ByIndex_MissingPathUsesEmptyString() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.SetWallpaper(1, "img");
        Assert.AreEqual(("", "img"), fake.SetWallpaperCalls[0]);
    }


    [TestMethod]
    /// <summary>
    /// Test for SetWallpaper_FromStream_DeletesTempFile.
    /// </summary>
    public void SetWallpaper_FromStream_DeletesTempFile() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        using var ms = new MemoryStream(new byte[] {1,2,3});
        service.SetWallpaper("mon", ms);
        string path = fake.SetWallpaperCalls[0].path;
        Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    /// <summary>
    /// Test for SetWallpaper_StreamNull_Throws.
    /// </summary>
    public void SetWallpaper_StreamNull_Throws() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        Assert.ThrowsException<NullReferenceException>(() => service.SetWallpaper("id", (Stream)null));
    }

    [TestMethod]
    /// <summary>
    /// Test for SetWallpaper_FromUrl_InvalidSchemeThrows.
    /// </summary>
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
    /// <summary>
    /// Test for GetWallpaper_ByIndex_ForwardsCall.
    /// </summary>
    public void GetWallpaper_ByIndex_ForwardsCall() {
        var fake = new FakeDesktopManager();
        fake.DevicePaths[0] = "d";
        var service = new MonitorService(fake);
        var res = service.GetWallpaper(0);
        Assert.AreEqual("wall", res);
        Assert.AreEqual("d", fake.GetWallpaperIds[0]);
    }

    [TestMethod]
    /// <summary>
    /// Test for SetWallpaperPosition_Forwards.
    /// </summary>
    public void SetWallpaperPosition_Forwards() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.SetWallpaperPosition(DesktopWallpaperPosition.Span);
        Assert.AreEqual(DesktopWallpaperPosition.Span, fake.WallpaperPosition);
    }

    [TestMethod]
    /// <summary>
    /// Test for GetWallpaperPosition_Forwards.
    /// </summary>
    public void GetWallpaperPosition_Forwards() {
        var fake = new FakeDesktopManager();
        fake.WallpaperPosition = DesktopWallpaperPosition.Tile;
        var service = new MonitorService(fake);
        Assert.AreEqual(DesktopWallpaperPosition.Tile, service.GetWallpaperPosition());
    }

    [TestMethod]
    /// <summary>
    /// Test for SetBackgroundColor_Forwards.
    /// </summary>
    public void SetBackgroundColor_Forwards() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.SetBackgroundColor(5);
        Assert.AreEqual((uint)5, fake.BackgroundColor);
    }

    [TestMethod]
    /// <summary>
    /// Test for GetBackgroundColor_Forwards.
    /// </summary>
    public void GetBackgroundColor_Forwards() {
        var fake = new FakeDesktopManager { BackgroundColor = 7 };
        var service = new MonitorService(fake);
        Assert.AreEqual((uint)7, service.GetBackgroundColor());
    }

    [TestMethod]
    /// <summary>
    /// Test for StopWallpaperSlideshow_CallsSetSlideshowWithZero.
    /// </summary>
    public void StopWallpaperSlideshow_CallsSetSlideshowWithZero() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.StopWallpaperSlideshow();
        Assert.AreEqual(IntPtr.Zero, fake.SetSlideshowCalls[0]);
    }

    [TestMethod]
    /// <summary>
    /// Test for AdvanceWallpaperSlide_ForwardsDirection.
    /// </summary>
    public void AdvanceWallpaperSlide_ForwardsDirection() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        service.AdvanceWallpaperSlide(DesktopSlideshowDirection.Backward);
        Assert.AreEqual(DesktopSlideshowDirection.Backward, fake.LastAdvanceDirection);
    }

    [TestMethod]
    /// <summary>
    /// Test for StartWallpaperSlideshow_ThrowsOnNull.
    /// </summary>
    public void StartWallpaperSlideshow_ThrowsOnNull() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        Assert.ThrowsException<ArgumentNullException>(() => service.StartWallpaperSlideshow(null));
    }

    [TestMethod]
    /// <summary>
    /// Test for GetMonitorDevicePathAt_Forwards.
    /// </summary>
    public void GetMonitorDevicePathAt_Forwards() {
        var fake = new FakeDesktopManager();
        fake.DevicePaths[0] = "xx";
        var service = new MonitorService(fake);
        Assert.AreEqual("xx", service.GetMonitorDevicePathAt(0));
    }

    [TestMethod]
    /// <summary>
    /// Test for GetMonitorBounds_Forwards.
    /// </summary>
    public void GetMonitorBounds_Forwards() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        var rect = service.GetMonitorBounds("id");
        Assert.AreEqual(10, rect.Right);
    }
}
