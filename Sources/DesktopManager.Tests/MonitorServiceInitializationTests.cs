using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorServiceInitializationTests {
    private class FailingEnableDesktopManager : IDesktopManager {
        public void SetWallpaper(string monitorId, string wallpaper) { }
        public string GetWallpaper(string monitorId) => string.Empty;
        public string GetMonitorDevicePathAt(uint monitorIndex) => string.Empty;
        public uint GetMonitorDevicePathCount() => 0;
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
        public bool Enable() => throw new COMException();
    }

    [TestMethod]
    public void Constructor_LogsMessage_WhenEnableThrows() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        using var sw = new System.IO.StringWriter();
        var original = Console.Out;
        Console.SetOut(sw);
        new MonitorService(new FailingEnableDesktopManager());
        Console.SetOut(original);

        StringAssert.Contains(sw.ToString(), "DesktopManager initialization failed");
    }
}

