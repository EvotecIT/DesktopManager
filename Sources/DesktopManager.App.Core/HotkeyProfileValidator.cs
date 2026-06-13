using System.Collections.Generic;

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
        return hotkey.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
