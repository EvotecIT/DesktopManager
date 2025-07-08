using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for slideshow handle cleanup.
/// </summary>
public class MonitorServiceSlideshowHandleTests {
    private class ThrowingSlideshowDesktopManager : IDesktopManager {
        public IntPtr ReceivedPtr;
        public int RefCountAfterAddRef;

        public void SetWallpaper(string monitorId, string wallpaper) { }
        public string GetWallpaper(string monitorId) => string.Empty;
        public string GetMonitorDevicePathAt(uint monitorIndex) => string.Empty;
        public uint GetMonitorDevicePathCount() => 1;
        public RECT GetMonitorBounds(string monitorId) => new();
        public void SetBackgroundColor(uint color) { }
        public uint GetBackgroundColor() => 0;
        public void SetPosition(DesktopWallpaperPosition position) { }
        public DesktopWallpaperPosition GetPosition() => DesktopWallpaperPosition.Center;
        public void SetSlideshow(IntPtr items) {
            ReceivedPtr = items;
            RefCountAfterAddRef = Marshal.AddRef(items);
            throw new COMException();
        }
        public IntPtr GetSlideshow() => IntPtr.Zero;
        public void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick) { }
        public uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick) { options = DesktopSlideshowDirection.Forward; slideshowTick = 0; return 0; }
        public void AdvanceSlideshow(string monitorId, DesktopSlideshowDirection direction) { }
        public DesktopSlideshowDirection GetStatus() => DesktopSlideshowDirection.Forward;
        public bool Enable() => true;
    }

    [TestMethod]
    /// <summary>
    /// StartWallpaperSlideshow releases handles when SetSlideshow throws.
    /// </summary>
    public void StartWallpaperSlideshow_ReleasesHandleOnException() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        string path = Path.GetTempFileName();
        File.WriteAllBytes(path, Array.Empty<byte>());
        try {
            var fake = new ThrowingSlideshowDesktopManager();
            var service = new MonitorService(fake);
            Assert.ThrowsException<COMException>(() => service.StartWallpaperSlideshow(new[] { path }));
            Assert.AreNotEqual(IntPtr.Zero, fake.ReceivedPtr);
            int remaining = Marshal.Release(fake.ReceivedPtr);
            Assert.AreEqual(0, remaining);
        } finally {
            File.Delete(path);
        }
    }
}
