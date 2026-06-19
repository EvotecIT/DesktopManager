using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

/// <summary>
/// Captures the connected monitor layout as stable identities plus row-and-column topology metadata.
/// </summary>
public sealed class MonitorTopologySnapshot {
    private MonitorTopologySnapshot(IReadOnlyList<MonitorTopologyItem> items) {
        Items = items;
    }

    /// <summary>
    /// Gets the monitors in visual row-major order.
    /// </summary>
    public IReadOnlyList<MonitorTopologyItem> Items { get; }

    /// <summary>
    /// Builds a topology snapshot from monitor snapshots.
    /// </summary>
    /// <param name="monitors">The monitors to map.</param>
    /// <param name="connectedOnly">Whether disconnected monitors should be excluded.</param>
    /// <returns>A topology snapshot ordered by visual rows and columns.</returns>
    public static MonitorTopologySnapshot FromMonitors(IEnumerable<Monitor> monitors, bool connectedOnly = true) {
        if (monitors == null) {
            throw new ArgumentNullException(nameof(monitors));
        }

        List<Monitor> candidates = monitors
            .Where(monitor => monitor != null)
            .Where(monitor => !connectedOnly || monitor.IsConnected)
            .ToList();

        IReadOnlyList<IReadOnlyList<Monitor>> rows = GroupRows(candidates);
        var items = new List<MonitorTopologyItem>();
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            IReadOnlyList<Monitor> row = rows[rowIndex];
            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++) {
                Monitor monitor = row[columnIndex];
                MonitorIdentity identity = MonitorIdentity.FromMonitor(monitor);
                string topologyName = GetTopologyName(rowIndex, columnIndex, rows.Count, row.Count);
                items.Add(new MonitorTopologyItem(
                    monitor,
                    identity,
                    rowIndex,
                    columnIndex,
                    topologyName,
                    CreateDisplayName(monitor, identity, topologyName)));
            }
        }

        return new MonitorTopologySnapshot(items.ToArray());
    }

    /// <summary>
    /// Groups monitors into visual rows using vertical overlap instead of exact Y-coordinate equality.
    /// </summary>
    /// <param name="monitors">The monitors to group.</param>
    /// <returns>Rows ordered top-to-bottom, with each row ordered left-to-right.</returns>
    public static IReadOnlyList<IReadOnlyList<Monitor>> GroupRows(IReadOnlyList<Monitor> monitors) {
        if (monitors == null) {
            throw new ArgumentNullException(nameof(monitors));
        }

        if (monitors.Count == 0) {
            return Array.Empty<IReadOnlyList<Monitor>>();
        }

        var rows = new List<List<Monitor>>();
        foreach (Monitor monitor in monitors.OrderBy(monitor => monitor.PositionTop).ThenBy(monitor => monitor.PositionLeft)) {
            List<Monitor>? row = rows.FirstOrDefault(candidateRow => candidateRow.Any(existing => VerticallyOverlaps(existing, monitor)));
            if (row == null) {
                row = new List<Monitor>();
                rows.Add(row);
            }

            row.Add(monitor);
        }

        return rows
            .Select(row => (IReadOnlyList<Monitor>)row.OrderBy(monitor => monitor.PositionLeft).ToArray())
            .OrderBy(row => row.Min(monitor => monitor.PositionTop))
            .ThenBy(row => row.Min(monitor => monitor.PositionLeft))
            .ToArray();
    }

    private static string CreateDisplayName(Monitor monitor, MonitorIdentity identity, string topologyName) {
        string deviceLabel = !string.IsNullOrWhiteSpace(monitor.DeviceString)
            ? monitor.DeviceString
            : monitor.DeviceName;

        if (string.IsNullOrWhiteSpace(deviceLabel)) {
            deviceLabel = $"Monitor {monitor.Index}";
        }

        return $"{topologyName}: {monitor.Index} - {deviceLabel} ({identity.Source})";
    }

    private static string GetTopologyName(int row, int column, int rowCount, int columnCount) {
        bool top = row == 0;
        bool bottom = row == rowCount - 1;
        bool left = column == 0;
        bool right = column == columnCount - 1;

        if (top && left && bottom && right) {
            return "Primary";
        }

        if (top && left) {
            return "Top Left";
        }

        if (top && right) {
            return "Top Right";
        }

        if (bottom && left) {
            return "Bottom Left";
        }

        if (bottom && right) {
            return "Bottom Right";
        }

        if (top) {
            return $"Top {column + 1}";
        }

        if (bottom) {
            return $"Bottom {column + 1}";
        }

        return $"Row {row + 1} Column {column + 1}";
    }

    private static bool VerticallyOverlaps(Monitor first, Monitor second) {
        int firstCenter = first.PositionTop + ((first.PositionBottom - first.PositionTop) / 2);
        int secondCenter = second.PositionTop + ((second.PositionBottom - second.PositionTop) / 2);

        return IsBetween(firstCenter, second.PositionTop, second.PositionBottom) ||
            IsBetween(secondCenter, first.PositionTop, first.PositionBottom);
    }

    private static bool IsBetween(int value, int start, int end) {
        return value >= start && value < end;
    }
}
