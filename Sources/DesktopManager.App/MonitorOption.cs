namespace DesktopManager.App;

internal sealed class MonitorOption {
    public MonitorOption(int? index, string displayName) {
        Index = index;
        DisplayName = displayName;
    }

    public int? Index { get; }

    public string DisplayName { get; }
}
