namespace DesktopManager;

/// <summary>
/// Identifies a Windows radio technology.
/// </summary>
public enum DesktopRadioKind {
    /// <summary>The radio kind is not recognized by this version of DesktopManager.</summary>
    Other = 0,
    /// <summary>A Wi-Fi radio.</summary>
    WiFi = 1,
    /// <summary>A mobile broadband radio.</summary>
    MobileBroadband = 2,
    /// <summary>A Bluetooth radio.</summary>
    Bluetooth = 3,
    /// <summary>An FM radio.</summary>
    FM = 4
}
