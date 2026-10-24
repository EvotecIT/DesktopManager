using System.Linq;

namespace DesktopManager;

internal sealed partial class LinuxDesktopBackend {
    private DesktopWallpaperBackground? ReadGnomeBackground(out double? opacity) {
        opacity = 1;
        var keys = RunGSettings("list-keys", BackgroundSchema).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // GNOME Shell does not consume the retained picture-opacity schema key. Its
        // existence is not evidence of effective opacity. GNOME Shell's steady-state
        // image actor is opaque; native compositor probes confirm unchanged pixels at
        // retained values 0, 50 and 100. Disabled images are handled separately.
        if (!keys.Contains("primary-color") || !keys.Contains("color-shading-type")) { return null; }
        var shade = DecodeGVariantString(RunGSettings("get", BackgroundSchema, "color-shading-type"));
        var shading = shade == "solid" ? DesktopBackgroundShading.Solid
            : shade == "horizontal" ? DesktopBackgroundShading.Horizontal
            : shade == "vertical" ? DesktopBackgroundShading.Vertical
            : throw new FormatException("GNOME returned an unknown background shading type.");
        if (shading != DesktopBackgroundShading.Solid && !keys.Contains("secondary-color")) { return null; }
        return new DesktopWallpaperBackground(DecodeGVariantString(RunGSettings("get", BackgroundSchema, "primary-color")), shading,
            shading == DesktopBackgroundShading.Solid ? null : DecodeGVariantString(RunGSettings("get", BackgroundSchema, "secondary-color")));
    }
}
