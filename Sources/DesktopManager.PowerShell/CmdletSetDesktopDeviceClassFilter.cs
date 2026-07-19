namespace DesktopManager.PowerShell;

/// <summary>Replaces the upper or lower filter-service chain for an exact device setup class.</summary>
/// <para>Every named filter service must already exist. An empty Service list removes the selected filter property.</para>
/// <list type="alertSet">
///   <item><term>Expert operation</term><description>An invalid filter chain can prevent every device in the class from starting.</description></item>
/// </list>
[Cmdlet(VerbsCommon.Set, "DesktopDeviceClassFilter", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletSetDesktopDeviceClassFilter : PSCmdlet {
    /// <summary><para type="description">The exact device setup class identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    public Guid ClassGuid;

    /// <summary><para type="description">Selects the Upper or Lower filter chain.</para></summary>
    [Parameter(Mandatory = true, Position = 1)]
    public DesktopDeviceClassFilterKind Kind;

    /// <summary><para type="description">The replacement filter-service names in load order. Supply an empty array to remove the property.</para></summary>
    [Parameter(Mandatory = true, Position = 2)]
    [AllowEmptyCollection]
    public string[] Service;

    /// <summary>Replaces the selected class-filter chain.</summary>
    protected override void ProcessRecord() {
        string target = $"{ClassGuid:D} {Kind}Filters";
        if (ShouldProcess(target, "Replace device class filter chain")) {
            WriteObject(new DeviceManagementService().SetClassFilters(ClassGuid, Kind, Service));
        }
    }
}
