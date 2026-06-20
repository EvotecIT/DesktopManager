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

        if (!IsKnownHotkeyBackend(profile.HotkeyBackend)) {
            result.Errors.Add($"Profile has invalid hotkey backend '{profile.HotkeyBackend}'.");
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
            } else if (!IsKnownActionType(function.ActionType)) {
                result.Errors.Add($"{prefix} has invalid action type '{function.ActionType}'.");
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

        ValidateLayouts(profile.Layouts, result);
        return result;
    }

    private static void ValidateLayouts(List<WindowLayoutProfileDefinition>? layouts, HotkeyProfileValidationResult result) {
        if (layouts == null) {
            result.Errors.Add("Profile layouts list is required.");
            return;
        }

        HashSet<string> layoutIds = new(StringComparer.OrdinalIgnoreCase);
        for (int layoutIndex = 0; layoutIndex < layouts.Count; layoutIndex++) {
            WindowLayoutProfileDefinition? layout = layouts[layoutIndex];
            string layoutPrefix = $"Layout {layoutIndex + 1}";
            if (layout == null) {
                result.Errors.Add($"{layoutPrefix} is empty.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(layout.Id)) {
                result.Errors.Add($"{layoutPrefix} is missing an id.");
            } else if (!layoutIds.Add(layout.Id)) {
                result.Errors.Add($"{layoutPrefix} uses duplicate id '{layout.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(layout.Name)) {
                result.Errors.Add($"{layoutPrefix} is missing a name.");
            }

            if (layout.Rules == null) {
                result.Errors.Add($"{layoutPrefix} rules list is required.");
                continue;
            }

            HashSet<string> ruleIds = new(StringComparer.OrdinalIgnoreCase);
            for (int ruleIndex = 0; ruleIndex < layout.Rules.Count; ruleIndex++) {
                ValidateRule(layout.Rules[ruleIndex], $"{layoutPrefix} rule {ruleIndex + 1}", ruleIds, result);
            }
        }
    }

    private static void ValidateRule(
        WindowRuleDefinition? rule,
        string prefix,
        HashSet<string> ruleIds,
        HotkeyProfileValidationResult result) {
        if (rule == null) {
            result.Errors.Add($"{prefix} is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(rule.Id)) {
            result.Errors.Add($"{prefix} is missing an id.");
        } else if (!ruleIds.Add(rule.Id)) {
            result.Errors.Add($"{prefix} uses duplicate id '{rule.Id}'.");
        }

        if (string.IsNullOrWhiteSpace(rule.Name)) {
            result.Errors.Add($"{prefix} is missing a name.");
        }

        if (rule.Match == null) {
            result.Errors.Add($"{prefix} is missing match criteria.");
        }

        if (rule.Action == null) {
            result.Errors.Add($"{prefix} is missing an action.");
        } else {
            ValidateWindowAction(rule.Action, prefix, result);
        }
    }

    private static void ValidateWindowAction(WindowHotkeyActionDefinition action, string prefix, HotkeyProfileValidationResult result) {
        if (!IsKnownWindowTarget(action.Target)) {
            result.Errors.Add($"{prefix} has invalid window target '{action.Target}'.");
        }

        if (!IsKnownMonitorTarget(action.Monitor)) {
            result.Errors.Add($"{prefix} has invalid monitor target '{action.Monitor}'.");
        }

        if (!IsKnownPlacement(action.Placement)) {
            result.Errors.Add($"{prefix} has invalid placement '{action.Placement}'.");
        } else if (string.Equals(action.Placement, WindowPlacements.ExactRectangle, StringComparison.OrdinalIgnoreCase) &&
            !HasCompleteExactRectangle(action)) {
            result.Errors.Add($"{prefix} has incomplete exact rectangle geometry.");
        }
    }

    private static bool IsKnownHotkeyBackend(string backend) {
        return string.Equals(backend, HotkeyBackendKinds.RegisterHotKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(backend, HotkeyBackendKinds.LowLevelKeyboardHook, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(backend, HotkeyBackendKinds.NativeHotkeyHost, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKnownActionType(string actionType) {
        return string.Equals(actionType, HotkeyActionKinds.ManageWindow, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKnownWindowTarget(string target) {
        return string.Equals(target, WindowTargets.ActiveWindow, StringComparison.OrdinalIgnoreCase);
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

    private static bool HasCompleteExactRectangle(WindowHotkeyActionDefinition action) {
        return action.ExactLeft.HasValue &&
            action.ExactTop.HasValue &&
            action.ExactWidth.GetValueOrDefault() > 0 &&
            action.ExactHeight.GetValueOrDefault() > 0;
    }
}
