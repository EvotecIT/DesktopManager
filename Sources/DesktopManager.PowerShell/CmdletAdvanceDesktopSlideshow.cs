using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Advances the desktop wallpaper slideshow.</summary>
/// <para type="synopsis">Advances the desktop wallpaper slideshow.</para>
[Cmdlet("Advance", "DesktopSlideshow", SupportsShouldProcess = false)]
public sealed class CmdletAdvanceDesktopSlideshow : PSCmdlet {
    [Parameter(Mandatory = true, Position = 0)]
    public DesktopSlideshowDirection Direction;

    protected override void BeginProcessing() {
        Monitors monitors = new();
        monitors.AdvanceWallpaperSlide(Direction);
    }
}
