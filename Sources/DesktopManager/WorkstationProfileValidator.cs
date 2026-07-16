using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Validates persisted workstation profiles before capture data reaches Windows mutation APIs.
/// </summary>
internal static class WorkstationProfileValidator {
    public static void Validate(WorkstationProfile profile) {
        if (profile == null) {
            throw new ArgumentNullException(nameof(profile));
        }
        if (profile.SchemaVersion != 1) {
            throw new NotSupportedException($"Workstation profile schema {profile.SchemaVersion} is not supported.");
        }
        if (profile.Monitors == null || profile.AudioEndpoints == null || profile.Personalization == null || profile.Taskbars == null) {
            throw new InvalidOperationException("Workstation profile sections cannot be null.");
        }
        if (profile.Personalization.Monitors == null || profile.Personalization.Policy == null || profile.Personalization.User == null) {
            throw new InvalidOperationException("Workstation personalization sections cannot be null.");
        }

        var monitorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (WorkstationMonitorProfile monitor in profile.Monitors) {
            if (monitor == null || string.IsNullOrWhiteSpace(monitor.StableKey)) {
                throw new InvalidOperationException("Every workstation monitor requires a stable key.");
            }
            if (!monitorKeys.Add(monitor.StableKey)) {
                throw new InvalidOperationException($"Workstation monitor key '{monitor.StableKey}' is duplicated.");
            }
            if (monitor.DisplayMode == null || monitor.DisplayMode.Width <= 0 || monitor.DisplayMode.Height <= 0 || monitor.DisplayMode.RefreshRate <= 0) {
                throw new InvalidOperationException($"Workstation monitor '{monitor.StableKey}' has an invalid display mode.");
            }
            if (!Enum.IsDefined(typeof(DisplayOrientation), monitor.DisplayMode.Orientation)) {
                throw new InvalidOperationException($"Workstation monitor '{monitor.StableKey}' has an invalid orientation.");
            }
            if (monitor.Brightness.HasValue && (monitor.Brightness.Value < 0 || monitor.Brightness.Value > 100)) {
                throw new InvalidOperationException($"Workstation monitor '{monitor.StableKey}' has brightness outside 0 through 100.");
            }
        }

        var endpointIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (WorkstationAudioEndpointProfile endpoint in profile.AudioEndpoints) {
            if (endpoint == null || string.IsNullOrWhiteSpace(endpoint.Id)) {
                throw new InvalidOperationException("Every workstation audio endpoint requires an ID.");
            }
            if (!endpointIds.Add(endpoint.Id)) {
                throw new InvalidOperationException($"Workstation audio endpoint '{endpoint.Id}' is duplicated.");
            }
            if (endpoint.VolumePercent.HasValue && (endpoint.VolumePercent.Value < 0 || endpoint.VolumePercent.Value > 100)) {
                throw new InvalidOperationException($"Workstation audio endpoint '{endpoint.Id}' has volume outside 0 through 100.");
            }
            if (endpoint.DefaultRoles == null) {
                throw new InvalidOperationException($"Workstation audio endpoint '{endpoint.Id}' has no default-role collection.");
            }
            if (!Enum.IsDefined(typeof(AudioDataFlow), endpoint.DataFlow) || endpoint.DataFlow == AudioDataFlow.All) {
                throw new InvalidOperationException($"Workstation audio endpoint '{endpoint.Id}' has an invalid data flow.");
            }
            var roles = new HashSet<AudioRole>();
            foreach (AudioRole role in endpoint.DefaultRoles) {
                if (!Enum.IsDefined(typeof(AudioRole), role) || !roles.Add(role)) {
                    throw new InvalidOperationException($"Workstation audio endpoint '{endpoint.Id}' has invalid or duplicated default roles.");
                }
            }
        }

        var taskbarKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (WorkstationTaskbarProfile taskbar in profile.Taskbars) {
            if (taskbar == null || string.IsNullOrWhiteSpace(taskbar.MonitorStableKey)) {
                throw new InvalidOperationException("Every workstation taskbar requires a monitor stable key.");
            }
            if (!monitorKeys.Contains(taskbar.MonitorStableKey)) {
                throw new InvalidOperationException($"Taskbar monitor '{taskbar.MonitorStableKey}' is not present in the profile.");
            }
            if (!taskbarKeys.Add(taskbar.MonitorStableKey)) {
                throw new InvalidOperationException($"Taskbar monitor '{taskbar.MonitorStableKey}' is duplicated.");
            }
            if (!Enum.IsDefined(typeof(TaskbarPosition), taskbar.Position)) {
                throw new InvalidOperationException($"Taskbar monitor '{taskbar.MonitorStableKey}' has an invalid position.");
            }
        }
    }
}
