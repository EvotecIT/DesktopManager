namespace DesktopManager.App.Core;

/// <summary>
/// Summarizes the latest diagnostic evidence for one hotkey function.
/// </summary>
public sealed class HotkeyDiagnosticSummary {
    /// <summary>
    /// Gets or sets whether matching diagnostic evidence was found.
    /// </summary>
    public bool Found { get; set; }

    /// <summary>
    /// Gets or sets the diagnostic timestamp when available.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the diagnostic event name.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the one-line operator summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional detail suitable for app display.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path that contained the diagnostic line.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}
