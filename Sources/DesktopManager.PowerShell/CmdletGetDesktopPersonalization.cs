namespace DesktopManager.PowerShell;

/// <summary>Gets current or stored personalization state.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopPersonalization")]
[OutputType(typeof(PersonalizationSnapshot))]
public sealed class CmdletGetDesktopPersonalization : PSCmdlet {
    /// <summary><para type="description">Optional stored snapshot name.</para></summary>
    [Parameter(Position = 0)]
    public string Name;

    /// <summary><para type="description">Lists stored snapshot names.</para></summary>
    [Parameter]
    public SwitchParameter List;

    /// <summary>Gets personalization state.</summary>
    protected override void BeginProcessing() {
        if (List) {
            WriteObject(PersonalizationStateStore.ListSnapshots(), true);
        } else if (!string.IsNullOrWhiteSpace(Name)) {
            WriteObject(PersonalizationStateStore.LoadSnapshot(Name));
        } else {
            WriteObject(new PersonalizationService().CaptureSnapshot());
        }
    }
}
