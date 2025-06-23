using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Starts a desktop wallpaper slideshow.</summary>
/// <para type="synopsis">Starts a desktop wallpaper slideshow.</para>
/// <para type="description">Begins a slideshow using the provided image paths for all monitors.</para>
/// <example>
/// <code>Start-DesktopSlideshow -ImagePath 'C:\Wallpapers\img1.jpg','C:\Wallpapers\img2.jpg'</code>
/// </example>
[Cmdlet(VerbsLifecycle.Start, "DesktopSlideshow", SupportsShouldProcess = true)]
public sealed class CmdletStartDesktopSlideshow : PSCmdlet {
    /// <summary>
    /// <para type="description">Paths to images used for the slideshow.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string[] ImagePath;

    /// <summary>
    /// Begins processing the cmdlet.
    /// </summary>
    protected override void BeginProcessing() {
        if (!ShouldProcess("Desktop", $"Start wallpaper slideshow with {ImagePath.Length} image(s)")) {
            return;
        }

        Monitors monitors = new();
        monitors.StartWallpaperSlideshow(ImagePath);
    }
}
