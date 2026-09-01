using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorServiceTests.
/// </summary>
public class MonitorServiceTests {
    private sealed class RecordingMonitorService : MonitorService {
        public RecordingMonitorService(IDesktopManager desktopManager)
            : base(desktopManager) {
        }

        public List<string> SystemWallpaperPaths { get; } = new();

        internal override void SetSystemWallpaper(string path) {
            SystemWallpaperPaths.Add(path);
        }
    }

    private static string CreateHistoryPath() {
        return Path.Combine(
            Path.GetTempPath(),
            "DesktopManager.Tests",
            nameof(MonitorServiceTests),
            Guid.NewGuid().ToString("N"),
            "wallpaper-history.json");
    }

    private static void WithIsolatedWallpaperHistory(Action<string> action) {
        string historyPath = CreateHistoryPath();
        Environment.SetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH", historyPath);
        try {
            action(historyPath);
        }
        finally {
            Environment.SetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH", null);
            string? directory = Path.GetDirectoryName(historyPath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) {
                Directory.Delete(directory, true);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Test for Constructor_DoesNotCallEnable.
    /// </summary>
    public void Constructor_DoesNotCallEnable() {
        var fake = new FakeDesktopManager();
        _ = new MonitorService(fake);
        Assert.IsFalse(fake.EnableCalled);
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
        Assert.IsTrue(fake.EnableCalled);
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
        WithIsolatedWallpaperHistory(_ => {
            var fake = new FakeDesktopManager();
            fake.DevicePaths[0] = "dev";
            var service = new MonitorService(fake);

            service.SetWallpaper(0, "w");

            Assert.AreEqual(("dev", "w"), fake.SetWallpaperCalls[0]);
            CollectionAssert.AreEqual(new[] { "w" }, WallpaperHistory.GetHistory());
        });
    }

    [TestMethod]
    /// <summary>
    /// A valid monitor slot without a device ID should use the session-wide wallpaper fallback.
    /// </summary>
    public void SetWallpaper_ByIndex_NullDevicePath_FallsBackToSystemWallpaper() {
        WithIsolatedWallpaperHistory(_ => {
            var fake = new FakeDesktopManager { DevicePathCount = 1 };
            fake.DevicePaths[0] = null!;
            var service = new RecordingMonitorService(fake);

            service.SetWallpaper(0, "img");

            CollectionAssert.AreEqual(new[] { "img" }, service.SystemWallpaperPaths);
            Assert.AreEqual(0, fake.SetWallpaperCalls.Count);
            CollectionAssert.AreEqual(new[] { "img" }, WallpaperHistory.GetHistory());
        });
    }

    [TestMethod]
    /// <summary>
    /// Stream-based wallpaper updates should use the same null-device fallback and clean up their temporary file.
    /// </summary>
    public void SetWallpaper_ByIndexStream_NullDevicePath_FallsBackAndDeletesTempFile() {
        var fake = new FakeDesktopManager { DevicePathCount = 1 };
        fake.DevicePaths[0] = null!;
        var service = new RecordingMonitorService(fake);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        service.SetWallpaper(0, stream);

        Assert.HasCount(1, service.SystemWallpaperPaths);
        Assert.IsFalse(File.Exists(service.SystemWallpaperPaths[0]));
        Assert.AreEqual(0, fake.SetWallpaperCalls.Count);
    }

    [TestMethod]
    /// <summary>
    /// URL-based wallpaper updates should continue through validation instead of silently returning for a null device ID.
    /// </summary>
    public void SetWallpaperFromUrl_ByIndex_NullDevicePath_UsesGlobalUrlPath() {
        var fake = new FakeDesktopManager { DevicePathCount = 1 };
        fake.DevicePaths[0] = null!;
        var service = new RecordingMonitorService(fake);

        Assert.ThrowsExactly<NotSupportedException>(
            () => service.SetWallpaperFromUrl(0, new Uri(Path.Combine(Path.GetTempPath(), "wallpaper.bmp")).AbsoluteUri));
    }

    [TestMethod]
    /// <summary>
    /// Test for SetWallpaper_ByIndex_MissingPathDoesNotFallBackGlobally.
    /// </summary>
    public void SetWallpaper_ByIndex_MissingPathDoesNotFallBackGlobally() {
        WithIsolatedWallpaperHistory(historyPath => {
            var fake = new FakeDesktopManager();
            var service = new MonitorService(fake);

            service.SetWallpaper(1, "img");

            Assert.AreEqual(0, fake.SetWallpaperCalls.Count);
            Assert.IsFalse(fake.EnableCalled);
            Assert.IsFalse(File.Exists(historyPath));
            Assert.AreEqual(0, WallpaperHistory.GetHistory().Count);
        });
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
        Assert.ThrowsExactly<ArgumentNullException>(() => service.SetWallpaper("id", (Stream)null!));
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
            Assert.ThrowsExactly<NotSupportedException>(() => service.SetWallpaperFromUrl("m", new Uri(temp).AbsoluteUri));
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
    /// Test for SetWallpaperSlideshowOptions_Forwards.
    /// </summary>
    public void SetWallpaperSlideshowOptions_Forwards() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);

        service.SetWallpaperSlideshowOptions(DesktopSlideshowOptions.ShuffleImages, 60000);

        Assert.AreEqual(DesktopSlideshowOptions.ShuffleImages, fake.SlideshowOptions);
        Assert.AreEqual((uint)60000, fake.SlideshowTick);
    }

    [TestMethod]
    /// <summary>
    /// Test for GetWallpaperSlideshow_ReturnsStateAndOptions.
    /// </summary>
    public void GetWallpaperSlideshow_ReturnsStateAndOptions() {
        var fake = new FakeDesktopManager {
            SlideshowOptions = DesktopSlideshowOptions.ShuffleImages,
            SlideshowTick = 300000,
            SlideshowState = DesktopSlideshowState.Enabled | DesktopSlideshowState.Slideshow
        };
        var service = new MonitorService(fake);

        var slideshow = service.GetWallpaperSlideshow();

        Assert.IsTrue(slideshow.IsEnabled);
        Assert.IsTrue(slideshow.IsRunning);
        Assert.IsTrue(slideshow.ShuffleImages);
        Assert.AreEqual((uint)300000, slideshow.SlideshowTick);
        Assert.AreEqual(0, slideshow.ImagePaths.Count);
    }

    [TestMethod]
    /// <summary>
    /// Test for StartWallpaperSlideshow_ThrowsOnNull.
    /// </summary>
    public void StartWallpaperSlideshow_ThrowsOnNull() {
        var fake = new FakeDesktopManager();
        var service = new MonitorService(fake);
        Assert.ThrowsExactly<ArgumentNullException>(() => service.StartWallpaperSlideshow(null!));
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
