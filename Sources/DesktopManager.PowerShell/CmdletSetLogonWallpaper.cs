using System.IO;
using System.Management.Automation;
using System.Runtime.Versioning;
using DesktopManager;

namespace DesktopManager.PowerShell;

/// <summary>Sets the logon (lock screen) wallpaper.</summary>
/// <para type="synopsis">Sets the logon wallpaper using native API when possible and falls back to registry.</para>
[Cmdlet(VerbsCommon.Set, "LogonWallpaper", SupportsShouldProcess = true)]
[Alias("Set-LockScreenWallpaper")]
[SupportedOSPlatform("windows10.0.10240.0")]
public sealed class CmdletSetLogonWallpaper : PSCmdlet {
    /// <summary>
    /// <para type="description">Path to the image file.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string ImagePath { get; set; }

    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        if (!File.Exists(ImagePath)) {
            ThrowTerminatingError(new ErrorRecord(new FileNotFoundException($"File '{ImagePath}' not found."), "FileNotFound", ErrorCategory.InvalidArgument, ImagePath));
        }

        if (ShouldProcess("System", $"Set logon wallpaper to '{ImagePath}'")) {
            new DesktopAutomationService().SetLogonWallpaper(ImagePath);
        }
    }
}
