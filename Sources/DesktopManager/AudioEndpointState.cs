namespace DesktopManager;

/// <summary>
/// Specifies Windows audio endpoint states.
/// </summary>
[Flags]
public enum AudioEndpointState : uint {
    /// <summary>No endpoint states.</summary>
    None = 0,
    /// <summary>The endpoint is active.</summary>
    Active = 1,
    /// <summary>The endpoint is disabled.</summary>
    Disabled = 2,
    /// <summary>The endpoint is not present.</summary>
    NotPresent = 4,
    /// <summary>The endpoint is unplugged.</summary>
    Unplugged = 8,
    /// <summary>All endpoint states.</summary>
    All = Active | Disabled | NotPresent | Unplugged
}
