using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorServiceCleanupTests.
/// </summary>
public class MonitorServiceCleanupTests {
    private class ThrowingWallpaperDesktopManager : IDesktopManager {
        public string? TempPath;
        /// <summary>
        /// Test for SetWallpaper.
        /// </summary>
        public void SetWallpaper(string monitorId, string wallpaper) {
            TempPath = wallpaper;
            throw new COMException();
        }
        /// <summary>
        /// Test for GetWallpaper.
        /// </summary>
        public string GetWallpaper(string monitorId) => "wall";
        /// <summary>
        /// Test for GetMonitorDevicePathAt.
        /// </summary>
        public string GetMonitorDevicePathAt(uint monitorIndex) => "id";
        /// <summary>
        /// Test for GetMonitorDevicePathCount.
        /// </summary>
        public uint GetMonitorDevicePathCount() => 1;
        /// <summary>
        /// Test for GetMonitorBounds.
        /// </summary>
        public RECT GetMonitorBounds(string monitorId) => new();
        /// <summary>
        /// Test for SetBackgroundColor.
        /// </summary>
        public void SetBackgroundColor(uint color) { }
        /// <summary>
        /// Test for GetBackgroundColor.
        /// </summary>
        public uint GetBackgroundColor() => 0;
        /// <summary>
        /// Test for SetPosition.
        /// </summary>
        public void SetPosition(DesktopWallpaperPosition position) { }
        /// <summary>
        /// Test for GetPosition.
        /// </summary>
        public DesktopWallpaperPosition GetPosition() => DesktopWallpaperPosition.Center;
        /// <summary>
        /// Test for SetSlideshow.
        /// </summary>
        public void SetSlideshow(IntPtr items) { }
        /// <summary>
        /// Test for GetSlideshow.
        /// </summary>
        public IntPtr GetSlideshow() => IntPtr.Zero;
        /// <summary>
        /// Test for SetSlideshowOptions.
        /// </summary>
        public void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick) { }
        /// <summary>
        /// Test for GetSlideshowOptions.
        /// </summary>
        public uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick) { options = DesktopSlideshowDirection.Forward; slideshowTick = 0; return 0; }
        /// <summary>
        /// Test for AdvanceSlideshow.
        /// </summary>
        public void AdvanceSlideshow(string monitorId, DesktopSlideshowDirection direction) { }
        /// <summary>
        /// Test for GetStatus.
        /// </summary>
        public DesktopSlideshowDirection GetStatus() => DesktopSlideshowDirection.Forward;
        /// <summary>
        /// Test for Enable.
        /// </summary>
        public bool Enable() => true;
    }

    private class ThrowingStream : Stream {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        /// <summary>
        /// Test for Flush.
        /// </summary>
        public override void Flush() { }
        /// <summary>
        /// Test for Read.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException();
        /// <summary>
        /// Test for Seek.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <summary>
        /// Test for SetLength.
        /// </summary>
        public override void SetLength(long value) => throw new NotSupportedException();
        /// <summary>
        /// Test for Write.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [TestMethod]
    /// <summary>
    /// Test for SetWallpaper_FromStream_DeletesTempFileOnFailure.
    /// </summary>
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
    /// <summary>
    /// Test for SetWallpaper_AllMonitors_FromStream_DeletesTempFileOnFailure.
    /// </summary>
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
    /// <summary>
    /// Test for SetWallpaper_FromStream_DeletesTempFileOnWriteFailure.
    /// </summary>
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
    /// <summary>
    /// Test for SetWallpaper_AllMonitors_FromStream_DeletesTempFileOnWriteFailure.
    /// </summary>
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
