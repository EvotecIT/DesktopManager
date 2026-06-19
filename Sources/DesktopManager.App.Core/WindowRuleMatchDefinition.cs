namespace DesktopManager.App.Core;

/// <summary>
/// Describes which windows a layout rule should match.
/// </summary>
public sealed class WindowRuleMatchDefinition {
    /// <summary>
    /// Gets or sets a wildcard pattern for the window title.
    /// </summary>
    public string TitlePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets a wildcard pattern for the process name without extension.
    /// </summary>
    public string ProcessNamePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets a wildcard pattern for the executable path.
    /// </summary>
    public string ProcessPathPattern { get; set; } = "*";
}
