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
    public string[] ImagePath { get; set; }

    /// <summary>
    /// <para type="description">Enables randomized image order for the slideshow.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Shuffle { get; set; }

    /// <summary>
    /// <para type="description">Slideshow tick interval in milliseconds.</para>
    /// </summary>
    [Parameter]
    public uint SlideshowTick { get; set; }

    /// <summary>
    /// Begins processing the cmdlet.
    /// </summary>
    protected override void BeginProcessing() {
        if (!ShouldProcess("Desktop", $"Start wallpaper slideshow with {ImagePath.Length} image(s)")) {
            return;
        }

        DesktopSlideshowOptions? options = Shuffle.IsPresent ? DesktopSlideshowOptions.ShuffleImages : null;
        uint? slideshowTick = MyInvocation.BoundParameters.ContainsKey(nameof(SlideshowTick)) ? SlideshowTick : null;
        new DesktopAutomationService().StartDesktopSlideshow(ImagePath, options, slideshowTick);
    }
}
