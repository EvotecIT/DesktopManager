using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests ensuring registry handles are released in MonitorService fallback methods.
/// </summary>
public class RegistryDisposalTests {
    private class ThrowingDesktopManager : IDesktopManager {
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
    /// <summary>
    /// Ensures GetWallpaperPosition does not leak registry handles when fallback logic executes.
    /// </summary>
    public void GetWallpaperPosition_NoHandleLeak() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new ThrowingDesktopManager());
        int before = Process.GetCurrentProcess().HandleCount;
        _ = service.GetWallpaperPosition();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        int after = Process.GetCurrentProcess().HandleCount;
        Assert.IsTrue(after <= before);
    }

    [TestMethod]
    /// <summary>
    /// Ensures GetBackgroundColor does not leak registry handles when fallback logic executes.
    /// </summary>
    public void GetBackgroundColor_NoHandleLeak() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new ThrowingDesktopManager());
        int before = Process.GetCurrentProcess().HandleCount;
        _ = service.GetBackgroundColor();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        int after = Process.GetCurrentProcess().HandleCount;
        Assert.IsTrue(after <= before);
    }
}
