using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Advances the desktop wallpaper slideshow.</summary>
/// <para type="synopsis">Advances the desktop wallpaper slideshow.</para>
/// <para type="description">Moves the wallpaper slideshow forward or backward on all monitors.</para>
[Cmdlet("Advance", "DesktopSlideshow", SupportsShouldProcess = false)]
public sealed class CmdletAdvanceDesktopSlideshow : PSCmdlet {
    /// <summary>
    /// <para type="description">Direction to advance the slideshow.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public DesktopSlideshowDirection Direction;

    /// <example>
    ///   <summary>Advance to the next slide</summary>
    ///   <code>Advance-DesktopSlideshow -Direction Forward</code>
    /// </example>

    /// <summary>Begin processing.</summary>
    protected override void BeginProcessing() {
        Monitors monitors = new();
        monitors.AdvanceWallpaperSlide(Direction);
    }
}
