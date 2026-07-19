namespace DesktopManager;

/// <summary>Describes the outcome of a device or driver mutation.</summary>
public sealed class DesktopDeviceOperationResult {
    /// <summary>Gets the requested operation.</summary>
    public string Operation { get; internal set; } = string.Empty;

    /// <summary>Gets the exact device, INF, class, or destination target.</summary>
    public string Target { get; internal set; } = string.Empty;

    /// <summary>Gets whether the native operation completed successfully.</summary>
    public bool Succeeded { get; internal set; }

    /// <summary>Gets whether Windows state was observed to change, or null when the native API does not report that distinction.</summary>
    public bool? Changed { get; internal set; }

    /// <summary>Gets whether Windows requires a system restart, or null when the native result could not be read.</summary>
    public bool? RebootRequired { get; internal set; }

    /// <summary>Gets the Configuration Manager return code when applicable.</summary>
    public uint? ConfigurationManagerCode { get; internal set; }

    /// <summary>Gets the Win32 error code when applicable.</summary>
    public int? Win32Error { get; internal set; }

    /// <summary>Gets a user-readable native outcome.</summary>
    public string? Message { get; internal set; }

    /// <summary>Gets a Plug and Play veto category when the operation was rejected.</summary>
    public string? VetoType { get; internal set; }

    /// <summary>Gets the component that vetoed the operation.</summary>
    public string? VetoName { get; internal set; }

    /// <summary>Gets the published INF name produced by a staging operation.</summary>
    public string? PublishedInfName { get; internal set; }

    /// <summary>Gets affected device instance identifiers when known.</summary>
    public IReadOnlyList<string> AffectedDeviceInstanceIds { get; internal set; } = Array.Empty<string>();

    internal static DesktopDeviceOperationResult Success(
        string operation,
        string target,
        bool? changed = true,
        bool? rebootRequired = false,
        string? message = null) {
        return new DesktopDeviceOperationResult {
            Operation = operation,
            Target = target,
            Succeeded = true,
            Changed = changed,
            RebootRequired = rebootRequired,
            Message = message
        };
    }
}
