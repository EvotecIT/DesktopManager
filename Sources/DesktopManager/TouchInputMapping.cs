using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>
/// Reads which displays Windows maps touch and pen digitizers to, as configured in Tablet PC Settings.
/// </summary>
/// <remarks>
/// When a touch panel is mapped to another display, touches on it land on that display instead.
/// Windows' own touch setup (Control Panel, Tablet PC Settings, Setup) changes the mapping.
/// </remarks>
[SupportedOSPlatform("windows")]
public static class TouchInputMapping {
    private const string MappingKey = @"SOFTWARE\Microsoft\Wisp\Pen\Digimon";

    /// <summary>
    /// Gets the device interface paths of the displays that touch or pen digitizers are mapped to.
    /// </summary>
    /// <returns>
    /// Distinct monitor device paths, comparable with <see cref="Monitor.DeviceId"/> ignoring case;
    /// empty when Windows holds no explicit mapping.
    /// </returns>
    public static IReadOnlyList<string> GetMappedMonitorDevicePaths() {
        // A 32-bit process sees a redirected HKLM\SOFTWARE, so both views are read; duplicates fold together.
        var values = new List<object?>();
        foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 }) {
            using RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using RegistryKey? key = root.OpenSubKey(MappingKey);
            if (key == null) {
                continue;
            }
            foreach (string name in key.GetValueNames()) {
                values.Add(key.GetValue(name));
            }
        }

        return ParseMonitorDevicePaths(values);
    }

    /// <summary>Keeps the non-empty string values, each digitizer's display, once per display.</summary>
    internal static IReadOnlyList<string> ParseMonitorDevicePaths(IEnumerable<object?> values) {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paths = new List<string>();
        foreach (object? value in values) {
            string path = (value as string)?.Trim() ?? string.Empty;
            if (path.Length > 0 && seen.Add(path)) {
                paths.Add(path);
            }
        }

        return paths;
    }
}
