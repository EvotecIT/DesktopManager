namespace DesktopManager.PowerShell;

/// <summary>Removes a stored workstation profile.</summary>
[Cmdlet(VerbsCommon.Remove, "DesktopWorkstationProfile", SupportsShouldProcess = true)]
public sealed class CmdletRemoveDesktopWorkstationProfile : PSCmdlet {
    /// <summary><para type="description">The stored profile name.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name;

    /// <summary>Deletes the profile.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess(Name, "Remove workstation profile")) {
            WriteObject(WorkstationProfileStore.Delete(Name));
        }
    }
}
