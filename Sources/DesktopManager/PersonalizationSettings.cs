namespace DesktopManager;

/// <summary>
/// Defines personalization settings to apply.
/// </summary>
public sealed class PersonalizationSettings {
    /// <summary>
    /// Gets or sets the lock screen image path.
    /// </summary>
    public string? LockScreenImagePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to disable lock screen slideshows.
    /// </summary>
    public bool? DisableLockScreenSlideshow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to disable Windows Spotlight features.
    /// </summary>
    public bool? DisableWindowsSpotlight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to disable Windows Spotlight on the lock screen.
    /// </summary>
    public bool? DisableWindowsSpotlightOnLockScreen { get; set; }

    /// <summary>
    /// Gets or sets the system theme to apply.
    /// </summary>
    public SystemTheme? SystemTheme { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable transparency effects.
    /// </summary>
    public bool? EnableTransparency { get; set; }

    /// <summary>
    /// Gets or sets the accent color (ARGB).
    /// </summary>
    public uint? AccentColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the accent color on Start and taskbar.
    /// </summary>
    public bool? UseAccentColorOnStartTaskbar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the accent color on title bars and window borders.
    /// </summary>
    public bool? UseAccentColorOnTitleBars { get; set; }

    /// <summary>
    /// Gets or sets the desktop wallpaper path.
    /// </summary>
    public string? DesktopWallpaperPath { get; set; }

    /// <summary>
    /// Gets or sets the wallpaper position.
    /// </summary>
    public DesktopWallpaperPosition? DesktopWallpaperPosition { get; set; }

    /// <summary>
    /// Gets or sets the desktop background color as RGB value.
    /// </summary>
    public uint? DesktopBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to apply wallpaper for all user profiles.
    /// </summary>
    public bool ApplyWallpaperToAllUsers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include the default profile when applying wallpaper for all users.
    /// </summary>
    public bool IncludeDefaultUserProfile { get; set; } = true;
}
