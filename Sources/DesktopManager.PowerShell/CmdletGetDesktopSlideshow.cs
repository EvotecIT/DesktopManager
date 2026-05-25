using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets the desktop wallpaper slideshow configuration and state.</summary>
/// <para type="synopsis">Gets the desktop wallpaper slideshow configuration and state.</para>
/// <para type="description">Returns configured slideshow images, runtime state, options, and tick interval.</para>
[Cmdlet(VerbsCommon.Get, "DesktopSlideshow")]
[OutputType(typeof(DesktopWallpaperSlideshow))]
public sealed class CmdletGetDesktopSlideshow : PSCmdlet {
    /// <summary>Begin processing.</summary>
    protected override void BeginProcessing() {
        WriteObject(new DesktopAutomationService().GetDesktopSlideshow());
    }
}
