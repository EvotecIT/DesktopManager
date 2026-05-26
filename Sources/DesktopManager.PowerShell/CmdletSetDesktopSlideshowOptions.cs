using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Sets desktop wallpaper slideshow options.</summary>
/// <para type="synopsis">Sets desktop wallpaper slideshow options.</para>
/// <para type="description">Updates slideshow shuffle behavior and tick interval without replacing the slideshow images.</para>
[Cmdlet(VerbsCommon.Set, "DesktopSlideshowOptions", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopSlideshowOptions : PSCmdlet {
    /// <summary>
    /// <para type="description">Enable randomized image order.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Shuffle { get; set; }

    /// <summary>
    /// <para type="description">Disable randomized image order.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter NoShuffle { get; set; }

    /// <summary>
    /// <para type="description">Slideshow tick interval in milliseconds.</para>
    /// </summary>
    [Parameter]
    public uint SlideshowTick { get; set; }

    /// <summary>
    /// Begins processing the cmdlet.
    /// </summary>
    protected override void BeginProcessing() {
        if (Shuffle.IsPresent && NoShuffle.IsPresent) {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException("Shuffle and NoShuffle cannot be used together.", nameof(Shuffle)),
                "DesktopSlideshowShuffleConflict",
                ErrorCategory.InvalidArgument,
                Shuffle));
            return;
        }

        var automation = new DesktopAutomationService();
        var current = automation.GetDesktopSlideshow();
        var options = current.Options;
        if (Shuffle.IsPresent) {
            options |= DesktopSlideshowOptions.ShuffleImages;
        }
        if (NoShuffle.IsPresent) {
            options &= ~DesktopSlideshowOptions.ShuffleImages;
        }

        uint tick = MyInvocation.BoundParameters.ContainsKey(nameof(SlideshowTick))
            ? SlideshowTick
            : current.SlideshowTick;

        if (ShouldProcess("Desktop", $"Set slideshow options to {options} and tick to {tick}")) {
            automation.SetDesktopSlideshowOptions(options, tick);
        }
    }
}
