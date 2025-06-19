using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Stops the desktop wallpaper slideshow.</summary>
/// <para type="synopsis">Stops the desktop wallpaper slideshow.</para>
[Cmdlet(VerbsLifecycle.Stop, "DesktopSlideshow", SupportsShouldProcess = true)]
public sealed class CmdletStopDesktopSlideshow : PSCmdlet {
    protected override void BeginProcessing() {
        if (ShouldProcess("Desktop", "Stop slideshow")) {
            Monitors monitors = new();
            monitors.StopWallpaperSlideshow();
        }
    }
}
