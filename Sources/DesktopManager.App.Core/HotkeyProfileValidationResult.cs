using System.Collections.Generic;

namespace DesktopManager.App.Core;

/// <summary>
/// Validation result for a hotkey profile.
/// </summary>
public sealed class HotkeyProfileValidationResult {
    /// <summary>Validation messages that should be fixed before registration.</summary>
    public List<string> Errors { get; } = new();

    /// <summary>Whether the profile can be used for registration.</summary>
    public bool IsValid => Errors.Count == 0;
}
