namespace DesktopManager.PowerShell;

/// <summary>Rolls an exact device instance back to its previous installed driver.</summary>
[Cmdlet(VerbsData.Restore, "DesktopDeviceDriver", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletRestoreDesktopDeviceDriver : PSCmdlet {
    /// <summary><para type="description">The exact device instance identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string InstanceId;

    /// <summary>Rolls back the selected device driver.</summary>
    protected override void ProcessRecord() {
        if (ShouldProcess(InstanceId, "Roll back device driver")) {
            WriteObject(new DeviceManagementService().RollbackDriver(InstanceId));
        }
    }
}
