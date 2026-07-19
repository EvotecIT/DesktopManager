namespace DesktopManager;

/// <summary>Controls optional detail returned while enumerating third-party driver packages.</summary>
public sealed class DesktopDriverPackageQuery {
    /// <summary>Gets or sets an exact published INF name such as oem42.inf.</summary>
    public string? PublishedInfName { get; set; }

    /// <summary>Gets or sets a setup class identifier filter.</summary>
    public Guid? ClassGuid { get; set; }

    /// <summary>Gets or sets whether package files are returned.</summary>
    public bool IncludeFiles { get; set; }

    /// <summary>Gets or sets whether devices currently using each package are returned.</summary>
    public bool IncludeDevices { get; set; }
}

/// <summary>Describes a third-party package in the Windows Driver Store.</summary>
public sealed class DesktopDriverPackageInfo {
    /// <summary>Gets the published INF name, such as oem42.inf.</summary>
    public string PublishedInfName { get; internal set; } = string.Empty;

    /// <summary>Gets the original INF name.</summary>
    public string? OriginalInfName { get; internal set; }

    /// <summary>Gets the original catalog name.</summary>
    public string? CatalogName { get; internal set; }

    /// <summary>Gets the provider.</summary>
    public string? Provider { get; internal set; }

    /// <summary>Gets the setup class name.</summary>
    public string? ClassName { get; internal set; }

    /// <summary>Gets the setup class identifier.</summary>
    public Guid? ClassGuid { get; internal set; }

    /// <summary>Gets the driver date.</summary>
    public DateTime? DriverDate { get; internal set; }

    /// <summary>Gets the driver version.</summary>
    public string? DriverVersion { get; internal set; }

    /// <summary>Gets the INF location inside the Driver Store.</summary>
    public string? DriverStoreInfPath { get; internal set; }

    /// <summary>Gets package files when requested.</summary>
    public IReadOnlyList<string> Files { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets devices currently using the package when requested.</summary>
    public IReadOnlyList<string> DeviceInstanceIds { get; internal set; } = Array.Empty<string>();
}
