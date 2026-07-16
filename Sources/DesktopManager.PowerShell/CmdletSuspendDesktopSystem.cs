namespace DesktopManager.PowerShell;

/// <summary>Requests Windows sleep or hibernation.</summary>
[Cmdlet(VerbsLifecycle.Suspend, "DesktopSystem", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class CmdletSuspendDesktopSystem : PSCmdlet {
    /// <summary><para type="description">Requests hibernation instead of sleep.</para></summary>
    [Parameter]
    public SwitchParameter Hibernate;

    /// <summary><para type="description">Forces immediate suspension.</para></summary>
    [Parameter]
    public SwitchParameter Force;

    /// <summary>Requests suspension.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess("Local computer", Hibernate ? "Hibernate" : "Sleep")) {
            new SystemPowerService().Suspend(Hibernate, Force);
        }
    }
}
