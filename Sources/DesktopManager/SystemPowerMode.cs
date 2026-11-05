namespace DesktopManager;

/// <summary>
/// The Windows power mode chosen in Settings, which Windows applies as an overlay on top of the active power plan.
/// </summary>
public enum SystemPowerMode {
    /// <summary>Windows did not report a power mode, or reported one this version does not recognize.</summary>
    Unknown,

    /// <summary>Balanced: no overlay is applied on top of the power plan.</summary>
    Balanced,

    /// <summary>Best power efficiency (overlay <c>961cc777-2547-4f9d-8174-7d86181b8a7a</c>).</summary>
    BestPowerEfficiency,

    /// <summary>Better battery (overlay <c>3af9b8d9-7c97-431d-ad78-34a8bfea439f</c>).</summary>
    BetterBattery,

    /// <summary>Best performance (overlay <c>ded574b5-45a0-4f42-8737-46345c09c238</c>).</summary>
    BestPerformance
}
