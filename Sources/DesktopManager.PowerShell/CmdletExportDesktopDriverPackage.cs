namespace DesktopManager.PowerShell;

/// <summary>Exports one exact third-party package from the Driver Store.</summary>
[Cmdlet(VerbsData.Export, "DesktopDriverPackage", SupportsShouldProcess = true)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletExportDesktopDriverPackage : PSCmdlet {
    /// <summary><para type="description">The exact published INF name, such as oem42.inf.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string PublishedInfName;

    /// <summary><para type="description">The directory that receives the exported package folder.</para></summary>
    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Destination;

    /// <summary><para type="description">Overwrites package files already present in the destination.</para></summary>
    [Parameter]
    public SwitchParameter Force;

    /// <summary>Exports the package.</summary>
    protected override void ProcessRecord() {
        if (ShouldProcess(PublishedInfName, $"Export driver package to '{Destination}'")) {
            WriteObject(new DeviceManagementService().ExportDriver(PublishedInfName, Destination, Force));
        }
    }
}
