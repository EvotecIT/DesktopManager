namespace DesktopManager.PowerShell;

/// <summary>Sets an explicit global airplane-mode state through an undocumented experimental Windows COM contract.</summary>
[Cmdlet(VerbsCommon.Set, "DesktopAirplaneMode", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(AirplaneModeState))]
public sealed class CmdletSetDesktopAirplaneMode : PSCmdlet {
    /// <summary><para type="description">The explicit Enabled or Disabled state.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public AirplaneModeState State;

    /// <summary><para type="description">Acknowledges that the global airplane-mode contract is experimental.</para></summary>
    [Parameter(Mandatory = true)]
    public SwitchParameter Experimental;

    /// <summary>Applies and verifies the experimental state.</summary>
    protected override void BeginProcessing() {
        if (!Experimental) {
            ThrowTerminatingError(new ErrorRecord(
                new InvalidOperationException("Experimental acknowledgement is required."),
                "ExperimentalAcknowledgementRequired",
                ErrorCategory.PermissionDenied,
                null));
        }
        if (ShouldProcess("Global Windows radio state", $"Set airplane mode {State}")) {
            WriteObject(new ExperimentalAirplaneModeService().SetState(State));
        }
    }
}
