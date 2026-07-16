using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Captures and restores cohesive workstation state through the owning DesktopManager services.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WorkstationProfileService {
    private readonly MonitorService _monitors;
    private readonly PersonalizationService _personalization;
    private readonly AudioService _audio;
    private readonly TaskbarService _taskbars;

    /// <summary>Initializes a workstation profile service over the current interactive desktop.</summary>
    public WorkstationProfileService()
        : this(
            new MonitorService((IDesktopManager)new DesktopManagerWrapper()),
            new PersonalizationService(),
            new AudioService(),
            new TaskbarService()) {
    }

    internal WorkstationProfileService(
        MonitorService monitors,
        PersonalizationService personalization,
        AudioService audio,
        TaskbarService taskbars) {
        _monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));
        _personalization = personalization ?? throw new ArgumentNullException(nameof(personalization));
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _taskbars = taskbars ?? throw new ArgumentNullException(nameof(taskbars));
    }

    /// <summary>Captures connected display, personalization, taskbar, and active audio state.</summary>
    /// <returns>A complete workstation profile snapshot.</returns>
    public WorkstationProfile CaptureProfile() {
        List<Monitor> monitors = _monitors.GetMonitors()
            .Where(monitor => monitor.IsConnected)
            .ToList();
        IReadOnlyDictionary<Monitor, string> monitorStableKeys = WorkstationMonitorKeyResolver.Resolve(monitors);
        PersonalizationSnapshot personalization = _personalization.CaptureSnapshot();
        personalization.Monitors.Clear();
        var profile = new WorkstationProfile {
            CapturedAt = DateTimeOffset.UtcNow,
            Personalization = personalization,
            TaskbarAutoHide = _taskbars.GetTaskbarAutoHide()
        };

        foreach (Monitor monitor in monitors) {
            profile.Monitors.Add(new WorkstationMonitorProfile {
                StableKey = monitorStableKeys[monitor],
                DeviceId = monitor.DeviceId,
                DeviceName = monitor.DeviceName,
                IsPrimary = monitor.IsPrimary,
                Left = monitor.PositionLeft,
                Top = monitor.PositionTop,
                DisplayMode = _monitors.GetMonitorDisplayMode(monitor.DeviceId),
                Brightness = TryGetBrightness(monitor),
                HdrEnabled = TryGetHdrState(monitor),
                WallpaperPath = monitor.Wallpaper
            });
        }

        IReadOnlyDictionary<int, string> monitorKeys = monitors.ToDictionary(
            monitor => monitor.Index,
            monitor => monitorStableKeys[monitor]);
        foreach (TaskbarInfo taskbar in _taskbars.GetTaskbars()) {
            if (!monitorKeys.TryGetValue(taskbar.MonitorIndex, out string? stableKey)) {
                continue;
            }
            profile.Taskbars.Add(new WorkstationTaskbarProfile {
                MonitorStableKey = stableKey,
                IsVisible = taskbar.IsVisible,
                Position = taskbar.Position
            });
        }

        foreach (AudioEndpointInfo endpoint in _audio.GetEndpoints(states: AudioEndpointState.Active)) {
            profile.AudioEndpoints.Add(new WorkstationAudioEndpointProfile {
                Id = endpoint.Id,
                Name = endpoint.Name,
                DataFlow = endpoint.DataFlow,
                VolumePercent = endpoint.VolumePercent,
                IsMuted = endpoint.IsMuted,
                DefaultRoles = endpoint.DefaultRoles.ToList()
            });
        }

        return profile;
    }

    /// <summary>Captures and saves a named workstation profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <returns>The captured profile.</returns>
    public WorkstationProfile SaveProfile(string name) {
        WorkstationProfile profile = CaptureProfile();
        WorkstationProfileStore.Save(name, profile);
        return profile;
    }

    /// <summary>Loads and applies a named workstation profile.</summary>
    /// <param name="name">The stored profile name.</param>
    /// <param name="options">Optional section and rollback settings.</param>
    /// <returns>The application result.</returns>
    public WorkstationProfileApplyResult ApplyProfile(string name, WorkstationProfileApplyOptions? options = null) {
        return ApplyProfile(WorkstationProfileStore.Load(name), options);
    }

    /// <summary>Applies a workstation profile with monitor matching and optional rollback.</summary>
    /// <param name="profile">The profile to apply.</param>
    /// <param name="options">Optional section and rollback settings.</param>
    /// <returns>The application result.</returns>
    public WorkstationProfileApplyResult ApplyProfile(
        WorkstationProfile profile,
        WorkstationProfileApplyOptions? options = null) {
        WorkstationProfileValidator.Validate(profile);

        WorkstationProfileApplyOptions effectiveOptions = options ?? new WorkstationProfileApplyOptions();
        WorkstationProfile? rollback = null;
        if (effectiveOptions.RollbackOnFailure) {
            try {
                rollback = CaptureProfile();
            } catch (Exception ex) {
                return new WorkstationProfileApplyResult(
                    false,
                    false,
                    $"The pre-apply rollback snapshot could not be captured: {ex.Message}",
                    Array.Empty<string>());
            }
        }

        var warnings = new List<string>();
        try {
            ApplyCore(profile, effectiveOptions, warnings);
            return new WorkstationProfileApplyResult(true, false, null, warnings.ToArray());
        } catch (Exception ex) {
            bool rolledBack = false;
            string error = ex.Message;
            if (rollback != null) {
                try {
                    ApplyCore(rollback, CreateRollbackOptions(effectiveOptions), warnings);
                    rolledBack = true;
                } catch (Exception rollbackException) {
                    error += $" Rollback also failed: {rollbackException.Message}";
                }
            }

            return new WorkstationProfileApplyResult(false, rolledBack, error, warnings.ToArray());
        }
    }

    private void ApplyCore(
        WorkstationProfile profile,
        WorkstationProfileApplyOptions options,
        ICollection<string> warnings) {
        List<Monitor> currentMonitors = _monitors.GetMonitors()
            .Where(monitor => monitor.IsConnected)
            .ToList();
        IReadOnlyDictionary<string, Monitor> matches = MatchMonitors(profile.Monitors, currentMonitors, warnings);
        string[] missing = profile.Monitors
            .Where(saved => !matches.ContainsKey(saved.StableKey))
            .Select(saved => saved.StableKey)
            .ToArray();
        if (missing.Length > 0 && options.RequireAllMonitors && (options.ApplyDisplays || options.ApplyTaskbars)) {
            throw new InvalidOperationException("Required monitors are not connected: " + string.Join(", ", missing) + ".");
        }

        if (options.ApplyDisplays) {
            _monitors.ApplyDisplayProfile(profile.Monitors, matches);
        }
        if (options.ApplyPersonalization) {
            _personalization.Restore(profile.Personalization, options.ApplyMachinePolicies);
        }
        if (options.ApplyDisplays) {
            ApplyMonitorDetails(profile.Monitors, matches, warnings);
        }
        if (options.ApplyTaskbars) {
            ApplyTaskbars(profile, matches, warnings);
        }
        if (options.ApplyAudio) {
            ApplyAudio(profile.AudioEndpoints, warnings);
        }
    }

    private void ApplyMonitorDetails(
        IEnumerable<WorkstationMonitorProfile> profiles,
        IReadOnlyDictionary<string, Monitor> matches,
        ICollection<string> warnings) {
        foreach (WorkstationMonitorProfile profile in profiles) {
            if (!matches.TryGetValue(profile.StableKey, out Monitor? monitor)) {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(profile.WallpaperPath)) {
                _monitors.SetWallpaper(monitor.DeviceId, profile.WallpaperPath);
            }
            if (profile.HdrEnabled.HasValue) {
                try {
                    MonitorAdvancedColorInfo color = _monitors.GetMonitorAdvancedColor(monitor.DeviceId);
                    if (color.HdrSupported) {
                        _monitors.SetMonitorHdr(monitor.DeviceId, profile.HdrEnabled.Value);
                    } else {
                        warnings.Add($"Monitor '{profile.StableKey}' no longer reports HDR support.");
                    }
                } catch (Exception ex) {
                    warnings.Add($"HDR was not applied to '{profile.StableKey}': {ex.Message}");
                }
            }
            if (profile.Brightness.HasValue) {
                try {
                    _monitors.SetMonitorBrightness(monitor.DeviceName, profile.Brightness.Value);
                } catch (Exception ex) {
                    warnings.Add($"Brightness was not applied to '{profile.StableKey}': {ex.Message}");
                }
            }
        }
    }

    private void ApplyTaskbars(
        WorkstationProfile profile,
        IReadOnlyDictionary<string, Monitor> matches,
        ICollection<string> warnings) {
        if (_taskbars.GetTaskbarAutoHide() != profile.TaskbarAutoHide) {
            _taskbars.SetTaskbarAutoHide(profile.TaskbarAutoHide);
        }
        IReadOnlyDictionary<int, TaskbarInfo> currentTaskbars = _taskbars.GetTaskbars()
            .GroupBy(taskbar => taskbar.MonitorIndex)
            .ToDictionary(group => group.Key, group => group.First());
        foreach (WorkstationTaskbarProfile taskbar in profile.Taskbars) {
            if (!matches.TryGetValue(taskbar.MonitorStableKey, out Monitor? monitor)) {
                warnings.Add($"Taskbar monitor '{taskbar.MonitorStableKey}' is not connected.");
                continue;
            }

            if (!currentTaskbars.TryGetValue(monitor.Index, out TaskbarInfo? current)) {
                warnings.Add($"No taskbar is present on monitor '{taskbar.MonitorStableKey}'.");
                continue;
            }
            if (current.Position != taskbar.Position) {
                _taskbars.SetTaskbarPosition(monitor.Index, taskbar.Position);
            }
            if (current.IsVisible != taskbar.IsVisible) {
                _taskbars.SetTaskbarVisibility(monitor.Index, taskbar.IsVisible);
            }
        }
    }

    private void ApplyAudio(IEnumerable<WorkstationAudioEndpointProfile> profiles, ICollection<string> warnings) {
        IReadOnlyDictionary<string, AudioEndpointInfo> current = _audio
            .GetEndpoints(states: AudioEndpointState.Active)
            .ToDictionary(endpoint => endpoint.Id, StringComparer.OrdinalIgnoreCase);
        foreach (WorkstationAudioEndpointProfile profile in profiles) {
            if (!current.ContainsKey(profile.Id)) {
                warnings.Add($"Audio endpoint '{profile.Name}' is not active.");
                continue;
            }

            try {
                if (profile.VolumePercent.HasValue) {
                    _audio.SetEndpointVolume(profile.Id, profile.VolumePercent.Value);
                }
                if (profile.IsMuted.HasValue) {
                    _audio.SetEndpointMute(profile.Id, profile.IsMuted.Value);
                }
                if (profile.DefaultRoles.Count > 0) {
                    _audio.SetDefaultAudioDevice(profile.Id, profile.DefaultRoles.ToArray());
                }
            } catch (Exception ex) {
                warnings.Add($"Audio endpoint '{profile.Name}' was only partially applied: {ex.Message}");
            }
        }
    }

    internal static IReadOnlyDictionary<string, Monitor> MatchMonitors(
        IEnumerable<WorkstationMonitorProfile> savedMonitors,
        IEnumerable<Monitor> currentMonitors,
        ICollection<string> warnings) {
        var available = currentMonitors.ToList();
        IReadOnlyDictionary<Monitor, string> currentKeys = WorkstationMonitorKeyResolver.Resolve(available);
        var matches = new Dictionary<string, Monitor>(StringComparer.OrdinalIgnoreCase);
        foreach (WorkstationMonitorProfile saved in savedMonitors) {
            Monitor? match = available.FirstOrDefault(current =>
                string.Equals(currentKeys[current], saved.StableKey, StringComparison.OrdinalIgnoreCase));
            string? fallback = null;
            if (match == null && !string.IsNullOrWhiteSpace(saved.DeviceId)) {
                Monitor[] deviceIdMatches = available.Where(current =>
                    string.Equals(current.DeviceId, saved.DeviceId, StringComparison.OrdinalIgnoreCase)).ToArray();
                match = deviceIdMatches.Length == 1 ? deviceIdMatches[0] : null;
                fallback = match == null ? null : "device ID";
            }
            if (match == null && !string.IsNullOrWhiteSpace(saved.DeviceName)) {
                Monitor[] deviceNameMatches = available.Where(current =>
                    string.Equals(current.DeviceName, saved.DeviceName, StringComparison.OrdinalIgnoreCase)).ToArray();
                match = deviceNameMatches.Length == 1 ? deviceNameMatches[0] : null;
                fallback = match == null ? null : "display source";
            }
            if (match == null) {
                warnings.Add($"Monitor '{saved.StableKey}' is not connected.");
                continue;
            }

            matches[saved.StableKey] = match;
            available.Remove(match);
            if (fallback != null) {
                warnings.Add($"Monitor '{saved.StableKey}' was matched by {fallback} because its stable identity changed.");
            }
        }
        return matches;
    }

    private int? TryGetBrightness(Monitor monitor) {
        try {
            return _monitors.GetMonitorBrightness(monitor.DeviceName);
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"Brightness was not captured for '{monitor.DeviceName}': {ex.Message}");
            return null;
        }
    }

    private bool? TryGetHdrState(Monitor monitor) {
        try {
            MonitorAdvancedColorInfo color = _monitors.GetMonitorAdvancedColor(monitor.DeviceId);
            return color.HdrSupported ? color.HdrEnabled : (bool?)null;
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"HDR was not captured for '{monitor.DeviceName}': {ex.Message}");
            return null;
        }
    }

    private static WorkstationProfileApplyOptions CreateRollbackOptions(WorkstationProfileApplyOptions source) {
        return new WorkstationProfileApplyOptions {
            RequireAllMonitors = false,
            ApplyDisplays = source.ApplyDisplays,
            ApplyAudio = source.ApplyAudio,
            ApplyPersonalization = source.ApplyPersonalization,
            ApplyMachinePolicies = source.ApplyMachinePolicies,
            ApplyTaskbars = source.ApplyTaskbars,
            RollbackOnFailure = false
        };
    }
}
