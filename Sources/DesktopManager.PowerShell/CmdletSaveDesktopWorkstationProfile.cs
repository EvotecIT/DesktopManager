namespace DesktopManager.PowerShell;

/// <summary>Captures and saves a named workstation profile.</summary>
/// <para type="description">Stores display, personalization, taskbar, and active audio state together.</para>
[Cmdlet(VerbsData.Save, "DesktopWorkstationProfile")]
[OutputType(typeof(WorkstationProfile))]
public sealed class CmdletSaveDesktopWorkstationProfile : PSCmdlet {
    /// <summary><para type="description">The profile name.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name;

    /// <summary>Captures and stores the profile.</summary>
    protected override void BeginProcessing() {
        WriteObject(new WorkstationProfileService().SaveProfile(Name));
    }
}
