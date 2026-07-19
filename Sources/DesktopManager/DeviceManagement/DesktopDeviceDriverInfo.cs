namespace DesktopManager;

/// <summary>Describes a driver candidate compatible with a specific device.</summary>
public sealed class DesktopDeviceDriverInfo {
    /// <summary>Gets the device instance identifier.</summary>
    public string InstanceId { get; internal set; } = string.Empty;

    /// <summary>Gets the driver description.</summary>
    public string Description { get; internal set; } = string.Empty;

    /// <summary>Gets the manufacturer.</summary>
    public string? Manufacturer { get; internal set; }

    /// <summary>Gets the provider.</summary>
    public string? Provider { get; internal set; }

    /// <summary>Gets the driver date.</summary>
    public DateTime? Date { get; internal set; }

    /// <summary>Gets the dotted driver version.</summary>
    public string? Version { get; internal set; }

    /// <summary>Gets the driver rank used by Windows selection.</summary>
    public uint Rank { get; internal set; }

    /// <summary>Gets the native driver-node flags.</summary>
    public uint Flags { get; internal set; }

    /// <summary>Gets the source INF path.</summary>
    public string? InfPath { get; internal set; }

    /// <summary>Gets the INF section.</summary>
    public string? InfSection { get; internal set; }

    /// <summary>Gets the primary matching hardware identifier.</summary>
    public string? HardwareId { get; internal set; }

    /// <summary>Gets additional compatible identifiers.</summary>
    public IReadOnlyList<string> CompatibleIds { get; internal set; } = Array.Empty<string>();
}
