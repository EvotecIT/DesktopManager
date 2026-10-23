namespace DesktopManager;

/// <summary>An addressable current-session wallpaper target, which need not expose physical display geometry.</summary>
public sealed class DesktopWallpaperTarget {
    /// <summary>Gets the identifier accepted by wallpaper reads and requests, or null for a desktop-wide target.</summary>
    public string? DisplayId { get; internal set; }
    /// <summary>Gets the zero-based target index in the current enumeration.</summary>
    public int Index { get; internal set; }
    /// <summary>Gets a descriptive target name.</summary>
    public string Name { get; internal set; } = string.Empty;
    /// <summary>Gets the qualified physical geometry when available; null never implies a default pixel size.</summary>
    public DesktopDisplayInfo? Display { get; internal set; }
    /// <summary>Gets the current native scope, such as a KDE activity, when available. Identifiers must be rediscovered after topology or scope changes.</summary>
    public string? ScopeId { get; internal set; }
}
