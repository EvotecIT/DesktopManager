namespace DesktopManager.PowerShell;

/// <summary>Locks the current interactive workstation.</summary>
[Cmdlet(VerbsCommon.Lock, "DesktopSession", SupportsShouldProcess = true)]
public sealed class CmdletLockDesktopSession : PSCmdlet {
    /// <summary>Locks the workstation.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess("Current workstation", "Lock")) {
            new SystemPowerService().LockWorkstation();
        }
    }
}
