namespace DesktopManager;

/// <summary>
/// Configures bounded semantic observation of controls.
/// </summary>
public sealed class DesktopControlObservationOptions {
    /// <summary>Gets or sets whether UI Automation semantic state should be requested.</summary>
    public bool UseUiAutomation { get; set; } = true;

    /// <summary>Gets or sets whether native control metadata and text may be used when UI Automation is unavailable.</summary>
    public bool IncludeNativeFallback { get; set; } = true;

    /// <summary>Gets or sets the maximum text returned from any single provider range.</summary>
    public int MaxTextLength { get; set; } = 4096;

    /// <summary>Gets or sets optional literal text to search for.</summary>
    public string? ExpectedText { get; set; }

    /// <summary>Gets or sets whether literal text search ignores case.</summary>
    public bool IgnoreCase { get; set; }

    /// <summary>Gets or sets the maximum number of prefix matches returned.</summary>
    public int MaxMatches { get; set; } = 20;

    /// <summary>Gets or sets the number of surrounding characters returned with each match.</summary>
    public int MatchContextLength { get; set; } = 40;

    /// <summary>Gets or sets whether selected text and caret context should be observed.</summary>
    public bool IncludeTextRanges { get; set; } = true;

    /// <summary>Gets or sets whether structured range, selection, scroll, grid, and table state should be observed.</summary>
    public bool IncludeSemanticState { get; set; } = true;

    /// <summary>Gets or sets whether a virtualized item may be realized before it is observed.</summary>
    public bool RealizeVirtualizedItem { get; set; }

    /// <summary>Gets or sets the maximum ancestor depth used to construct a selector path.</summary>
    public int MaxAncestorDepth { get; set; } = 12;
}
