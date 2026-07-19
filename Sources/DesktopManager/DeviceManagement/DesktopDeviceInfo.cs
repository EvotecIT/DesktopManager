namespace DesktopManager;

/// <summary>Describes one local Windows Plug and Play device instance.</summary>
public sealed class DesktopDeviceInfo {
    /// <summary>Gets the stable device instance identifier.</summary>
    public string InstanceId { get; internal set; } = string.Empty;

    /// <summary>Gets the Windows-provided friendly name, or the device description when no friendly name exists.</summary>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>Gets the device description.</summary>
    public string? Description { get; internal set; }

    /// <summary>Gets the manufacturer.</summary>
    public string? Manufacturer { get; internal set; }

    /// <summary>Gets the setup class name.</summary>
    public string? ClassName { get; internal set; }

    /// <summary>Gets the setup class identifier.</summary>
    public Guid ClassGuid { get; internal set; }

    /// <summary>Gets the bus enumerator name.</summary>
    public string? EnumeratorName { get; internal set; }

    /// <summary>Gets the Windows-reported location.</summary>
    public string? Location { get; internal set; }

    /// <summary>Gets the device container identifier.</summary>
    public Guid? ContainerId { get; internal set; }

    /// <summary>Gets whether the device is currently present.</summary>
    public bool Present { get; internal set; }

    /// <summary>Gets whether Windows reports a device problem.</summary>
    public bool HasProblem { get; internal set; }

    /// <summary>Gets the Configuration Manager problem code.</summary>
    public uint ProblemCode { get; internal set; }

    /// <summary>Gets the Configuration Manager devnode status flags.</summary>
    public uint StatusFlags { get; internal set; }

    /// <summary>Gets the device capability flags.</summary>
    public uint Capabilities { get; internal set; }

    /// <summary>Gets whether Windows does not declare the device disableable.</summary>
    public bool NotDisableable => (StatusFlags & 0x00002000u) == 0;

    /// <summary>Gets whether Windows declares that the device supports silent installation.</summary>
    public bool SilentInstall => (Capabilities & 0x00000020u) != 0;

    /// <summary>Gets the hardware identifiers.</summary>
    public IReadOnlyList<string> HardwareIds { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets the compatible identifiers.</summary>
    public IReadOnlyList<string> CompatibleIds { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets the installed driver information.</summary>
    public DesktopInstalledDriverInfo? Driver { get; internal set; }

    /// <summary>Gets device relations when requested.</summary>
    public DesktopDeviceRelations? Relations { get; internal set; }

    /// <summary>Gets the effective driver stack when requested.</summary>
    public IReadOnlyList<string> Stack { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets allocated hardware resources when requested.</summary>
    public IReadOnlyList<DesktopDeviceResourceInfo> Resources { get; internal set; } = Array.Empty<DesktopDeviceResourceInfo>();

    /// <summary>Gets registered device interfaces when requested.</summary>
    public IReadOnlyList<DesktopDeviceInterfaceInfo> Interfaces { get; internal set; } = Array.Empty<DesktopDeviceInterfaceInfo>();

    /// <summary>Gets unified device properties when requested.</summary>
    public IReadOnlyList<DesktopDevicePropertyInfo> Properties { get; internal set; } = Array.Empty<DesktopDevicePropertyInfo>();
}

/// <summary>Describes the driver currently selected for a device.</summary>
public sealed class DesktopInstalledDriverInfo {
    /// <summary>Gets the published INF name.</summary>
    public string? PublishedInfName { get; internal set; }

    /// <summary>Gets the INF section used for installation.</summary>
    public string? InfSection { get; internal set; }

    /// <summary>Gets the driver description.</summary>
    public string? Description { get; internal set; }

    /// <summary>Gets the provider.</summary>
    public string? Provider { get; internal set; }

    /// <summary>Gets the driver version.</summary>
    public string? Version { get; internal set; }

    /// <summary>Gets the driver date.</summary>
    public DateTime? Date { get; internal set; }

    /// <summary>Gets the matching hardware or compatible identifier.</summary>
    public string? MatchingDeviceId { get; internal set; }

    /// <summary>Gets the selected driver rank.</summary>
    public uint? Rank { get; internal set; }
}

/// <summary>Describes parent, child, sibling, and dependency relationships for a device.</summary>
public sealed class DesktopDeviceRelations {
    /// <summary>Gets the parent device instance identifier.</summary>
    public string? Parent { get; internal set; }

    /// <summary>Gets child device instance identifiers.</summary>
    public IReadOnlyList<string> Children { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets sibling device instance identifiers.</summary>
    public IReadOnlyList<string> Siblings { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets bus relation device instance identifiers.</summary>
    public IReadOnlyList<string> Bus { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets removal relation device instance identifiers.</summary>
    public IReadOnlyList<string> Removal { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets ejection relation device instance identifiers.</summary>
    public IReadOnlyList<string> Ejection { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets power relation device instance identifiers.</summary>
    public IReadOnlyList<string> Power { get; internal set; } = Array.Empty<string>();
}
