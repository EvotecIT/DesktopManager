namespace DesktopManager.Tests;

internal sealed class DesktopManagerWinUiHarnessStatus {
    public int ProcessId { get; set; }

    public string WindowTitle { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public string EditorText { get; set; } = string.Empty;

    public bool AutomationCheckBoxChecked { get; set; }

    public string SelectedOption { get; set; } = string.Empty;

    public string ActionStatus { get; set; } = string.Empty;
}
