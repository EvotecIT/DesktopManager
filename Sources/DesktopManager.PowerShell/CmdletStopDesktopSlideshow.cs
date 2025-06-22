using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Stops the desktop wallpaper slideshow.</summary>
/// <para type="synopsis">Stops the desktop wallpaper slideshow.</para>
/// <para type="description">Ends any running wallpaper slideshow on all monitors.</para>
/// <example>
///   <summary>Stop slideshow</summary>
///   <code>Stop-DesktopSlideshow</code>
/// </example>
[Cmdlet(VerbsLifecycle.Stop, "DesktopSlideshow", SupportsShouldProcess = true)]
public sealed class CmdletStopDesktopSlideshow : PSCmdlet {
    /// <summary>
    /// Begins processing the cmdlet.
    /// </summary>
    protected override void BeginProcessing() {
        if (ShouldProcess("Desktop", "Stop slideshow")) {
            Monitors monitors = new();
            monitors.StopWallpaperSlideshow();
        }
    }
}
