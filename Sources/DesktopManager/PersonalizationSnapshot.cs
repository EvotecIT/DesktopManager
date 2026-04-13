using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Captures personalization-related state for later restoration.
/// </summary>
public sealed class PersonalizationSnapshot {
    /// <summary>
    /// Gets or sets the capture timestamp.
    /// </summary>
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the desktop background color as an RGB value.
    /// </summary>
    public uint? DesktopBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the wallpaper position.
    /// </summary>
    public DesktopWallpaperPosition? WallpaperPosition { get; set; }

    /// <summary>
    /// Gets or sets the monitor-specific wallpaper snapshot.
    /// </summary>
    public List<PersonalizationMonitorSnapshot> Monitors { get; set; } = new();

    /// <summary>
    /// Gets or sets the policy-based personalization snapshot.
    /// </summary>
    public PersonalizationPolicySnapshot Policy { get; set; } = new();

    /// <summary>
    /// Gets or sets the user-scoped personalization snapshot.
    /// </summary>
    public PersonalizationUserSnapshot User { get; set; } = new();
}

/// <summary>
/// Captures wallpaper state for a monitor.
/// </summary>
public sealed class PersonalizationMonitorSnapshot {
    /// <summary>
    /// Gets or sets the monitor device ID.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device name for display.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the wallpaper path.
    /// </summary>
    public string WallpaperPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the monitor was primary.
    /// </summary>
    public bool IsPrimary { get; set; }
}

/// <summary>
/// Captures policy settings that affect personalization.
/// </summary>
public sealed class PersonalizationPolicySnapshot {
    /// <summary>
    /// Gets or sets the enforced lock screen image policy value.
    /// </summary>
    public StringPolicyValue LockScreenImage { get; set; } = new();

    /// <summary>
    /// Gets or sets the policy to disable lock screen slideshows.
    /// </summary>
    public DwordPolicyValue NoLockScreenSlideshow { get; set; } = new();

    /// <summary>
    /// Gets or sets the policy that disables Windows Spotlight features.
    /// </summary>
    public DwordPolicyValue DisableWindowsSpotlightFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the policy that disables Windows Spotlight on the lock screen.
    /// </summary>
    public DwordPolicyValue DisableWindowsSpotlightOnLockScreen { get; set; } = new();
}

/// <summary>
/// Captures user-scoped personalization settings.
/// </summary>
public sealed class PersonalizationUserSnapshot {
    /// <summary>
    /// Gets or sets the system theme preference.
    /// </summary>
    public DwordSettingValue SystemUsesLightTheme { get; set; } = new();

    /// <summary>
    /// Gets or sets the apps theme preference.
    /// </summary>
    public DwordSettingValue AppsUseLightTheme { get; set; } = new();

    /// <summary>
    /// Gets or sets the transparency effects preference.
    /// </summary>
    public DwordSettingValue EnableTransparency { get; set; } = new();

    /// <summary>
    /// Gets or sets the accent color.
    /// </summary>
    public DwordSettingValue AccentColor { get; set; } = new();

    /// <summary>
    /// Gets or sets the colorization color.
    /// </summary>
    public DwordSettingValue ColorizationColor { get; set; } = new();

    /// <summary>
    /// Gets or sets the accent color menu value.
    /// </summary>
    public DwordSettingValue AccentColorMenu { get; set; } = new();

    /// <summary>
    /// Gets or sets the start color menu value.
    /// </summary>
    public DwordSettingValue StartColorMenu { get; set; } = new();

    /// <summary>
    /// Gets or sets the color prevalence toggle.
    /// </summary>
    public DwordSettingValue ColorPrevalence { get; set; } = new();

    /// <summary>
    /// Gets or sets the window colorization toggle.
    /// </summary>
    public DwordSettingValue EnableWindowColorization { get; set; } = new();

    /// <summary>
    /// Gets or sets the accent palette.
    /// </summary>
    public BinarySettingValue AccentPalette { get; set; } = new();
}

/// <summary>
/// Represents a string policy value with state tracking.
/// </summary>
public sealed class StringPolicyValue {
    /// <summary>
    /// Gets or sets a value indicating whether the policy value existed.
    /// </summary>
    public bool IsSet { get; set; }

    /// <summary>
    /// Gets or sets the stored policy value.
    /// </summary>
    public string? Value { get; set; }
}

/// <summary>
/// Represents a DWORD policy value with state tracking.
/// </summary>
public sealed class DwordPolicyValue {
    /// <summary>
    /// Gets or sets a value indicating whether the policy value existed.
    /// </summary>
    public bool IsSet { get; set; }

    /// <summary>
    /// Gets or sets the stored policy value.
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// Represents a DWORD setting value with state tracking.
/// </summary>
public sealed class DwordSettingValue {
    /// <summary>
    /// Gets or sets a value indicating whether the setting value existed.
    /// </summary>
    public bool IsSet { get; set; }

    /// <summary>
    /// Gets or sets the stored setting value.
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// Represents a binary setting value with state tracking.
/// </summary>
public sealed class BinarySettingValue {
    /// <summary>
    /// Gets or sets a value indicating whether the setting value existed.
    /// </summary>
    public bool IsSet { get; set; }

    /// <summary>
    /// Gets or sets the stored setting value.
    /// </summary>
    public byte[]? Value { get; set; }
}
