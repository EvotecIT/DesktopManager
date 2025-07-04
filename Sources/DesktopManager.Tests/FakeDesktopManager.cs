using System;
using System.Collections.Generic;

namespace DesktopManager.Tests;

internal class FakeDesktopManager : IDesktopManager {
    public List<(string id, string path)> SetWallpaperCalls = new();
    public List<string> GetWallpaperIds = new();
    public Dictionary<uint, string> DevicePaths = new();
    public uint DevicePathCount = 1;
    public uint BackgroundColor;
    public DesktopWallpaperPosition WallpaperPosition;
    public List<IntPtr> SetSlideshowCalls = new();
    public DesktopSlideshowDirection LastAdvanceDirection;
    public bool EnableCalled;

    /// <summary>
    /// Test for SetWallpaper.
    /// </summary>
    public void SetWallpaper(string monitorId, string wallpaper) => SetWallpaperCalls.Add((monitorId, wallpaper));

    /// <summary>
    /// Test for GetWallpaper.
    /// </summary>
    public string GetWallpaper(string monitorId) {
        GetWallpaperIds.Add(monitorId);
        return "wall";
    }

    /// <summary>
    /// Test for GetMonitorDevicePathAt.
    /// </summary>
    public string GetMonitorDevicePathAt(uint monitorIndex) => DevicePaths.TryGetValue(monitorIndex, out var path) ? path : string.Empty;

    /// <summary>
    /// Test for GetMonitorDevicePathCount.
    /// </summary>
    public uint GetMonitorDevicePathCount() => DevicePathCount;

    /// <summary>
    /// Test for GetMonitorBounds.
    /// </summary>
    public RECT GetMonitorBounds(string monitorId) => new() { Left = 0, Top = 0, Right = 10, Bottom = 10 };

    /// <summary>
    /// Test for SetBackgroundColor.
    /// </summary>
    public void SetBackgroundColor(uint color) => BackgroundColor = color;

    /// <summary>
    /// Test for GetBackgroundColor.
    /// </summary>
    public uint GetBackgroundColor() => BackgroundColor;

    /// <summary>
    /// Test for SetPosition.
    /// </summary>
    public void SetPosition(DesktopWallpaperPosition position) => WallpaperPosition = position;

    /// <summary>
    /// Test for GetPosition.
    /// </summary>
    public DesktopWallpaperPosition GetPosition() => WallpaperPosition;

    /// <summary>
    /// Test for SetSlideshow.
    /// </summary>
    public void SetSlideshow(IntPtr items) => SetSlideshowCalls.Add(items);

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
    public uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick) {
        options = DesktopSlideshowDirection.Forward;
        slideshowTick = 0;
        return 0;
    }

    /// <summary>
    /// Test for AdvanceSlideshow.
    /// </summary>
    public void AdvanceSlideshow(string monitorId, DesktopSlideshowDirection direction) => LastAdvanceDirection = direction;

    /// <summary>
    /// Test for GetStatus.
    /// </summary>
    public DesktopSlideshowDirection GetStatus() => DesktopSlideshowDirection.Forward;

    /// <summary>
    /// Test for Enable.
    /// </summary>
    public bool Enable() { EnableCalled = true; return true; }
}
