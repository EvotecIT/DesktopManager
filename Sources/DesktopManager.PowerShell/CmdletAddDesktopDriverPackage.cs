namespace DesktopManager.PowerShell;

/// <summary>Stages an INF package in the Driver Store and optionally installs it on matching devices.</summary>
/// <example>
///   <summary>Stage a driver package</summary>
///   <prefix>PS&gt; </prefix>
///   <code>Add-DesktopDriverPackage -InfPath C:\Drivers\device.inf -Confirm</code>
///   <para>Adds the package to the Driver Store without selecting it for devices.</para>
/// </example>
[Cmdlet(VerbsCommon.Add, "DesktopDriverPackage", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(DesktopDeviceOperationResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletAddDesktopDriverPackage : PSCmdlet {
    /// <summary><para type="description">The path to the package INF file.</para></summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string InfPath;

    /// <summary><para type="description">Installs the package on matching present devices after staging.</para></summary>
    [Parameter]
    public SwitchParameter Install;

    /// <summary><para type="description">Forces the INF when Install is selected, including a lower-ranked driver.</para></summary>
    [Parameter]
    public SwitchParameter Force;

    /// <summary>Stages or installs the driver package.</summary>
    protected override void ProcessRecord() {
        if (Force && !Install) {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException("Force can be used only with Install."),
                "DesktopDriverForceRequiresInstall",
                ErrorCategory.InvalidArgument,
                InfPath));
        }
        string action = Install ? (Force ? "Force-install driver package" : "Install driver package") : "Stage driver package";
        if (ShouldProcess(InfPath, action)) {
            var service = new DeviceManagementService();
            WriteObject(Install ? service.InstallDriver(InfPath, Force) : service.StageDriver(InfPath));
        }
    }
}
