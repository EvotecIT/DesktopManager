namespace DesktopManager.App.Core;

/// <summary>
/// Window metadata used when evaluating saved layout rules.
/// </summary>
public sealed class WindowRuleObservation {
    /// <summary>
    /// Gets or sets the native window handle.
    /// </summary>
    public IntPtr Handle { get; set; }

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the process name without extension.
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the executable path when available.
    /// </summary>
    public string ProcessPath { get; set; } = string.Empty;
}
