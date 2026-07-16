using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

/// <summary>
/// Keeps the most stable monitor identity while disambiguating duplicate EDID or device values.
/// </summary>
internal static class WorkstationMonitorKeyResolver {
    public static IReadOnlyDictionary<Monitor, string> Resolve(IEnumerable<Monitor> monitors) {
        Monitor[] items = monitors.ToArray();
        var result = new Dictionary<Monitor, string>();
        foreach (IGrouping<string, Monitor> group in items.GroupBy(
            monitor => MonitorIdentity.FromMonitor(monitor).StableKey,
            StringComparer.OrdinalIgnoreCase)) {
            Monitor[] duplicates = group.ToArray();
            if (duplicates.Length == 1) {
                result[duplicates[0]] = group.Key;
                continue;
            }

            foreach (Monitor monitor in duplicates) {
                result[monitor] = group.Key + "|" + GetUniqueSuffix(monitor, duplicates);
            }
        }
        return result;
    }

    private static string GetUniqueSuffix(Monitor monitor, IReadOnlyList<Monitor> group) {
        if (IsUnique(monitor.DeviceId, group.Select(candidate => candidate.DeviceId))) {
            return "device-id:" + Normalize(monitor.DeviceId);
        }
        if (IsUnique(monitor.DeviceName, group.Select(candidate => candidate.DeviceName))) {
            return "device-name:" + Normalize(monitor.DeviceName);
        }
        if (IsUnique(monitor.DeviceKey, group.Select(candidate => candidate.DeviceKey))) {
            return "device-key:" + Normalize(monitor.DeviceKey);
        }
        return "index:" + monitor.Index;
    }

    private static bool IsUnique(string value, IEnumerable<string> values) {
        return !string.IsNullOrWhiteSpace(value) &&
            values.Count(candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)) == 1;
    }

    private static string Normalize(string value) {
        return value.Trim().ToUpperInvariant();
    }
}
