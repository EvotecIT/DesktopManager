namespace DesktopManager;

/// <summary>
/// Identifies a Core Audio endpoint notification.
/// </summary>
public enum AudioEndpointChangeKind {
    /// <summary>An endpoint was added.</summary>
    Added,
    /// <summary>An endpoint was removed.</summary>
    Removed,
    /// <summary>An endpoint changed device state.</summary>
    StateChanged,
    /// <summary>A default endpoint assignment changed.</summary>
    DefaultChanged,
    /// <summary>An endpoint property changed.</summary>
    PropertyChanged
}
