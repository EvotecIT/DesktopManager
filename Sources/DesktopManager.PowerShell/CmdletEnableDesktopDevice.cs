namespace DesktopManager.PowerShell;

/// <summary>Enables an exact Windows Plug and Play device instance.</summary>
[Cmdlet(VerbsLifecycle.Enable, "DesktopDevice", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletEnableDesktopDevice : PSCmdlet {
    /// <summary><para type="description">The exact device instance identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string InstanceId;

    /// <summary>Enables the selected device.</summary>
    protected override void ProcessRecord() {
        if (ShouldProcess(InstanceId, "Enable Plug and Play device")) {
            WriteObject(new DeviceManagementService().EnableDevice(InstanceId));
        }
    }
}
