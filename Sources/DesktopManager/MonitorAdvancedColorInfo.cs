namespace DesktopManager;

/// <summary>
/// Describes the Advanced Color and HDR state reported for a monitor.
/// </summary>
public sealed class MonitorAdvancedColorInfo {
    /// <summary>
    /// Gets or sets the DesktopManager monitor index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the monitor device name, such as <c>\\.\DISPLAY1</c>.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the monitor device identifier.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the monitor is primary.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any Advanced Color mode is supported.
    /// </summary>
    public bool AdvancedColorSupported { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any Advanced Color mode is currently active or enabled.
    /// </summary>
    public bool AdvancedColorEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HDR is supported.
    /// </summary>
    public bool HdrSupported { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HDR is enabled by the user.
    /// </summary>
    public bool HdrEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether wide color gamut is supported.
    /// </summary>
    public bool WideColorSupported { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether wide color gamut is enabled by the user.
    /// </summary>
    public bool WideColorEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether wide color gamut is enforced by the display stack.
    /// </summary>
    public bool WideColorEnforced { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Advanced Color is limited or disabled by policy.
    /// </summary>
    public bool AdvancedColorLimitedByPolicy { get; set; }

    /// <summary>
    /// Gets or sets the active color mode reported by newer Windows builds.
    /// </summary>
    public string ActiveColorMode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the active color encoding.
    /// </summary>
    public string ColorEncoding { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of bits per color channel.
    /// </summary>
    public uint BitsPerColorChannel { get; set; }

    /// <summary>
    /// Gets or sets the raw SDR white level when the display stack reports it.
    /// </summary>
    public uint? SdrWhiteLevel { get; set; }

    /// <summary>
    /// Gets or sets the SDR white level converted to nits.
    /// </summary>
    public double? SdrWhiteLevelNits { get; set; }
}
