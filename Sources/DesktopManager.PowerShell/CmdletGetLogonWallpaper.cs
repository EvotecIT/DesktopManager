using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets the current logon (lock screen) wallpaper.</summary>
/// <para type="synopsis">Retrieves the logon wallpaper path using native API when possible and falls back to registry.</para>
[Cmdlet(VerbsCommon.Get, "LogonWallpaper")]
[Alias("Get-LockScreenWallpaper")]
public sealed class CmdletGetLogonWallpaper : PSCmdlet {
    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        Monitors monitors = new Monitors();
        WriteObject(monitors.GetLogonWallpaper());
    }
}
