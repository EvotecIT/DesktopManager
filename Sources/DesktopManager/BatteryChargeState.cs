namespace DesktopManager;

/// <summary>
/// Describes the current battery charging condition.
/// </summary>
[Flags]
public enum BatteryChargeState : byte {
    /// <summary>No battery charge condition is reported.</summary>
    None = 0,
    /// <summary>The battery level is high.</summary>
    High = 1,
    /// <summary>The battery level is low.</summary>
    Low = 2,
    /// <summary>The battery level is critical.</summary>
    Critical = 4,
    /// <summary>The battery is charging.</summary>
    Charging = 8,
    /// <summary>No system battery is present.</summary>
    NoBattery = 128,
    /// <summary>Windows could not determine the battery condition.</summary>
    Unknown = 255
}
