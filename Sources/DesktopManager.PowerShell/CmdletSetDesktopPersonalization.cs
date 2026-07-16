namespace DesktopManager.PowerShell;

/// <summary>Applies a typed personalization settings object.</summary>
[Cmdlet(VerbsCommon.Set, "DesktopPersonalization", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopPersonalization : PSCmdlet {
    /// <summary><para type="description">The typed settings to apply.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public PersonalizationSettings InputObject;

    /// <summary><para type="description">Returns the resulting snapshot.</para></summary>
    [Parameter]
    public SwitchParameter PassThru;

    /// <summary>Applies settings.</summary>
    protected override void ProcessRecord() {
        if (!ShouldProcess("Current user personalization", "Apply settings")) {
            return;
        }
        var service = new PersonalizationService();
        service.Apply(InputObject);
        if (PassThru) {
            WriteObject(service.CaptureSnapshot());
        }
    }
}
