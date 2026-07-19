namespace DesktopManager.PowerShell;

/// <summary>Removes an exact published third-party package from the Driver Store.</summary>
[Cmdlet(VerbsCommon.Remove, "DesktopDriverPackage", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletRemoveDesktopDriverPackage : PSCmdlet {
    /// <summary><para type="description">The exact published INF name, such as oem42.inf.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string PublishedInfName;

    /// <summary><para type="description">Uninstalls the package from devices before deleting it.</para></summary>
    [Parameter]
    public SwitchParameter UninstallDevices;

    /// <summary><para type="description">Forces direct Driver Store deletion when UninstallDevices is not set. With UninstallDevices, native package uninstall reassigns affected devices before removing the package, so Force is accepted but redundant.</para></summary>
    [Parameter]
    public SwitchParameter Force;

    /// <summary>Removes the package.</summary>
    protected override void ProcessRecord() {
        string action = UninstallDevices ? "Uninstall devices and remove driver package" : "Remove driver package";
        if (Force) {
            action = "Force-" + char.ToLowerInvariant(action[0]) + action.Substring(1);
        }
        if (ShouldProcess(PublishedInfName, action)) {
            WriteObject(new DeviceManagementService().DeleteDriver(PublishedInfName, UninstallDevices, Force));
        }
    }
}
