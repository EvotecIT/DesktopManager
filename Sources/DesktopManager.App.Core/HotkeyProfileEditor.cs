namespace DesktopManager.App.Core;

/// <summary>
/// Small helpers for profile editing operations shared by app UI and tests.
/// </summary>
public static class HotkeyProfileEditor {
    /// <summary>
    /// Creates a disabled custom window action ready for user editing.
    /// </summary>
    /// <param name="existingFunctions">Existing functions used to select a unique id.</param>
    /// <returns>A new window-management function.</returns>
    public static HotkeyFunctionDefinition CreateCustomWindowAction(IEnumerable<HotkeyFunctionDefinition>? existingFunctions) {
        string id = CreateUniqueId("custom-window-action", existingFunctions?.Select(function => function.Id));
        return new HotkeyFunctionDefinition {
            Id = id,
            Name = "New Window Action",
            Category = "Custom Functions",
            Enabled = false,
            Hotkey = string.Empty,
            ActionType = HotkeyActionKinds.ManageWindow,
            WindowAction = new WindowHotkeyActionDefinition {
                Target = WindowTargets.ActiveWindow,
                Monitor = MonitorTargets.Current,
                Placement = WindowPlacements.Maximize,
                VerifyAfterAction = true
            }
        };
    }

    /// <summary>
    /// Creates a layout rule from a window action.
    /// </summary>
    /// <param name="function">Source function.</param>
    /// <param name="titlePattern">Window title wildcard pattern.</param>
    /// <param name="processNamePattern">Process name wildcard pattern.</param>
    /// <param name="existingRules">Existing rules used to select a unique id.</param>
    /// <returns>A new layout rule.</returns>
    public static WindowRuleDefinition CreateRuleFromFunction(
        HotkeyFunctionDefinition function,
        string? titlePattern,
        string? processNamePattern,
        IEnumerable<WindowRuleDefinition>? existingRules) {
        if (function == null) {
            throw new ArgumentNullException(nameof(function));
        }

        if (function.WindowAction == null) {
            throw new InvalidOperationException("Function does not have a window action.");
        }

        return new WindowRuleDefinition {
            Id = CreateUniqueId(NormalizeId(function.Id, "rule"), existingRules?.Select(rule => rule.Id)),
            Name = function.Name,
            Match = new WindowRuleMatchDefinition {
                TitlePattern = NormalizePattern(titlePattern),
                ProcessNamePattern = NormalizePattern(processNamePattern),
                ProcessPathPattern = "*"
            },
            Action = CloneWindowAction(function.WindowAction)
        };
    }

    /// <summary>
    /// Clones a window action so profile functions and rules do not share mutable references.
    /// </summary>
    /// <param name="action">Action to clone.</param>
    /// <returns>A copy of the action.</returns>
    public static WindowHotkeyActionDefinition CloneWindowAction(WindowHotkeyActionDefinition action) {
        if (action == null) {
            throw new ArgumentNullException(nameof(action));
        }

        return new WindowHotkeyActionDefinition {
            Target = action.Target,
            Monitor = action.Monitor,
            MonitorIndex = action.MonitorIndex,
            Placement = action.Placement,
            ExactLeft = action.ExactLeft,
            ExactTop = action.ExactTop,
            ExactWidth = action.ExactWidth,
            ExactHeight = action.ExactHeight,
            VerifyAfterAction = action.VerifyAfterAction
        };
    }

    /// <summary>
    /// Normalizes empty wildcard patterns to match all values.
    /// </summary>
    /// <param name="value">User-entered pattern.</param>
    /// <returns>A usable wildcard pattern.</returns>
    public static string NormalizePattern(string? value) {
        return string.IsNullOrWhiteSpace(value) ? "*" : value.Trim();
    }

    private static string CreateUniqueId(string baseId, IEnumerable<string>? existingIds) {
        HashSet<string> existing = existingIds == null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(existingIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);

        string normalized = NormalizeId(baseId, "item");
        if (!existing.Contains(normalized)) {
            return normalized;
        }

        int suffix = 2;
        while (existing.Contains($"{normalized}-{suffix}")) {
            suffix++;
        }

        return $"{normalized}-{suffix}";
    }

    private static string NormalizeId(string? value, string fallback) {
        string source = string.IsNullOrWhiteSpace(value) ? fallback : value;
        string normalized = new string(source!
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray()).Trim('-');

        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
