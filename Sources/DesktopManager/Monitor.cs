namespace DesktopManager;

public class Monitor {
    private readonly MonitorService _monitorService;
    public int Index { get; internal set; }
    public bool IsConnected => (StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0;
    public bool IsPrimary => (StateFlags & DisplayDeviceStateFlags.PrimaryDevice) != 0;
    public string DeviceString { get; internal set; }
    public int PositionLeft => Position.Left;
    public int PositionTop => Position.Top;
    public int PositionRight => Position.Right;
    public int PositionBottom => Position.Bottom;
    public string DeviceId { get; internal set; }
    public string Wallpaper { get; internal set; }
    public DesktopWallpaperPosition WallpaperPosition { get; internal set; }
    public MonitorPosition Position => GetMonitorPosition();
    public string DeviceName { get; internal set; }
    public DisplayDeviceStateFlags StateFlags { get; internal set; }
    public string DeviceKey { get; internal set; }


    internal RECT Rect { get; set; }

    public Monitor(MonitorService monitorService) {
        _monitorService = monitorService;
    }

    public void SetWallpaper(string wallpaperPath) {
        _monitorService.SetWallpaper(DeviceId, wallpaperPath);
    }

    public string GetWallpaper() {
        return _monitorService.GetWallpaper(DeviceId);
    }

    public void SetMonitorPosition(MonitorPosition position) {
        _monitorService.SetMonitorPosition(DeviceId, position);
    }

    public void SetMonitorPosition(int left, int top, int right, int bottom) {
        _monitorService.SetMonitorPosition(DeviceId, left, top, right, bottom);
    }

    public MonitorPosition GetMonitorPosition() {
        return _monitorService.GetMonitorPosition(DeviceId);
    }

    internal RECT GetMonitorBounds() {
        return _monitorService.GetMonitorBounds(DeviceId);
    }
}
