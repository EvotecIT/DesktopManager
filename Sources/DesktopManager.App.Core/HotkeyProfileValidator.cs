using System.Collections.Generic;
using System;

namespace DesktopManager.App.Core;

/// <summary>
/// Validates profile fields that affect safe hotkey registration.
/// </summary>
public static class HotkeyProfileValidator {
    /// <summary>
    /// Validates a profile for missing fields and duplicate enabled hotkeys.
    /// </summary>
    /// <param name="profile">Profile to validate.</param>
    /// <returns>Validation result with actionable errors.</returns>
    public static HotkeyProfileValidationResult Validate(HotkeyProfile profile) {
        HotkeyProfileValidationResult result = new();
        if (profile == null) {
            result.Errors.Add("Profile is required.");
            return result;
        }

        if (profile.Functions == null) {
            result.Errors.Add("Profile functions list is required.");
            return result;
        }

        HashSet<string> enabledHotkeys = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < profile.Functions.Count; index++) {
            HotkeyFunctionDefinition? function = profile.Functions[index];
            string prefix = $"Function {index + 1}";
            if (function == null) {
                result.Errors.Add($"{prefix} is empty.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(function.Id)) {
                result.Errors.Add($"{prefix} is missing an id.");
            }

            if (string.IsNullOrWhiteSpace(function.Name)) {
                result.Errors.Add($"{prefix} is missing a name.");
            }

            if (string.IsNullOrWhiteSpace(function.ActionType)) {
                result.Errors.Add($"{prefix} is missing an action type.");
            }

            if (function.WindowAction == null) {
                result.Errors.Add($"{prefix} is missing a window action.");
            } else {
                ValidateWindowAction(function.WindowAction, prefix, result);
            }

            if (function.Enabled) {
                if (string.IsNullOrWhiteSpace(function.Hotkey)) {
                    result.Errors.Add($"{prefix} is enabled but has no hotkey.");
                } else if (!HotkeyGestureParser.TryNormalize(function.Hotkey, out string normalizedHotkey, out string hotkeyError)) {
                    result.Errors.Add($"{prefix} has invalid hotkey '{function.Hotkey}': {hotkeyError}");
                } else if (!enabledHotkeys.Add(normalizedHotkey)) {
                    result.Errors.Add($"{prefix} uses duplicate hotkey '{function.Hotkey}'.");
                }
            }
        }

        return result;
    }

    private static void ValidateWindowAction(WindowHotkeyActionDefinition action, string prefix, HotkeyProfileValidationResult result) {
        if (!IsKnownMonitorTarget(action.Monitor)) {
            result.Errors.Add($"{prefix} has invalid monitor target '{action.Monitor}'.");
        }

        if (!IsKnownPlacement(action.Placement)) {
            result.Errors.Add($"{prefix} has invalid placement '{action.Placement}'.");
        }
    }

    private static bool IsKnownMonitorTarget(string monitor) {
        return string.Equals(monitor, MonitorTargets.Current, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(monitor, MonitorTargets.TopLeft, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(monitor, MonitorTargets.TopRight, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(monitor, MonitorTargets.BottomLeft, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(monitor, MonitorTargets.BottomRight, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKnownPlacement(string placement) {
        return string.Equals(placement, WindowPlacements.Restore, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(placement, WindowPlacements.LeftHalf, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(placement, WindowPlacements.RightHalf, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(placement, WindowPlacements.Maximize, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(placement, WindowPlacements.ExactRectangle, StringComparison.OrdinalIgnoreCase);
    }
}
