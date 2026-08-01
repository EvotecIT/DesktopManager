namespace DesktopManager;

/// <summary>
/// Carries focused UI Automation metadata and its optional bounded text read.
/// </summary>
internal sealed class UiAutomationFocusedControlResult {
    public WindowControlInfo? Control { get; set; }
    public UiAutomationTextReadResult? Text { get; set; }
}
