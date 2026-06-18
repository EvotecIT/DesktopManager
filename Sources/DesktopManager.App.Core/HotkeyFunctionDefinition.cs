namespace DesktopManager.App.Core;

/// <summary>
/// Describes one user-visible hotkey function.
/// </summary>
public sealed class HotkeyFunctionDefinition {
    /// <summary>Stable identifier for the function.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name shown in the hotkey manager.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Display group for organizing function lists.</summary>
    public string Category { get; set; } = "Custom Functions";

    /// <summary>Whether this function should be registered when hotkeys are enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>User-facing gesture such as Ctrl+Alt+Shift+5.</summary>
    public string Hotkey { get; set; } = string.Empty;

    /// <summary>Action type identifier.</summary>
    public string ActionType { get; set; } = HotkeyActionKinds.ManageWindow;

    /// <summary>Window-management details for ManageWindow actions.</summary>
    public WindowHotkeyActionDefinition WindowAction { get; set; } = new();
}
