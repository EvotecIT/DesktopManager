

namespace DesktopManager.PowerShell;

/// <summary>Returns stored wallpaper history.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopWallpaperHistory")]
public sealed class CmdletGetDesktopWallpaperHistory : PSCmdlet
{
    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing()
    {
        List<string> history = WallpaperHistory.GetHistory();
        WriteObject(history, true);
    }
}
