using System.IO;
using System.Management.Automation;
using DesktopManager;

namespace DesktopManager.PowerShell;

/// <summary>Sets the logon (lock screen) wallpaper.</summary>
/// <para type="synopsis">Sets the logon wallpaper using native API when possible and falls back to registry.</para>
[Cmdlet(VerbsCommon.Set, "LogonWallpaper", SupportsShouldProcess = true)]
[Alias("Set-LockScreenWallpaper")]
public sealed class CmdletSetLogonWallpaper : PSCmdlet {
    /// <summary>
    /// <para type="description">Path to the image file.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string ImagePath;

    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        if (!File.Exists(ImagePath)) {
            ThrowTerminatingError(new ErrorRecord(new FileNotFoundException($"File '{ImagePath}' not found."), "FileNotFound", ErrorCategory.InvalidArgument, ImagePath));
        }

        if (ShouldProcess("System", $"Set logon wallpaper to '{ImagePath}'")) {
            PrivilegeChecker.EnsureElevated();
            Monitors monitors = new Monitors();
            monitors.SetLogonWallpaper(ImagePath);
        }
    }
}
