using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Starts a desktop wallpaper slideshow.</summary>
/// <para type="synopsis">Starts a desktop wallpaper slideshow.</para>
/// <example>
/// <code>Start-DesktopSlideshow -ImagePath 'C:\Wallpapers\img1.jpg','C:\Wallpapers\img2.jpg'</code>
/// </example>
[Cmdlet(VerbsLifecycle.Start, "DesktopSlideshow", SupportsShouldProcess = true)]
public sealed class CmdletStartDesktopSlideshow : PSCmdlet {
    /// <summary>Paths to images used for the slideshow.</summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string[] ImagePath;

    protected override void BeginProcessing() {
        if (!ShouldProcess("Desktop", "Start slideshow")) {
            return;
        }

        Monitors monitors = new();
        monitors.StartWallpaperSlideshow(ImagePath);
    }
}
