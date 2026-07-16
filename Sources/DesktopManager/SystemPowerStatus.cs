using System;

namespace DesktopManager;

/// <summary>
/// Captures the current Windows power and battery state.
/// </summary>
public sealed class SystemPowerStatus {
    internal SystemPowerStatus(
        PowerLineState powerLineState,
        BatteryChargeState batteryChargeState,
        int? batteryPercent,
        TimeSpan? batteryLifeRemaining,
        TimeSpan? fullBatteryLife) {
        PowerLineState = powerLineState;
        BatteryChargeState = batteryChargeState;
        BatteryPercent = batteryPercent;
        BatteryLifeRemaining = batteryLifeRemaining;
        FullBatteryLife = fullBatteryLife;
    }

    /// <summary>Gets whether external power is connected.</summary>
    public PowerLineState PowerLineState { get; }

    /// <summary>Gets the battery charging condition.</summary>
    public BatteryChargeState BatteryChargeState { get; }

    /// <summary>Gets the battery percentage, or <c>null</c> when Windows reports it as unknown.</summary>
    public int? BatteryPercent { get; }

    /// <summary>Gets estimated battery life remaining, or <c>null</c> when unavailable.</summary>
    public TimeSpan? BatteryLifeRemaining { get; }

    /// <summary>Gets estimated full battery life, or <c>null</c> when unavailable.</summary>
    public TimeSpan? FullBatteryLife { get; }

    /// <summary>Gets whether Windows does not explicitly report that no battery is installed.</summary>
    public bool HasBattery => BatteryChargeState != BatteryChargeState.NoBattery;
}
