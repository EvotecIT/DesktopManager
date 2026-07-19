namespace DesktopManager.PowerShell;

/// <summary>Creates a ROOT-enumerated device and installs an INF package for it.</summary>
/// <list type="alertSet">
///   <item><term>Expert operation</term><description>Use only with an INF designed for the supplied ROOT hardware identifier.</description></item>
/// </list>
[Cmdlet(VerbsCommon.New, "DesktopRootDevice", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletNewDesktopRootDevice : PSCmdlet {
    /// <summary><para type="description">The path to the package INF file.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string InfPath;

    /// <summary><para type="description">The exact ROOT hardware identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string HardwareId;

    /// <summary>Creates and installs the ROOT device.</summary>
    protected override void ProcessRecord() {
        if (ShouldProcess(HardwareId, "Create ROOT-enumerated device and install driver")) {
            WriteObject(new DeviceManagementService().CreateRootDevice(InfPath, HardwareId));
        }
    }
}
