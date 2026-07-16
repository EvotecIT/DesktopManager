namespace DesktopManager.PowerShell;

/// <summary>Removes a stored personalization snapshot.</summary>
[Cmdlet(VerbsCommon.Remove, "DesktopPersonalization", SupportsShouldProcess = true)]
public sealed class CmdletRemoveDesktopPersonalization : PSCmdlet {
    /// <summary><para type="description">The stored snapshot name.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name;

    /// <summary>Removes the snapshot.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess(Name, "Remove personalization snapshot")) {
            WriteObject(PersonalizationStateStore.DeleteSnapshot(Name));
        }
    }
}
