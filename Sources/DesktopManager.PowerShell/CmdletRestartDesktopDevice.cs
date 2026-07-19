namespace DesktopManager.PowerShell;

/// <summary>Restarts an exact Plug and Play device without rebooting Windows.</summary>
[Cmdlet(VerbsLifecycle.Restart, "DesktopDevice", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletRestartDesktopDevice : PSCmdlet {
    /// <summary><para type="description">The exact device instance identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string InstanceId;

    /// <summary>Restarts the selected device.</summary>
    protected override void ProcessRecord() {
        if (ShouldProcess(InstanceId, "Restart Plug and Play device")) {
            WriteObject(new DeviceManagementService().RestartDevice(InstanceId));
        }
    }
}
