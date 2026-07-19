namespace DesktopManager.PowerShell;

/// <summary>Gets third-party packages from the Windows Driver Store.</summary>
/// <example>
///   <summary>Inspect one published package and its devices</summary>
///   <prefix>PS&gt; </prefix>
///   <code>Get-DesktopDriverPackage -PublishedInfName oem42.inf -IncludeDevices</code>
///   <para>Returns the exact package and device instances currently using it.</para>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopDriverPackage")]
[OutputType(typeof(DesktopDriverPackageInfo))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletGetDesktopDriverPackage : PSCmdlet {
    /// <summary><para type="description">An exact published INF name such as oem42.inf.</para></summary>
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    public string PublishedInfName;

    /// <summary><para type="description">An optional setup class identifier.</para></summary>
    [Parameter]
    public Guid? ClassGuid;

    /// <summary><para type="description">Includes files in each package.</para></summary>
    [Parameter]
    public SwitchParameter IncludeFiles;

    /// <summary><para type="description">Includes device instances using each package.</para></summary>
    [Parameter]
    public SwitchParameter IncludeDevices;

    /// <summary>Gets matching Driver Store packages.</summary>
    protected override void BeginProcessing() {
        WriteObject(new DeviceManagementService().GetDriverPackages(new DesktopDriverPackageQuery {
            PublishedInfName = PublishedInfName,
            ClassGuid = ClassGuid,
            IncludeFiles = IncludeFiles,
            IncludeDevices = IncludeDevices
        }), true);
    }
}
