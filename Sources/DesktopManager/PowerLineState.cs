namespace DesktopManager;

/// <summary>
/// Describes whether the computer is connected to external power.
/// </summary>
public enum PowerLineState {
    /// <summary>The computer is running from battery power.</summary>
    Offline = 0,
    /// <summary>The computer is connected to external power.</summary>
    Online = 1,
    /// <summary>Windows could not determine the external power state.</summary>
    Unknown = 255
}
