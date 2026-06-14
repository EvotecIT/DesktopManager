using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Tests;

internal sealed class StubDesktopManager : IDesktopManager {
    private readonly Dictionary<string, RECT> _bounds;
    private readonly string[] _deviceIds;

    public StubDesktopManager() {
        _bounds = new Dictionary<string, RECT>(StringComparer.OrdinalIgnoreCase);
        _deviceIds = Array.Empty<string>();
    }

    public StubDesktopManager(IReadOnlyDictionary<string, RECT> bounds) {
        _bounds = new Dictionary<string, RECT>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, RECT> item in bounds) {
            _bounds.Add(item.Key, item.Value);
        }

        _deviceIds = _bounds.Keys.ToArray();
    }

    public void SetWallpaper(string monitorId, string wallpaper) {
        throw new NotSupportedException();
    }

    public string GetWallpaper(string monitorId) {
        return string.Empty;
    }

    public string GetMonitorDevicePathAt(uint monitorIndex) {
        return _deviceIds[monitorIndex];
    }

    public uint GetMonitorDevicePathCount() {
        return (uint)_deviceIds.Length;
    }

    public RECT GetMonitorBounds(string monitorId) {
        if (_bounds.TryGetValue(monitorId, out RECT bounds)) {
            return bounds;
        }

        throw new NotSupportedException();
    }

    public void SetBackgroundColor(uint color) {
        throw new NotSupportedException();
    }

    public uint GetBackgroundColor() {
        throw new NotSupportedException();
    }

    public void SetPosition(DesktopWallpaperPosition position) {
        throw new NotSupportedException();
    }

    public DesktopWallpaperPosition GetPosition() {
        return DesktopWallpaperPosition.Fill;
    }

    public void SetSlideshow(IntPtr items) {
        throw new NotSupportedException();
    }

    public uint GetSlideshow(out IntPtr items) {
        throw new NotSupportedException();
    }

    public void SetSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick) {
        throw new NotSupportedException();
    }

    public uint GetSlideshowOptions(out DesktopSlideshowOptions options, out uint slideshowTick) {
        throw new NotSupportedException();
    }

    public void AdvanceSlideshow(string? monitorId, DesktopSlideshowDirection direction) {
        throw new NotSupportedException();
    }

    public uint GetStatus(out DesktopSlideshowState state) {
        throw new NotSupportedException();
    }

    public void Enable(bool enable) {
        throw new NotSupportedException();
    }
}
