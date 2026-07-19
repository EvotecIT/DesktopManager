namespace DesktopManager;

/// <summary>Describes a Windows device setup class.</summary>
public sealed class DesktopDeviceClassInfo {
    /// <summary>Gets the setup class identifier.</summary>
    public Guid ClassGuid { get; internal set; }

    /// <summary>Gets the setup class name.</summary>
    public string? Name { get; internal set; }

    /// <summary>Gets the localized setup class description.</summary>
    public string? Description { get; internal set; }

    /// <summary>Gets the default service, when defined.</summary>
    public string? DefaultService { get; internal set; }

    /// <summary>Gets upper class-filter drivers in load order.</summary>
    public IReadOnlyList<string> UpperFilters { get; internal set; } = Array.Empty<string>();

    /// <summary>Gets lower class-filter drivers in load order.</summary>
    public IReadOnlyList<string> LowerFilters { get; internal set; } = Array.Empty<string>();
}

/// <summary>Groups devices that Windows associates with one physical device container.</summary>
public sealed class DesktopDeviceContainerInfo {
    /// <summary>Gets the container identifier.</summary>
    public Guid ContainerId { get; internal set; }

    /// <summary>Gets whether at least one device in the container is present.</summary>
    public bool Connected { get; internal set; }

    /// <summary>Gets whether at least one device in the container has a problem.</summary>
    public bool HasProblem { get; internal set; }

    /// <summary>Gets the devices in the container.</summary>
    public IReadOnlyList<DesktopDeviceInfo> Devices { get; internal set; } = Array.Empty<DesktopDeviceInfo>();
}

/// <summary>Selects the upper or lower class-filter chain.</summary>
public enum DesktopDeviceClassFilterKind {
    /// <summary>Selects upper class-filter drivers.</summary>
    Upper,

    /// <summary>Selects lower class-filter drivers.</summary>
    Lower
}
