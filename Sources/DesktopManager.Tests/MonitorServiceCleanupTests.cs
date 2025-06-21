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

    private class ThrowingStream : Stream {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [TestMethod]
    public void SetWallpaper_FromStream_DeletesTempFileOnFailure() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        var fake = new ThrowingWallpaperDesktopManager();
        var service = new MonitorService(fake);
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
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
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        service.SetWallpaper(ms);
        Assert.IsNotNull(fake.TempPath);
        Assert.IsFalse(File.Exists(fake.TempPath!));
    }

    [TestMethod]
    public void SetWallpaper_FromStream_DeletesTempFileOnWriteFailure() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        string? oldTemp = Environment.GetEnvironmentVariable("TEMP");
        string? oldTmp = Environment.GetEnvironmentVariable("TMP");
        Environment.SetEnvironmentVariable("TEMP", dir);
        Environment.SetEnvironmentVariable("TMP", dir);
        try {
            var fake = new FakeDesktopManager();
            var service = new MonitorService(fake);
            using var stream = new ThrowingStream();
            Assert.ThrowsException<IOException>(() => service.SetWallpaper("mon", stream));
            Assert.AreEqual(0, Directory.GetFiles(dir).Length);
        } finally {
            Environment.SetEnvironmentVariable("TEMP", oldTemp);
            Environment.SetEnvironmentVariable("TMP", oldTmp);
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void SetWallpaper_AllMonitors_FromStream_DeletesTempFileOnWriteFailure() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        string? oldTemp = Environment.GetEnvironmentVariable("TEMP");
        string? oldTmp = Environment.GetEnvironmentVariable("TMP");
        Environment.SetEnvironmentVariable("TEMP", dir);
        Environment.SetEnvironmentVariable("TMP", dir);
        try {
            var fake = new FakeDesktopManager();
            var service = new MonitorService(fake);
            using var stream = new ThrowingStream();
            Assert.ThrowsException<IOException>(() => service.SetWallpaper(stream));
            Assert.AreEqual(0, Directory.GetFiles(dir).Length);
        } finally {
            Environment.SetEnvironmentVariable("TEMP", oldTemp);
            Environment.SetEnvironmentVariable("TMP", oldTmp);
            Directory.Delete(dir, true);
        }
    }
}
