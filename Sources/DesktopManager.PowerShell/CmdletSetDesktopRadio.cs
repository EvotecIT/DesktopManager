namespace DesktopManager.PowerShell;

/// <summary>Sets an explicit state through the supported Windows radio API.</summary>
[Cmdlet(VerbsCommon.Set, "DesktopRadio", SupportsShouldProcess = true)]
[OutputType(typeof(DesktopRadioSetResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.14393.0")]
public sealed class CmdletSetDesktopRadio : PSCmdlet {
    /// <summary><para type="description">The radio technology to select.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public DesktopRadioKind Kind;

    /// <summary><para type="description">The explicit On or Off state.</para></summary>
    [Parameter(Mandatory = true, Position = 1)]
    public DesktopRadioState State;

    /// <summary><para type="description">Optional exact Windows-provided radio name.</para></summary>
    [Parameter]
    public string Name;

    /// <summary>Applies the explicit state.</summary>
    protected override void BeginProcessing() {
        if (!ShouldProcess(string.IsNullOrWhiteSpace(Name) ? Kind.ToString() : Name, $"Set radio {State}")) {
            return;
        }
        using var service = new RadioService();
        WriteObject(service.SetRadioStateAsync(Kind, State, Name).GetAwaiter().GetResult(), true);
    }
}
