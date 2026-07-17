namespace DesktopManager;

/// <summary>
/// Describes the observed outcome of a saved Wi-Fi profile connection request.
/// </summary>
public enum DesktopWifiConnectionOutcome {
    /// <summary>Windows reported that the connection completed successfully.</summary>
    Connected,

    /// <summary>Windows reported that the connection failed.</summary>
    Failed,

    /// <summary>No completion notification arrived within the requested wait period.</summary>
    TimedOut
}
