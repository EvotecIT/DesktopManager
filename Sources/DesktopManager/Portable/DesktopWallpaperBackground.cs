namespace DesktopManager;

/// <summary>Describes the color behind a desktop image, including desktops with no image.</summary>
public sealed class DesktopWallpaperBackground {
    internal DesktopWallpaperBackground(string primaryColor, DesktopBackgroundShading shading, string? secondaryColor = null) {
        PrimaryColor = primaryColor;
        Shading = shading;
        SecondaryColor = secondaryColor;
    }

    /// <summary>Gets the solid, left or top color. Windows and macOS use #RRGGBB; GNOME returns its native CSS color string. Consumers must report unsupported color syntax instead of guessing.</summary>
    public string PrimaryColor { get; }
    /// <summary>Gets the right or bottom gradient color, or null for a solid background.</summary>
    public string? SecondaryColor { get; }
    /// <summary>Gets the native color shading direction.</summary>
    public DesktopBackgroundShading Shading { get; }
}

/// <summary>Specifies how native desktop background colors are drawn.</summary>
public enum DesktopBackgroundShading {
    /// <summary>A single background color.</summary>
    Solid,
    /// <summary>A gradient from the primary color on the left to the secondary color on the right.</summary>
    Horizontal,
    /// <summary>A gradient from the primary color at the top to the secondary color at the bottom.</summary>
    Vertical
}
