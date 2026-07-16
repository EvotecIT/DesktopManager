namespace DesktopManager;

/// <summary>
/// Captures one monitor inside a workstation profile.
/// </summary>
public sealed class WorkstationMonitorProfile {
    /// <summary>Gets or sets the stable monitor identity key.</summary>
    public string StableKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the captured Windows monitor device path.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the captured display source name.</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this was the primary monitor.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Gets or sets the left desktop coordinate.</summary>
    public int Left { get; set; }

    /// <summary>Gets or sets the top desktop coordinate.</summary>
    public int Top { get; set; }

    /// <summary>Gets or sets the display mode.</summary>
    public MonitorDisplayMode DisplayMode { get; set; } = new();

    /// <summary>Gets or sets brightness from 0 through 100 when DDC/CI exposes it.</summary>
    public int? Brightness { get; set; }

    /// <summary>Gets or sets the HDR state when Advanced Color exposes it.</summary>
    public bool? HdrEnabled { get; set; }

    /// <summary>Gets or sets the monitor wallpaper path.</summary>
    public string WallpaperPath { get; set; } = string.Empty;
}
