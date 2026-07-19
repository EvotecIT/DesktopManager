namespace DesktopManager.PowerShell;

/// <summary>Uninstalls an exact Plug and Play device instance.</summary>
/// <para>By default the device subtree is removed. Use DeviceOnly to uninstall only the selected instance.</para>
[Cmdlet(VerbsCommon.Remove, "DesktopDevice", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletRemoveDesktopDevice : PSCmdlet {
    /// <summary><para type="description">The exact device instance identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string InstanceId;

    /// <summary><para type="description">Uninstalls only the selected instance instead of its subtree.</para></summary>
    [Parameter]
    public SwitchParameter DeviceOnly;

    /// <summary>Uninstalls the selected device or subtree.</summary>
    protected override void ProcessRecord() {
        string action = DeviceOnly ? "Uninstall Plug and Play device" : "Remove Plug and Play device subtree";
        if (ShouldProcess(InstanceId, action)) {
            WriteObject(new DeviceManagementService().RemoveDevice(InstanceId, !DeviceOnly));
        }
    }
}
