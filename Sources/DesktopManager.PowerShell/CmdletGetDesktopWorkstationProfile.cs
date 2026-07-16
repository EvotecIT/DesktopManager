namespace DesktopManager.PowerShell;

/// <summary>Gets stored workstation profiles or a live profile snapshot.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopWorkstationProfile")]
public sealed class CmdletGetDesktopWorkstationProfile : PSCmdlet {
    /// <summary><para type="description">Optional stored profile name.</para></summary>
    [Parameter(Position = 0)]
    public string Name;

    /// <summary><para type="description">Captures the live workstation instead of reading storage.</para></summary>
    [Parameter]
    public SwitchParameter Current;

    /// <summary>Gets the requested profile data.</summary>
    protected override void BeginProcessing() {
        if (Current) {
            WriteObject(new WorkstationProfileService().CaptureProfile());
        } else if (!string.IsNullOrWhiteSpace(Name)) {
            WriteObject(WorkstationProfileStore.Load(Name));
        } else {
            WriteObject(WorkstationProfileStore.List(), true);
        }
    }
}
