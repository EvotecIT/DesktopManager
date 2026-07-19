namespace DesktopManager.PowerShell;

/// <summary>Gets drivers Windows considers compatible with an exact device instance.</summary>
/// <example>
///   <summary>Inspect candidate drivers</summary>
///   <prefix>PS&gt; </prefix>
///   <code>Get-DesktopDeviceDriver -InstanceId 'PCI\VEN_1234&amp;DEV_5678\1'</code>
///   <para>Returns ranked compatible driver nodes without changing the selected driver.</para>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopDeviceDriver")]
[OutputType(typeof(DesktopDeviceDriverInfo))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletGetDesktopDeviceDriver : PSCmdlet {
    /// <summary><para type="description">The exact device instance identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string InstanceId;

    /// <summary>Gets compatible driver nodes.</summary>
    protected override void ProcessRecord() {
        WriteObject(new DeviceManagementService().GetCompatibleDrivers(InstanceId), true);
    }
}
