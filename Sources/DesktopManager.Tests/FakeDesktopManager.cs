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

    public void SetWallpaper(string monitorId, string wallpaper) => SetWallpaperCalls.Add((monitorId, wallpaper));

    public string GetWallpaper(string monitorId) {
        GetWallpaperIds.Add(monitorId);
        return "wall";
    }

    public string GetMonitorDevicePathAt(uint monitorIndex) => DevicePaths.TryGetValue(monitorIndex, out var path) ? path : string.Empty;

    public uint GetMonitorDevicePathCount() => DevicePathCount;

    public RECT GetMonitorBounds(string monitorId) => new() { Left = 0, Top = 0, Right = 10, Bottom = 10 };

    public void SetBackgroundColor(uint color) => BackgroundColor = color;

    public uint GetBackgroundColor() => BackgroundColor;

    public void SetPosition(DesktopWallpaperPosition position) => WallpaperPosition = position;

    public DesktopWallpaperPosition GetPosition() => WallpaperPosition;

    public void SetSlideshow(IntPtr items) => SetSlideshowCalls.Add(items);

    public IntPtr GetSlideshow() => IntPtr.Zero;

    public void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick) { }

    public uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick) {
        options = DesktopSlideshowDirection.Forward;
        slideshowTick = 0;
        return 0;
    }

    public void AdvanceSlideshow(string monitorId, DesktopSlideshowDirection direction) => LastAdvanceDirection = direction;

    public DesktopSlideshowDirection GetStatus() => DesktopSlideshowDirection.Forward;

    public bool Enable() { EnableCalled = true; return true; }
}
