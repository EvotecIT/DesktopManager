namespace DesktopManager;

/// <summary>
/// Represents one monitor inside a row-and-column desktop topology snapshot.
/// </summary>
public sealed class MonitorTopologyItem {
    internal MonitorTopologyItem(
        Monitor monitor,
        MonitorIdentity identity,
        int row,
        int column,
        string topologyName,
        string displayName) {
        Monitor = monitor;
        Identity = identity;
        Row = row;
        Column = column;
        TopologyName = topologyName;
        DisplayName = displayName;
    }

    /// <summary>
    /// Gets the underlying monitor snapshot.
    /// </summary>
    public Monitor Monitor { get; }

    /// <summary>
    /// Gets the stable monitor identity.
    /// </summary>
    public MonitorIdentity Identity { get; }

    /// <summary>
    /// Gets the zero-based row of this monitor in the current topology.
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// Gets the zero-based column of this monitor in its row.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets a human-friendly topology name such as Top Left or Bottom Right.
    /// </summary>
    public string TopologyName { get; }

    /// <summary>
    /// Gets a concise display name suitable for UI selectors and diagnostics.
    /// </summary>
    public string DisplayName { get; }
}
