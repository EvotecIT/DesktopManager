namespace DesktopManager.App;

internal sealed class MonitorOption {
    public MonitorOption(int? index, string displayName, string? stableKey = null, string? topologyName = null) {
        Index = index;
        DisplayName = displayName;
        StableKey = stableKey;
        TopologyName = topologyName;
    }

    public int? Index { get; }

    public string DisplayName { get; }

    public string? StableKey { get; }

    public string? TopologyName { get; }
}
