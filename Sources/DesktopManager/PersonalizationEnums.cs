namespace DesktopManager;

/// <summary>
/// Defines Start layout preferences.
/// </summary>
public enum StartLayoutPreference {
    /// <summary>
    /// Default layout.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Prefer more pins.
    /// </summary>
    MorePins = 1,

    /// <summary>
    /// Prefer more recommendations.
    /// </summary>
    MoreRecommendations = 2
}

/// <summary>
/// Defines taskbar alignment preferences.
/// </summary>
public enum TaskbarAlignmentPreference {
    /// <summary>
    /// Left aligned taskbar.
    /// </summary>
    Left = 0,

    /// <summary>
    /// Center aligned taskbar.
    /// </summary>
    Center = 1
}

/// <summary>
/// Defines taskbar grouping preferences.
/// </summary>
public enum TaskbarGroupingPreference {
    /// <summary>
    /// Always group taskbar buttons.
    /// </summary>
    Always = 0,

    /// <summary>
    /// Group when taskbar is full.
    /// </summary>
    WhenFull = 1,

    /// <summary>
    /// Never group taskbar buttons.
    /// </summary>
    Never = 2
}
