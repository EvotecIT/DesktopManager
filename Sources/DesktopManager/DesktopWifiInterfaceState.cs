namespace DesktopManager;

/// <summary>
/// Describes the connection state Windows reports for a wireless LAN interface.
/// </summary>
public enum DesktopWifiInterfaceState {
    /// <summary>The interface is not ready.</summary>
    NotReady,

    /// <summary>The interface is connected.</summary>
    Connected,

    /// <summary>The interface formed an ad hoc network.</summary>
    AdHocNetworkFormed,

    /// <summary>The interface is disconnecting.</summary>
    Disconnecting,

    /// <summary>The interface is disconnected.</summary>
    Disconnected,

    /// <summary>The interface is associating with a network.</summary>
    Associating,

    /// <summary>The interface is discovering connection settings.</summary>
    Discovering,

    /// <summary>The interface is authenticating.</summary>
    Authenticating,

    /// <summary>Windows returned an interface state this version does not recognize.</summary>
    Unknown
}
