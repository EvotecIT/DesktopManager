using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorServiceCleanupTests {
    private class ThrowingWallpaperDesktopManager : IDesktopManager {
        public string? TempPath;
        public void SetWallpaper(string monitorId, string wallpaper) {
            TempPath = wallpaper;
            throw new COMException();
        }
        public string GetWallpaper(string monitorId) => "wall";
        public string GetMonitorDevicePathAt(uint monitorIndex) => "id";
        public uint GetMonitorDevicePathCount() => 1;
        public RECT GetMonitorBounds(string monitorId) => new();
        public void SetBackgroundColor(uint color) { }
        public uint GetBackgroundColor() => 0;
        public void SetPosition(DesktopWallpaperPosition position) { }
        public DesktopWallpaperPosition GetPosition() => DesktopWallpaperPosition.Center;
        public void SetSlideshow(IntPtr items) { }
        public IntPtr GetSlideshow() => IntPtr.Zero;
        public void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick) { }
        public uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick) { options = DesktopSlideshowDirection.Forward; slideshowTick = 0; return 0; }
        public void AdvanceSlideshow(string monitorId, DesktopSlideshowDirection direction) { }
        public DesktopSlideshowDirection GetStatus() => DesktopSlideshowDirection.Forward;
        public bool Enable() => true;
    }

    [TestMethod]
    public void SetWallpaper_FromStream_DeletesTempFileOnFailure() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        var fake = new ThrowingWallpaperDesktopManager();
        var service = new MonitorService(fake);
        using var ms = new MemoryStream(new byte[] {1,2,3});
        service.SetWallpaper("mon", ms);
        Assert.IsNotNull(fake.TempPath);
        Assert.IsFalse(File.Exists(fake.TempPath!));
    }

    [TestMethod]
    public void SetWallpaper_AllMonitors_FromStream_DeletesTempFileOnFailure() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        var fake = new ThrowingWallpaperDesktopManager();
        var service = new MonitorService(fake);
        using var ms = new MemoryStream(new byte[] {1,2,3});
        service.SetWallpaper(ms);
        Assert.IsNotNull(fake.TempPath);
        Assert.IsFalse(File.Exists(fake.TempPath!));
    }
}
