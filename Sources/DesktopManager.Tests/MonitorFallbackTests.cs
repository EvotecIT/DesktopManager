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
}

