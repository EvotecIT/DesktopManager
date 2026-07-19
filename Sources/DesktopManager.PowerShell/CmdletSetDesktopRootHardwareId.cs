namespace DesktopManager.PowerShell;

/// <summary>Replaces the hardware identifier list of an exact ROOT-enumerated device.</summary>
/// <list type="alertSet">
///   <item><term>Expert operation</term><description>Changing identifiers can change driver matching after the next scan.</description></item>
/// </list>
[Cmdlet(VerbsCommon.Set, "DesktopRootHardwareId", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletSetDesktopRootHardwareId : PSCmdlet {
    /// <summary><para type="description">The exact ROOT device instance identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string InstanceId;

    /// <summary><para type="description">One or more exact hardware identifiers in replacement order.</para></summary>
    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string[] HardwareId;

    /// <summary>Replaces the hardware identifier list.</summary>
    protected override void ProcessRecord() {
        if (ShouldProcess(InstanceId, "Replace ROOT device hardware identifiers")) {
            WriteObject(new DeviceManagementService().SetRootHardwareIds(InstanceId, HardwareId));
        }
    }
}
