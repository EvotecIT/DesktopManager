namespace DesktopManager;

/// <summary>
/// Describes the Windows Terminal Services connection state for a desktop session.
/// </summary>
public enum DesktopSessionConnectState {
    /// <summary>The session is active.</summary>
    Active = 0,
    /// <summary>The session is connected.</summary>
    Connected = 1,
    /// <summary>The session is connecting.</summary>
    ConnectQuery = 2,
    /// <summary>The session is shadowing another session.</summary>
    Shadow = 3,
    /// <summary>The session is disconnected.</summary>
    Disconnected = 4,
    /// <summary>The session is idle.</summary>
    Idle = 5,
    /// <summary>The session is listening for connections.</summary>
    Listen = 6,
    /// <summary>The session is resetting.</summary>
    Reset = 7,
    /// <summary>The session is down.</summary>
    Down = 8,
    /// <summary>The session is initializing.</summary>
    Initializing = 9,
    /// <summary>The state is unknown.</summary>
    Unknown = -1
}
