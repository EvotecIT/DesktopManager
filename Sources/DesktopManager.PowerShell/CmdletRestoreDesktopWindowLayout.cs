using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Restores window positions from a saved layout.</summary>
/// <para type="synopsis">Restores desktop window layout.</para>
[Cmdlet(VerbsData.Restore, "DesktopWindowLayout", SupportsShouldProcess = true)]
public sealed class CmdletRestoreDesktopWindowLayout : PSCmdlet {
    /// <summary>
    /// <para type="description">Path to the layout file.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; }

    /// <summary>
    /// <para type="description">Validate layout before applying.</para>
    /// </summary>
    [Parameter()]
    public SwitchParameter Validate { get; set; }

    /// <summary>
    /// Begin processing.
    /// </summary>
    protected override void BeginProcessing() {
        var manager = new WindowManager();
        if (ShouldProcess($"Layout file '{Path}'", "Restore desktop window layout")) {
            manager.LoadLayout(Path, Validate.IsPresent);
        }
    }
}
