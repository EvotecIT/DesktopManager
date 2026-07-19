namespace DesktopManager;

/// <summary>
/// Selects local Plug and Play devices and controls which expensive detail families are returned.
/// </summary>
public sealed class DesktopDeviceQuery {
    /// <summary>Gets or sets an exact device instance identifier.</summary>
    public string? InstanceId { get; set; }

    /// <summary>Gets or sets a hardware or compatible identifier to match exactly.</summary>
    public string? DeviceId { get; set; }

    /// <summary>Gets or sets a setup class name to match.</summary>
    public string? ClassName { get; set; }

    /// <summary>Gets or sets a setup class identifier to match.</summary>
    public Guid? ClassGuid { get; set; }

    /// <summary>Gets or sets a bus enumerator name to match.</summary>
    public string? EnumeratorName { get; set; }

    /// <summary>Gets or sets whether present or non-present devices are returned.</summary>
    public bool? Present { get; set; }

    /// <summary>Gets or sets whether only devices with or without a problem are returned.</summary>
    public bool? HasProblem { get; set; }

    /// <summary>Gets or sets a specific Configuration Manager problem code to match.</summary>
    public uint? ProblemCode { get; set; }

    /// <summary>Gets or sets whether parent, child, sibling, and other Plug and Play relations are returned.</summary>
    public bool IncludeRelations { get; set; }

    /// <summary>Gets or sets whether the effective device stack is returned.</summary>
    public bool IncludeStack { get; set; }

    /// <summary>Gets or sets whether allocated hardware resources are returned.</summary>
    public bool IncludeResources { get; set; }

    /// <summary>Gets or sets whether registered device interfaces are returned.</summary>
    public bool IncludeInterfaces { get; set; }

    /// <summary>Gets or sets whether every available unified device property is returned.</summary>
    public bool IncludeProperties { get; set; }

    internal void Validate() {
        if (!string.IsNullOrWhiteSpace(InstanceId) && ContainsWildcard(InstanceId!)) {
            throw new ArgumentException("Device instance identifiers must be exact and cannot contain wildcards.", nameof(InstanceId));
        }
        if (!string.IsNullOrWhiteSpace(DeviceId) && ContainsWildcard(DeviceId!)) {
            throw new ArgumentException("Hardware and compatible identifiers must be exact and cannot contain wildcards.", nameof(DeviceId));
        }
        if (ProblemCode.HasValue && HasProblem == false) {
            throw new ArgumentException("ProblemCode cannot be combined with HasProblem=false.", nameof(ProblemCode));
        }
    }

    private static bool ContainsWildcard(string value) {
        return value.IndexOf('*') >= 0 || value.IndexOf('?') >= 0;
    }
}
