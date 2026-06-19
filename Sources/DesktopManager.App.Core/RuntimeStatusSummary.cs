namespace DesktopManager.App.Core;

/// <summary>
/// Formats concise runtime state for tray tooltip and status surfaces.
/// </summary>
public static class RuntimeStatusSummary {
    /// <summary>
    /// Creates a compact status line for the tray icon.
    /// </summary>
    /// <param name="profileEnabled">Whether hotkeys are enabled in the profile.</param>
    /// <param name="registeredHotkeys">Number of currently registered hotkeys.</param>
    /// <param name="layoutCount">Number of configured layout profiles.</param>
    /// <param name="ruleCount">Number of configured layout rules.</param>
    /// <param name="profileName">Display profile name.</param>
    /// <returns>A short status string suitable for NOTIFYICONDATA tooltip limits.</returns>
    public static string FormatTrayTooltip(
        bool profileEnabled,
        int registeredHotkeys,
        int layoutCount,
        int ruleCount,
        string? profileName) {
        string state = profileEnabled
            ? $"{registeredHotkeys} hotkey(s)"
            : "hotkeys disabled";
        string rules = ruleCount > 0
            ? $"{ruleCount} rule(s)"
            : "no rules";
        string profile = string.IsNullOrWhiteSpace(profileName)
            ? "DesktopManager"
            : profileName.Trim();

        string value = $"DesktopManager - {profile}: {state}, {rules}";
        return value.Length <= 120 ? value : value.Substring(0, 117) + "...";
    }
}
