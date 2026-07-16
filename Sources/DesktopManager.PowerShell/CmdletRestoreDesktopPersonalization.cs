namespace DesktopManager.PowerShell;

/// <summary>Restores a stored personalization snapshot.</summary>
[Cmdlet(VerbsData.Restore, "DesktopPersonalization", SupportsShouldProcess = true)]
public sealed class CmdletRestoreDesktopPersonalization : PSCmdlet {
    /// <summary><para type="description">The stored snapshot name.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name;

    /// <summary><para type="description">Skips machine-wide lock-screen and Spotlight policy values.</para></summary>
    [Parameter]
    public SwitchParameter SkipMachinePolicies;

    /// <summary>Restores the snapshot.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess(Name, "Restore personalization snapshot")) {
            new PersonalizationService().Restore(
                PersonalizationStateStore.LoadSnapshot(Name),
                restoreMachinePolicies: !SkipMachinePolicies);
        }
    }
}
