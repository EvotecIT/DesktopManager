namespace DesktopManager;

public class Monitor {
    private readonly MonitorService _monitorService;
    public int Index { get; internal set; }
    public string DeviceId { get; internal set; }
    public string Wallpaper { get; internal set; }
    public DesktopWallpaperPosition WallpaperPosition { get; internal set; }
    public MonitorPosition Position { get; internal set; }
    public RECT Rect { get; internal set; }

    public Monitor(MonitorService monitorService) {
        _monitorService = monitorService;
    }

    public void SetWallpaper(string wallpaperPath) {
        _monitorService.SetWallpaper(DeviceId, wallpaperPath);
    }

    public string GetWallpaper() {
        return _monitorService.GetWallpaper(DeviceId);
    }

    public void SetPosition(MonitorPosition position) {
        _monitorService.SetMonitorPosition(DeviceId, position);
    }

    public void SetPosition(int left, int top, int right, int bottom) {
        _monitorService.SetMonitorPosition(DeviceId, left, top, right, bottom);
    }

    public RECT GetBounds() {
        return _monitorService.GetMonitorBounds(DeviceId);
    }
}
