using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Gets the current logon (lock screen) wallpaper.</summary>
/// <para type="synopsis">Retrieves the logon wallpaper path using native API when possible and falls back to registry.</para>
[Cmdlet(VerbsCommon.Get, "LogonWallpaper")]
[Alias("Get-LockScreenWallpaper")]
[SupportedOSPlatform("windows10.0.10240.0")]
public sealed class CmdletGetLogonWallpaper : PSCmdlet {
    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        WriteObject(new DesktopAutomationService().GetLogonWallpaper());
    }
}
