namespace DesktopManager.PowerShell;

/// <summary>Gets radios through the supported Windows radio API.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopRadio")]
[OutputType(typeof(DesktopRadioInfo))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.14393.0")]
public sealed class CmdletGetDesktopRadio : PSCmdlet {
    /// <summary><para type="description">Optional radio technology filter.</para></summary>
    [Parameter]
    public DesktopRadioKind? Kind;

    /// <summary>Gets matching radio snapshots.</summary>
    protected override void BeginProcessing() {
        using var service = new RadioService();
        foreach (DesktopRadioInfo radio in service.GetRadiosAsync().GetAwaiter().GetResult()) {
            if (!Kind.HasValue || radio.Kind == Kind.Value) {
                WriteObject(radio);
            }
        }
    }
}
