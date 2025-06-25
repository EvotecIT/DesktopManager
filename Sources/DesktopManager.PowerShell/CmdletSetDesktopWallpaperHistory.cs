

namespace DesktopManager.PowerShell;

/// <summary>Updates wallpaper history file.</summary>
[Cmdlet(VerbsCommon.Set, "DesktopWallpaperHistory", SupportsShouldProcess = true, DefaultParameterSetName = "Paths")]
public sealed class CmdletSetDesktopWallpaperHistory : PSCmdlet {
    /// <summary>
    /// <para type="description">Array of wallpaper file paths to store in the history file.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Paths")]
    public string[] WallpaperPath { get; set; }

    /// <summary>
    /// <para type="description">Clear existing wallpaper history entries.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Clear")]
    public SwitchParameter Clear { get; set; }

    /// <summary>
    /// Begin processing the command by writing or clearing wallpaper history entries.
    /// </summary>
    protected override void BeginProcessing() {
        if (Clear.IsPresent) {
            if (ShouldProcess("Wallpaper history", "Clear")) {
                WallpaperHistory.SetHistory(Array.Empty<string>());
            }
        } else {
            if (ShouldProcess("Wallpaper history", "Set entries")) {
                WallpaperHistory.SetHistory(WallpaperPath);
            }
        }
    }
}
