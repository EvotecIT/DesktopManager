using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopManager;

public class Monitor {
    public int Index { get; internal set; }
    public string DeviceId { get; internal set; }
    public string Wallpaper { get; internal set; }
    public DesktopWallpaperPosition WallpaperPosition { get; internal set; }
    public Rect Rect { get; internal set; }
}
