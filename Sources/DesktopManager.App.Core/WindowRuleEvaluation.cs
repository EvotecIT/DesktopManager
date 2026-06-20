namespace DesktopManager.App.Core;

/// <summary>
/// Result of evaluating saved layout rules for one window.
/// </summary>
public sealed class WindowRuleEvaluation {
    /// <summary>
    /// Gets or sets whether a matching rule was found.
    /// </summary>
    public bool Matched { get; set; }

    /// <summary>
    /// Gets or sets the layout that contained the matching rule.
    /// </summary>
    public WindowLayoutProfileDefinition? Layout { get; set; }

    /// <summary>
    /// Gets or sets the matching rule.
    /// </summary>
    public WindowRuleDefinition? Rule { get; set; }

    /// <summary>
    /// Gets or sets the placement request generated for the rule.
    /// </summary>
    public global::DesktopManager.WindowPlacementRequest? Request { get; set; }
}
