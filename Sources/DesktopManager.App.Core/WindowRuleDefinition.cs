namespace DesktopManager.App.Core;

/// <summary>
/// Places matching windows according to a saved workstation rule.
/// </summary>
public sealed class WindowRuleDefinition {
    /// <summary>
    /// Gets or sets whether this rule should be evaluated.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the stable rule identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the display name shown in the app.
    /// </summary>
    public string Name { get; set; } = "Window rule";

    /// <summary>
    /// Gets or sets the window matching criteria.
    /// </summary>
    public WindowRuleMatchDefinition Match { get; set; } = new();

    /// <summary>
    /// Gets or sets the placement to apply when a window matches.
    /// </summary>
    public WindowHotkeyActionDefinition Action { get; set; } = new();
}
