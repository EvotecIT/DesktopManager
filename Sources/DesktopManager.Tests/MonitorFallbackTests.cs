using System;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorFallbackTests {
    private class FailingDesktopManager : IDesktopManager {
        public void SetWallpaper(string monitorId, string wallpaper) => throw new COMException();
        public string GetWallpaper(string monitorId) => throw new COMException();
        public string GetMonitorDevicePathAt(uint monitorIndex) => throw new COMException();
        public uint GetMonitorDevicePathCount() => throw new COMException();
        public RECT GetMonitorBounds(string monitorId) => throw new COMException();
        public void SetBackgroundColor(uint color) => throw new COMException();
        public uint GetBackgroundColor() => throw new COMException();
        public void SetPosition(DesktopWallpaperPosition position) => throw new COMException();
        public DesktopWallpaperPosition GetPosition() => throw new COMException();
        public void SetSlideshow(IntPtr items) => throw new COMException();
        public IntPtr GetSlideshow() => throw new COMException();
        public void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick) => throw new COMException();
        public uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick) { throw new COMException(); }
        public void AdvanceSlideshow(string monitorId, DesktopSlideshowDirection direction) => throw new COMException();
        public DesktopSlideshowDirection GetStatus() => throw new COMException();
        public bool Enable() => throw new COMException();
    }

    [TestMethod]
    public void EnumeratesMonitors_WhenDesktopManagerFails() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FailingDesktopManager());
        var monitors = service.GetMonitors();
        Assert.IsNotNull(monitors);
        Assert.IsTrue(monitors.Count > 0);
        var rect = service.GetMonitorBounds(monitors[0].DeviceId);
        Assert.IsTrue(rect.Right > rect.Left);
        Assert.IsTrue(rect.Bottom > rect.Top);
    }

    [TestMethod]
    public void SetAndGetWallpaper_FallsBackToSystemParametersInfo() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FailingDesktopManager());
        var monitors = service.GetMonitorsConnected();
        if (monitors.Count == 0) {
            Assert.Inconclusive("No monitors found");
        }

        string temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".bmp");
        using (var bmp = new System.Drawing.Bitmap(1, 1)) {
            bmp.Save(temp, System.Drawing.Imaging.ImageFormat.Bmp);
        }

        service.SetWallpaper(monitors[0].DeviceId, temp);
        var retrieved = service.GetWallpaper(monitors[0].DeviceId);

        Assert.AreEqual(temp, retrieved);
        System.IO.File.Delete(temp);
    }

    [TestMethod]
    public void SetAndGetWallpaperPosition_FallsBackToRegistry() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FailingDesktopManager());
        var original = service.GetWallpaperPosition();
        var newPos = original == DesktopWallpaperPosition.Center ? DesktopWallpaperPosition.Stretch : DesktopWallpaperPosition.Center;

        service.SetWallpaperPosition(newPos);
        var roundTrip = service.GetWallpaperPosition();

        service.SetWallpaperPosition(original);

        Assert.AreEqual(newPos, roundTrip);
    }
}

