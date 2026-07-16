namespace DesktopManager.PowerShell;

/// <summary>Gets global airplane mode through an undocumented experimental Windows COM contract.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopAirplaneMode")]
[OutputType(typeof(AirplaneModeState))]
public sealed class CmdletGetDesktopAirplaneMode : PSCmdlet {
    /// <summary><para type="description">Acknowledges that the global airplane-mode contract is experimental.</para></summary>
    [Parameter(Mandatory = true)]
    public SwitchParameter Experimental;

    /// <summary>Gets the experimental state.</summary>
    protected override void BeginProcessing() {
        if (!Experimental) {
            ThrowTerminatingError(new ErrorRecord(
                new InvalidOperationException("Experimental acknowledgement is required."),
                "ExperimentalAcknowledgementRequired",
                ErrorCategory.PermissionDenied,
                null));
        }
        WriteObject(new ExperimentalAirplaneModeService().GetState());
    }
}
