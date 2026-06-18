using DesktopHotkeyModifiers = global::DesktopManager.HotkeyModifiers;
using DesktopVirtualKey = global::DesktopManager.VirtualKey;

namespace DesktopManager.App.Core;

/// <summary>
/// Parses profile hotkey gestures into DesktopManager hotkey registration values.
/// </summary>
public static class HotkeyGestureParser {
    /// <summary>
    /// Parses a profile hotkey gesture.
    /// </summary>
    /// <param name="gesture">Gesture text such as Ctrl+Alt+Shift+1.</param>
    /// <param name="modifiers">Parsed modifier flags.</param>
    /// <param name="key">Parsed virtual key.</param>
    /// <param name="error">Actionable parse error when parsing fails.</param>
    /// <returns><c>true</c> when the gesture can be registered.</returns>
    public static bool TryParse(string gesture, out DesktopHotkeyModifiers modifiers, out DesktopVirtualKey key, out string error) {
        modifiers = DesktopHotkeyModifiers.None;
        key = default;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(gesture)) {
            error = "Hotkey is empty.";
            return false;
        }

        string[] parts = gesture.Split('+').Select(part => part.Trim()).ToArray();
        if (parts.Length == 0) {
            error = $"Hotkey '{gesture}' is not valid.";
            return false;
        }

        if (parts.Any(string.IsNullOrWhiteSpace)) {
            error = $"Hotkey '{gesture}' contains an empty token.";
            return false;
        }

        for (int index = 0; index < parts.Length - 1; index++) {
            string modifier = parts[index];
            if (string.Equals(modifier, "Ctrl", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(modifier, "Control", StringComparison.OrdinalIgnoreCase)) {
                modifiers |= DesktopHotkeyModifiers.Control;
            } else if (string.Equals(modifier, "Alt", StringComparison.OrdinalIgnoreCase)) {
                modifiers |= DesktopHotkeyModifiers.Alt;
            } else if (string.Equals(modifier, "Shift", StringComparison.OrdinalIgnoreCase)) {
                modifiers |= DesktopHotkeyModifiers.Shift;
            } else if (string.Equals(modifier, "Win", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(modifier, "Windows", StringComparison.OrdinalIgnoreCase)) {
                modifiers |= DesktopHotkeyModifiers.Win;
            } else if (string.Equals(modifier, "NoRepeat", StringComparison.OrdinalIgnoreCase)) {
                modifiers |= DesktopHotkeyModifiers.NoRepeat;
            } else {
                error = $"Unknown hotkey modifier '{modifier}'.";
                return false;
            }
        }

        string keyToken = NormalizeKey(parts[parts.Length - 1]);
        if (!Enum.TryParse(keyToken, ignoreCase: true, out key)) {
            error = $"Unknown hotkey key '{parts[parts.Length - 1]}'.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Parses and normalizes a profile hotkey gesture for duplicate detection.
    /// </summary>
    /// <param name="gesture">Gesture text.</param>
    /// <param name="normalizedGesture">Canonical gesture value when parsing succeeds.</param>
    /// <param name="error">Actionable parse error when parsing fails.</param>
    /// <returns><c>true</c> when the gesture can be normalized.</returns>
    public static bool TryNormalize(string gesture, out string normalizedGesture, out string error) {
        normalizedGesture = string.Empty;
        if (!TryParse(gesture, out DesktopHotkeyModifiers modifiers, out DesktopVirtualKey key, out error)) {
            return false;
        }

        var parts = new List<string>();
        if ((modifiers & DesktopHotkeyModifiers.Control) != 0) {
            parts.Add("ctrl");
        }

        if ((modifiers & DesktopHotkeyModifiers.Alt) != 0) {
            parts.Add("alt");
        }

        if ((modifiers & DesktopHotkeyModifiers.Shift) != 0) {
            parts.Add("shift");
        }

        if ((modifiers & DesktopHotkeyModifiers.Win) != 0) {
            parts.Add("win");
        }

        parts.Add(key.ToString().ToUpperInvariant());
        normalizedGesture = string.Join("+", parts);
        return true;
    }

    private static string NormalizeKey(string key) {
        string trimmed = key.Trim();
        if (trimmed.StartsWith("VK_", StringComparison.OrdinalIgnoreCase)) {
            return trimmed.ToUpperInvariant();
        }

        if (trimmed.Length == 1 && char.IsDigit(trimmed[0])) {
            return "VK_" + trimmed;
        }

        if (trimmed.Length == 1 && char.IsLetter(trimmed[0])) {
            return "VK_" + char.ToUpperInvariant(trimmed[0]);
        }

        if (trimmed.Length > 1 && char.ToUpperInvariant(trimmed[0]) == 'F' && trimmed.Skip(1).All(char.IsDigit)) {
            return "VK_" + trimmed.ToUpperInvariant();
        }

        return "VK_" + trimmed.ToUpperInvariant();
    }
}
