namespace DesktopManager.PowerShell;

/// <summary>Signs out the current interactive Windows session.</summary>
[Cmdlet("Exit", "DesktopSession", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class CmdletExitDesktopSession : PSCmdlet {
    /// <summary><para type="description">Forces applications to close during sign-out.</para></summary>
    [Parameter]
    public SwitchParameter Force;

    /// <summary>Requests sign-out after PowerShell confirmation.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess("Current Windows session", "Sign out")) {
            new SystemPowerService().SignOut(Force);
        }
    }
}
