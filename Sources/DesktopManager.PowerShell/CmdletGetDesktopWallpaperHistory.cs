

namespace DesktopManager.PowerShell;

/// <summary>Returns stored wallpaper history.</summary>
/// <para type="synopsis">Gets previously used wallpaper paths.</para>
/// <para type="description">Retrieves the wallpaper history saved by the module.
/// The list is returned in most‑recent‑first order.</para>
/// <example>
///   <summary>List saved wallpaper paths</summary>
///   <code>Get-DesktopWallpaperHistory</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopWallpaperHistory")]
public sealed class CmdletGetDesktopWallpaperHistory : PSCmdlet
{
    /// <summary>Begin processing the command.</summary>
    protected override void BeginProcessing()
    {
        List<string> history = WallpaperHistory.GetHistory();
        WriteObject(history, true);
    }
}
