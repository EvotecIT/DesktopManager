namespace DesktopManager.PowerShell;

/// <summary>Gets local Windows Plug and Play device instances.</summary>
/// <para>Filters use exact identifiers. Optional switches add expensive relation, resource, interface, stack, or unified-property details.</para>
/// <example>
///   <summary>List present display devices</summary>
///   <prefix>PS&gt; </prefix>
///   <code>Get-DesktopDevice -Class Display -Present</code>
///   <para>Returns present devices in the Display setup class.</para>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopDevice")]
[OutputType(typeof(DesktopDeviceInfo))]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.15063.0")]
public sealed class CmdletGetDesktopDevice : PSCmdlet {
    /// <summary><para type="description">An exact device instance identifier.</para></summary>
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    public string InstanceId;

    /// <summary><para type="description">An exact hardware or compatible identifier.</para></summary>
    [Parameter]
    public string DeviceId;

    /// <summary><para type="description">An exact setup class name.</para></summary>
    [Parameter]
    public string Class;

    /// <summary><para type="description">An exact setup class identifier.</para></summary>
    [Parameter]
    public Guid? ClassGuid;

    /// <summary><para type="description">An exact bus enumerator name.</para></summary>
    [Parameter]
    public string Enumerator;

    /// <summary><para type="description">Returns only present devices.</para></summary>
    [Parameter]
    public SwitchParameter Present;

    /// <summary><para type="description">Returns only non-present devices.</para></summary>
    [Parameter]
    public SwitchParameter NonPresent;

    /// <summary><para type="description">Returns only devices with a problem.</para></summary>
    [Parameter]
    public SwitchParameter Problem;

    /// <summary><para type="description">Selects an exact Configuration Manager problem code.</para></summary>
    [Parameter]
    public uint? ProblemCode;

    /// <summary><para type="description">Includes parent, child, sibling, and dependency relations.</para></summary>
    [Parameter]
    public SwitchParameter IncludeRelations;

    /// <summary><para type="description">Includes the effective driver stack.</para></summary>
    [Parameter]
    public SwitchParameter IncludeStack;

    /// <summary><para type="description">Includes allocated hardware resources.</para></summary>
    [Parameter]
    public SwitchParameter IncludeResources;

    /// <summary><para type="description">Includes registered device interfaces.</para></summary>
    [Parameter]
    public SwitchParameter IncludeInterfaces;

    /// <summary><para type="description">Includes every available unified device property.</para></summary>
    [Parameter]
    public SwitchParameter IncludeProperties;

    /// <summary>Gets matching device instances.</summary>
    protected override void BeginProcessing() {
        if (Present && NonPresent) {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException("Present and NonPresent cannot be combined."),
                "DesktopDevicePresenceConflict",
                ErrorCategory.InvalidArgument,
                null));
        }
        var query = new DesktopDeviceQuery {
            InstanceId = InstanceId,
            DeviceId = DeviceId,
            ClassName = Class,
            ClassGuid = ClassGuid,
            EnumeratorName = Enumerator,
            Present = Present ? true : NonPresent ? false : null,
            HasProblem = Problem ? true : null,
            ProblemCode = ProblemCode,
            IncludeRelations = IncludeRelations,
            IncludeStack = IncludeStack,
            IncludeResources = IncludeResources,
            IncludeInterfaces = IncludeInterfaces,
            IncludeProperties = IncludeProperties
        };
        WriteObject(new DeviceManagementService().GetDevices(query), true);
    }
}
