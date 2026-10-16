namespace DesktopManager;

/// <summary>Detached, read-only monitor geometry and effective display scaling captured at one point in time.</summary>
public sealed class MonitorSnapshot {
    internal MonitorSnapshot(int index, string deviceId, string deviceName, bool isPrimary, RECT bounds, uint? dpiX, uint? dpiY) {
        Index = index;
        DeviceId = deviceId;
        DeviceName = deviceName;
        IsPrimary = isPrimary;
        Bounds = bounds;
        DpiX = dpiX;
        DpiY = dpiY;
    }

    /// <summary>
    /// Gets the existing zero-based wallpaper monitor index when the wallpaper COM service is available.
    /// Otherwise, gets the index in attached display enumeration order. It remains stable for an unchanged
    /// topology and enumeration source, but is not a permanent hardware identifier across topology or session changes.
    /// </summary>
    public int Index { get; }

    /// <summary>Gets the wallpaper COM device path, or an empty string for missing paths and native geometry fallback.</summary>
    public string DeviceId { get; }

    /// <summary>Gets the display source name, such as \\.\DISPLAY1, or an empty string when unavailable.</summary>
    public string DeviceName { get; }

    /// <summary>Gets whether this is the primary display.</summary>
    public bool IsPrimary { get; }

    /// <summary>Gets a copy of the physical pixel bounds in virtual desktop coordinates; negative origins are valid.</summary>
    public RECT Bounds { get; }

    /// <summary>Gets the left edge in physical pixels.</summary>
    public int Left => Bounds.Left;

    /// <summary>Gets the top edge in physical pixels.</summary>
    public int Top => Bounds.Top;

    /// <summary>Gets the right edge in physical pixels.</summary>
    public int Right => Bounds.Right;

    /// <summary>Gets the bottom edge in physical pixels.</summary>
    public int Bottom => Bounds.Bottom;

    /// <summary>Gets the width in physical pixels, or zero when bounds are unavailable.</summary>
    public int Width => Math.Max(0, Right - Left);

    /// <summary>Gets the height in physical pixels, or zero when bounds are unavailable.</summary>
    public int Height => Math.Max(0, Bottom - Top);

    /// <summary>Gets the effective horizontal DPI, including Windows display scaling, or null when unavailable.</summary>
    public uint? DpiX { get; }

    /// <summary>Gets the effective vertical DPI, including Windows display scaling, or null when unavailable.</summary>
    public uint? DpiY { get; }

    /// <summary>Gets the effective display scale (DpiX / 96), such as 1.5 for 150 percent, or null when unavailable.</summary>
    public double? Scale => DpiX.HasValue ? DpiX.Value / 96d : (double?)null;
}
