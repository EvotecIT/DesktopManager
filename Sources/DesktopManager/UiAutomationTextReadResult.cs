namespace DesktopManager;

/// <summary>
/// Carries a bounded text read from one UI Automation element.
/// </summary>
internal sealed class UiAutomationTextReadResult {
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsTruncated { get; set; }
    public bool? ContainsExpected { get; set; }
    public bool IsPassword { get; set; }
}
