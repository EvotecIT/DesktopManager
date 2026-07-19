namespace DesktopManager.PowerShell;

/// <summary>Updates present devices matching an exact hardware identifier from an INF package.</summary>
[Cmdlet(VerbsData.Update, "DesktopDeviceDriver", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletUpdateDesktopDeviceDriver : PSCmdlet {
    /// <summary><para type="description">The path to the package INF file.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string InfPath;

    /// <summary><para type="description">The exact hardware identifier to update.</para></summary>
    [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string HardwareId;

    /// <summary><para type="description">Forces selection of the INF, including a lower-ranked driver.</para></summary>
    [Parameter]
    public SwitchParameter Force;

    /// <summary>Updates matching present devices.</summary>
    protected override void ProcessRecord() {
        string action = Force ? "Force-update matching device drivers" : "Update matching device drivers";
        if (ShouldProcess(HardwareId, action)) {
            WriteObject(new DeviceManagementService().UpdateDriver(InfPath, HardwareId, Force));
        }
    }
}
