

namespace DesktopManager.PowerShell;

/// <summary>Updates wallpaper history file.</summary>
[Cmdlet(VerbsCommon.Set, "DesktopWallpaperHistory", SupportsShouldProcess = true, DefaultParameterSetName = "Paths")]
public sealed class CmdletSetDesktopWallpaperHistory : PSCmdlet
{
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Paths")]
    public string[] WallpaperPath { get; set; }

    [Parameter(Mandatory = true, ParameterSetName = "Clear")]
    public SwitchParameter Clear { get; set; }

    protected override void BeginProcessing()
    {
        if (Clear.IsPresent)
        {
            if (ShouldProcess("Wallpaper history", "Clear"))
            {
                WallpaperHistory.SetHistory(Array.Empty<string>());
            }
        }
        else
        {
            if (ShouldProcess("Wallpaper history", "Set entries"))
            {
                WallpaperHistory.SetHistory(WallpaperPath);
            }
        }
    }
}
