using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Steps the desktop wallpaper slideshow.</summary>
/// <para type="synopsis">Steps the desktop wallpaper slideshow.</para>
/// <para type="description">Moves the wallpaper slideshow forward or backward on all monitors.</para>
[Cmdlet(VerbsCommon.Step, "DesktopSlideshow", SupportsShouldProcess = false)]
public sealed class CmdletStepDesktopSlideshow : PSCmdlet {
    /// <summary>
    /// <para type="description">Direction to advance the slideshow.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public DesktopSlideshowDirection Direction { get; set; }

    /// <example>
    ///   <summary>Step to the next slide</summary>
    ///   <code>Step-DesktopSlideshow -Direction Forward</code>
    /// </example>

    /// <summary>Begin processing.</summary>
    protected override void BeginProcessing() {
        new DesktopAutomationService().AdvanceDesktopSlideshow(Direction);
    }
}
