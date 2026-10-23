namespace DesktopManager;

/// <summary>Describes the current desktop backend without changing desktop settings.</summary>
public sealed class DesktopCapabilities {
    internal DesktopCapabilities(string backendId, bool available, bool displays, bool wallpaper, bool perDisplay, bool position, string message,
        bool wallpaperTargets = false, DesktopWallpaperPosition[]? supportedPositions = null) {
        BackendId = backendId;
        IsAvailable = available;
        CanEnumerateDisplays = available && displays;
        CanSetWallpaper = available && wallpaper;
        CanReadWallpaper = available && wallpaper;
        SupportsPerDisplayWallpaper = available && perDisplay;
        SupportsWallpaperPosition = available && position;
        CanEnumerateWallpaperTargets = available && (wallpaperTargets || displays);
        SupportedWallpaperPositions = Array.AsReadOnly(available && position
            ? (DesktopWallpaperPosition[])(supportedPositions ?? Enum.GetValues(typeof(DesktopWallpaperPosition))).Clone()
            : Array.Empty<DesktopWallpaperPosition>());
        Message = message;
    }

    /// <summary>Gets the selected backend: windows, macos, gnome, kde, or unsupported.</summary>
    public string BackendId { get; }
    /// <summary>Gets whether the backend found its required user session and platform facilities.</summary>
    public bool IsAvailable { get; }
    /// <summary>Gets whether this backend can enumerate display geometry.</summary>
    public bool CanEnumerateDisplays { get; }
    /// <summary>Gets whether wallpaper targets can be discovered independently of physical display geometry.</summary>
    public bool CanEnumerateWallpaperTargets { get; }
    /// <summary>Gets the fit modes accepted by this backend in the current session. An unavailable backend returns an empty list.</summary>
    public IReadOnlyList<DesktopWallpaperPosition> SupportedWallpaperPositions { get; }
    /// <summary>Gets whether this backend can request a desktop wallpaper change.</summary>
    public bool CanSetWallpaper { get; }
    /// <summary>Gets whether this backend can read the current wallpaper path.</summary>
    public bool CanReadWallpaper { get; }
    /// <summary>Gets whether individual displays can receive different images.</summary>
    public bool SupportsPerDisplayWallpaper { get; }
    /// <summary>Gets whether supported image fit modes can be selected.</summary>
    public bool SupportsWallpaperPosition { get; }
    /// <summary>Gets why a backend is unavailable or the limits of its desktop support.</summary>
    public string Message { get; }
    /// <summary>Gets whether this portable surface changes lock screen images. This capability is not implemented.</summary>
    public bool SupportsLockScreen => false;
    /// <summary>Gets whether this portable surface changes other users' settings. This capability is not implemented.</summary>
    public bool SupportsAllUsers => false;
    /// <summary>Gets whether this portable surface manages wallpaper slideshows. This capability is not implemented.</summary>
    public bool SupportsSlideshow => false;
}

/// <summary>Detached display identity and geometry for image generation.</summary>
public sealed class DesktopDisplayInfo {
    /// <summary>Gets the backend's display identifier, valid for its current topology.</summary>
    public string Id { get; internal set; } = string.Empty;
    /// <summary>Gets the zero-based index in the current enumeration.</summary>
    public int Index { get; internal set; }
    /// <summary>Gets the platform display name when available.</summary>
    public string DeviceName { get; internal set; } = string.Empty;
    /// <summary>Gets whether this is the primary display.</summary>
    public bool IsPrimary { get; internal set; }
    /// <summary>Gets the horizontal origin in the platform's desktop coordinate system.</summary>
    public int Left { get; internal set; }
    /// <summary>Gets the vertical origin in the platform's desktop coordinate system.</summary>
    public int Top { get; internal set; }
    /// <summary>Gets the image width in physical pixels.</summary>
    public int Width { get; internal set; }
    /// <summary>Gets the image height in physical pixels.</summary>
    public int Height { get; internal set; }
    /// <summary>Gets the display scale, or null when it is unavailable.</summary>
    public double? Scale { get; internal set; }
}

/// <summary>Describes the current process's user and graphical-session context.</summary>
public sealed class DesktopUserSession {
    /// <summary>Gets the process account on Unix or the selected Windows session account. This alone does not prove a graphical login.</summary>
    public string AccountName { get; internal set; } = string.Empty;
    /// <summary>Gets the unqualified account name; inspect HasGraphicalSession before presenting it as an active desktop user.</summary>
    public string UserName { get; internal set; } = string.Empty;
    /// <summary>Gets the desktop environment name when known.</summary>
    public string DesktopEnvironment { get; internal set; } = string.Empty;
    /// <summary>Gets the session type, such as windows, aqua, x11, or wayland.</summary>
    public string SessionType { get; internal set; } = string.Empty;
    /// <summary>Gets whether the current context has a usable graphical desktop.</summary>
    public bool HasGraphicalSession { get; internal set; }
}
