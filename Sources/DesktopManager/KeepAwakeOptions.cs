namespace DesktopManager;

/// <summary>
/// Specifies which Windows idle behaviors a keep-awake lease should prevent.
/// </summary>
[Flags]
public enum KeepAwakeOptions {
    /// <summary>Prevents automatic system sleep.</summary>
    System = 1,
    /// <summary>Prevents the display from automatically turning off.</summary>
    Display = 2,
    /// <summary>Allows away mode for media and background workloads.</summary>
    AwayMode = 4
}
