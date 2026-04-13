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
