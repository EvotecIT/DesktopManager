namespace DesktopManager.App;

internal sealed class WindowLayoutRuleExecutionResult {
    public int WindowsScanned { get; set; }
    public int Matches { get; set; }
    public int Applied { get; set; }
    public int Failed { get; set; }
    public List<string> Messages { get; } = new();

    public string ToStatusMessage() {
        return $"Layout rules scanned {WindowsScanned} window(s), matched {Matches}, applied {Applied}, failed {Failed}.";
    }
}
