namespace DesktopManager.PowerShell;

/// <summary>Captures and saves current personalization state.</summary>
[Cmdlet(VerbsData.Save, "DesktopPersonalization")]
[OutputType(typeof(PersonalizationSnapshot))]
public sealed class CmdletSaveDesktopPersonalization : PSCmdlet {
    /// <summary><para type="description">The snapshot name.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name;

    /// <summary>Captures and saves state.</summary>
    protected override void BeginProcessing() {
        PersonalizationSnapshot snapshot = new PersonalizationService().CaptureSnapshot();
        PersonalizationStateStore.SaveSnapshot(Name, snapshot);
        WriteObject(snapshot);
    }
}
