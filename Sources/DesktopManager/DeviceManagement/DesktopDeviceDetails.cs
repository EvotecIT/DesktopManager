namespace DesktopManager;

/// <summary>Describes an allocated hardware resource for a device.</summary>
public sealed class DesktopDeviceResourceInfo {
    /// <summary>Gets the resource kind, such as Memory, IoPort, Dma, Irq, or BusNumber.</summary>
    public string Kind { get; internal set; } = string.Empty;

    /// <summary>Gets the allocated start value.</summary>
    public ulong Start { get; internal set; }

    /// <summary>Gets the allocated end value.</summary>
    public ulong End { get; internal set; }

    /// <summary>Gets native resource flags.</summary>
    public uint Flags { get; internal set; }

    /// <summary>Gets a human-readable representation.</summary>
    public string DisplayValue { get; internal set; } = string.Empty;
}

/// <summary>Describes a registered device interface.</summary>
public sealed class DesktopDeviceInterfaceInfo {
    /// <summary>Gets the interface class identifier.</summary>
    public Guid ClassGuid { get; internal set; }

    /// <summary>Gets the symbolic device interface path.</summary>
    public string Path { get; internal set; } = string.Empty;

    /// <summary>Gets whether the interface is active.</summary>
    public bool Enabled { get; internal set; }

    /// <summary>Gets whether the interface is the default interface for its class.</summary>
    public bool Default { get; internal set; }

    /// <summary>Gets whether Windows reports that the interface was removed.</summary>
    public bool Removed { get; internal set; }
}

/// <summary>Describes one value from the unified Windows device property model.</summary>
public sealed class DesktopDevicePropertyInfo {
    /// <summary>Gets the property key in GUID/PID form or a known friendly name.</summary>
    public string Key { get; internal set; } = string.Empty;

    /// <summary>Gets the native DEVPROPTYPE value.</summary>
    public uint PropertyType { get; internal set; }

    /// <summary>Gets the property value converted to a JSON-serializable .NET value.</summary>
    public object? Value { get; internal set; }
}
