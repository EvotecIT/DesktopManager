using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Restores desktop window layout from a file.</summary>
/// <para type="synopsis">Restores window positions saved earlier.</para>
/// <example>
///   <para>Restore layout from a file</para>
///   <code>Restore-DesktopWindowLayout -Path C:\layout.json</code>
/// </example>
[Cmdlet(VerbsData.Restore, "DesktopWindowLayout", SupportsShouldProcess = true)]
public sealed class CmdletRestoreDesktopWindowLayout : PSCmdlet {
    /// <summary>
    /// <para type="description">Path to layout file.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; }

    /// <summary>Begin processing.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess(Path, "Restore desktop window layout")) {
            var manager = new WindowManager();
            manager.LoadLayout(Path);
        }
    }
}
