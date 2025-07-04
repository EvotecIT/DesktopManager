using System;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorFallbackTests.
/// </summary>
public class MonitorFallbackTests {
    private class FailingDesktopManager : IDesktopManager {
        /// <summary>
        /// Test for SetWallpaper.
        /// </summary>
        public void SetWallpaper(string monitorId, string wallpaper) => throw new COMException();
        /// <summary>
        /// Test for GetWallpaper.
        /// </summary>
        public string GetWallpaper(string monitorId) => throw new COMException();
        /// <summary>
        /// Test for GetMonitorDevicePathAt.
        /// </summary>
        public string GetMonitorDevicePathAt(uint monitorIndex) => throw new COMException();
        /// <summary>
        /// Test for GetMonitorDevicePathCount.
        /// </summary>
        public uint GetMonitorDevicePathCount() => throw new COMException();
        /// <summary>
        /// Test for GetMonitorBounds.
        /// </summary>
        public RECT GetMonitorBounds(string monitorId) => throw new COMException();
        /// <summary>
        /// Test for SetBackgroundColor.
        /// </summary>
        public void SetBackgroundColor(uint color) => throw new COMException();
        /// <summary>
        /// Test for GetBackgroundColor.
        /// </summary>
        public uint GetBackgroundColor() => throw new COMException();
        /// <summary>
        /// Test for SetPosition.
        /// </summary>
        public void SetPosition(DesktopWallpaperPosition position) => throw new COMException();
        /// <summary>
        /// Test for GetPosition.
        /// </summary>
        public DesktopWallpaperPosition GetPosition() => throw new COMException();
        /// <summary>
        /// Test for SetSlideshow.
        /// </summary>
        public void SetSlideshow(IntPtr items) => throw new COMException();
        /// <summary>
        /// Test for GetSlideshow.
        /// </summary>
        public IntPtr GetSlideshow() => throw new COMException();
        /// <summary>
        /// Test for SetSlideshowOptions.
        /// </summary>
        public void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick) => throw new COMException();
        /// <summary>
        /// Test for GetSlideshowOptions.
        /// </summary>
        public uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick) { throw new COMException(); }
        /// <summary>
        /// Test for AdvanceSlideshow.
        /// </summary>
        public void AdvanceSlideshow(string monitorId, DesktopSlideshowDirection direction) => throw new COMException();
        /// <summary>
        /// Test for GetStatus.
        /// </summary>
        public DesktopSlideshowDirection GetStatus() => throw new COMException();
        /// <summary>
        /// Test for Enable.
        /// </summary>
        public bool Enable() => throw new COMException();
    }

    [TestMethod]
    /// <summary>
    /// Test for EnumeratesMonitors_WhenDesktopManagerFails.
    /// </summary>
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
    /// <summary>
    /// Test for SetAndGetWallpaper_FallsBackToSystemParametersInfo.
    /// </summary>
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
    /// <summary>
    /// Test for SetAndGetWallpaperPosition_FallsBackToRegistry.
    /// </summary>
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

    [TestMethod]
    /// <summary>
    /// Test for SetAndGetBackgroundColor_FallsBackToRegistry.
    /// </summary>
    public void SetAndGetBackgroundColor_FallsBackToRegistry() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FailingDesktopManager());
        uint original = service.GetBackgroundColor();
        uint newColor = original == 0xFFFFFFu ? 0x0000FFu : 0xFFFFFFu;

        service.SetBackgroundColor(newColor);
        uint roundTrip = service.GetBackgroundColor();

        service.SetBackgroundColor(original);

        Assert.AreEqual(newColor, roundTrip);
    }
}

