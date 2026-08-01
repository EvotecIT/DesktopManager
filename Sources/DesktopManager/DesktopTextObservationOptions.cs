namespace DesktopManager;

/// <summary>
/// Configures text observation against windows and controls.
/// </summary>
public sealed class DesktopTextObservationOptions {
    /// <summary>
    /// Largest supported observation length. This keeps provider and native buffer requests bounded.
    /// </summary>
    public const int MaximumTextLength = 1_048_576;

    /// <summary>
    /// Gets or sets the maximum number of characters returned in the observed value.
    /// </summary>
    public int MaxObservedTextLength { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the number of observation retries.
    /// </summary>
    public int RetryCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the delay between retries in milliseconds.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 0;
}
