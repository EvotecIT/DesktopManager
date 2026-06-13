using System.Collections.Generic;
using System;
using System.Linq;

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

        HashSet<string> enabledHotkeys = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < profile.Functions.Count; index++) {
            HotkeyFunctionDefinition function = profile.Functions[index];
            string prefix = $"Function {index + 1}";

            if (string.IsNullOrWhiteSpace(function.Id)) {
                result.Errors.Add($"{prefix} is missing an id.");
            }

            if (string.IsNullOrWhiteSpace(function.Name)) {
                result.Errors.Add($"{prefix} is missing a name.");
            }

            if (string.IsNullOrWhiteSpace(function.ActionType)) {
                result.Errors.Add($"{prefix} is missing an action type.");
            }

            if (function.Enabled) {
                if (string.IsNullOrWhiteSpace(function.Hotkey)) {
                    result.Errors.Add($"{prefix} is enabled but has no hotkey.");
                } else if (!enabledHotkeys.Add(NormalizeHotkey(function.Hotkey))) {
                    result.Errors.Add($"{prefix} uses duplicate hotkey '{function.Hotkey}'.");
                }
            }
        }

        return result;
    }

    private static string NormalizeHotkey(string hotkey) {
        bool control = false;
        bool alt = false;
        bool shift = false;
        bool win = false;
        var keys = new List<string>();
        foreach (string rawPart in hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            string part = rawPart.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
            switch (part) {
                case "ctrl":
                case "control":
                    control = true;
                    break;
                case "alt":
                    alt = true;
                    break;
                case "shift":
                    shift = true;
                    break;
                case "win":
                case "windows":
                    win = true;
                    break;
                default:
                    keys.Add(part);
                    break;
            }
        }

        var canonical = new List<string>();
        if (control) {
            canonical.Add("ctrl");
        }

        if (alt) {
            canonical.Add("alt");
        }

        if (shift) {
            canonical.Add("shift");
        }

        if (win) {
            canonical.Add("win");
        }

        canonical.AddRange(keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));
        return string.Join("+", canonical);
    }
}
