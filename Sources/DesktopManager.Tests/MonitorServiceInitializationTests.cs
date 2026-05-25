using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorServiceInitializationTests.
/// </summary>
public class MonitorServiceInitializationTests {
    private class FailingEnableDesktopManager : IDesktopManager {
        /// <summary>
        /// Test for SetWallpaper.
        /// </summary>
        public void SetWallpaper(string monitorId, string wallpaper) { }
        /// <summary>
        /// Test for GetWallpaper.
        /// </summary>
        public string GetWallpaper(string monitorId) => string.Empty;
        /// <summary>
        /// Test for GetMonitorDevicePathAt.
        /// </summary>
        public string GetMonitorDevicePathAt(uint monitorIndex) => string.Empty;
        /// <summary>
        /// Test for GetMonitorDevicePathCount.
        /// </summary>
        public uint GetMonitorDevicePathCount() => 0;
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
        public uint GetSlideshow(out IntPtr items) { items = IntPtr.Zero; return 0; }
        /// <summary>
        /// Test for SetSlideshowOptions.
        /// </summary>
        public void SetSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick) { }
        /// <summary>
        /// Test for GetSlideshowOptions.
        /// </summary>
        public uint GetSlideshowOptions(out DesktopSlideshowOptions options, out uint slideshowTick) { options = DesktopSlideshowOptions.None; slideshowTick = 0; return 0; }
        /// <summary>
        /// Test for AdvanceSlideshow.
        /// </summary>
        public void AdvanceSlideshow(string? monitorId, DesktopSlideshowDirection direction) { }
        /// <summary>
        /// Test for GetStatus.
        /// </summary>
        public uint GetStatus(out DesktopSlideshowState state) { state = DesktopSlideshowState.Enabled; return 0; }
        /// <summary>
        /// Test for Enable.
        /// </summary>
        public void Enable(bool enable) => throw new COMException();
    }

    [TestMethod]
    /// <summary>
    /// Test for Constructor_DoesNotInvokeEnable.
    /// </summary>
    public void Constructor_DoesNotInvokeEnable() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        using var sw = new System.IO.StringWriter();
        var original = Console.Out;
        Console.SetOut(sw);
        new MonitorService(new FailingEnableDesktopManager());
        Console.SetOut(original);

        Assert.IsFalse(sw.ToString().Contains("DesktopManager initialization failed", StringComparison.Ordinal));
    }
}

