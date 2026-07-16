namespace DesktopManager.PowerShell;

/// <summary>Gets current AC and battery state.</summary>
[Cmdlet(VerbsCommon.Get, "DesktopPowerStatus")]
[OutputType(typeof(SystemPowerStatus))]
public sealed class CmdletGetDesktopPowerStatus : PSCmdlet {
    /// <summary>Gets the current power snapshot.</summary>
    protected override void BeginProcessing() {
        WriteObject(new SystemPowerService().GetStatus());
    }
}
