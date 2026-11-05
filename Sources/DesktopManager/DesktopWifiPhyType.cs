namespace DesktopManager;

/// <summary>
/// The 802.11 physical layer a Wi-Fi connection uses, as Windows reports it in <c>DOT11_PHY_TYPE</c>.
/// </summary>
public enum DesktopWifiPhyType {
    /// <summary>Windows did not report a PHY type, or reported a vendor-defined or unrecognized one.</summary>
    Unknown,

    /// <summary>Frequency-hopping spread spectrum (original 802.11).</summary>
    FrequencyHoppingSpreadSpectrum,

    /// <summary>Direct-sequence spread spectrum (original 802.11).</summary>
    DirectSequenceSpreadSpectrum,

    /// <summary>Infrared baseband (original 802.11).</summary>
    InfraredBaseband,

    /// <summary>Orthogonal frequency-division multiplexing (802.11a).</summary>
    Ofdm,

    /// <summary>High-rate DSSS (802.11b).</summary>
    HighRateDsss,

    /// <summary>Extended rate PHY (802.11g).</summary>
    ExtendedRate,

    /// <summary>High throughput (802.11n, Wi-Fi 4).</summary>
    HighThroughput,

    /// <summary>Very high throughput (802.11ac, Wi-Fi 5).</summary>
    VeryHighThroughput,

    /// <summary>Directional multi-gigabit (802.11ad).</summary>
    DirectionalMultiGigabit,

    /// <summary>High efficiency (802.11ax, Wi-Fi 6 and 6E).</summary>
    HighEfficiency,

    /// <summary>Extremely high throughput (802.11be, Wi-Fi 7).</summary>
    ExtremelyHighThroughput
}
