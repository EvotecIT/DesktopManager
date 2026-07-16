namespace DesktopManager;

/// <summary>
/// Describes the result of requesting permission to change radio state.
/// </summary>
public enum DesktopRadioAccessStatus {
    /// <summary>The access status is unknown.</summary>
    Unspecified = 0,
    /// <summary>Radio state changes are allowed.</summary>
    Allowed = 1,
    /// <summary>The user denied access.</summary>
    DeniedByUser = 2,
    /// <summary>The operating system or policy denied access.</summary>
    DeniedBySystem = 3
}
