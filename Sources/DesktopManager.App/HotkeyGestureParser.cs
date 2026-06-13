using DesktopHotkeyModifiers = global::DesktopManager.HotkeyModifiers;
using DesktopVirtualKey = global::DesktopManager.VirtualKey;

namespace DesktopManager.App;

internal static class HotkeyGestureParser {
    public static bool TryParse(string gesture, out DesktopHotkeyModifiers modifiers, out DesktopVirtualKey key, out string error) {
        modifiers = DesktopHotkeyModifiers.None;
        key = default;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(gesture)) {
            error = "Hotkey is empty.";
            return false;
        }

        string[] parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) {
            error = $"Hotkey '{gesture}' is not valid.";
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

        string keyToken = NormalizeKey(parts[^1]);
        if (!Enum.TryParse(keyToken, ignoreCase: true, out key)) {
            error = $"Unknown hotkey key '{parts[^1]}'.";
            return false;
        }

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

        if (trimmed.Length > 1 && trimmed[0] == 'F' && trimmed.Skip(1).All(char.IsDigit)) {
            return "VK_" + trimmed.ToUpperInvariant();
        }

        return "VK_" + trimmed.ToUpperInvariant();
    }
}
