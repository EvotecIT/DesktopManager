using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Options for low-level keyboard hotkey capture.
/// </summary>
public sealed class LowLevelKeyboardHotkeyOptions {
    /// <summary>
    /// Gets or sets a legacy request to suppress modifier keys while a registered chord is being formed.
    /// The hook keeps standalone modifier keys visible to the foreground app and consumes only matched shortcut keys.
    /// </summary>
    public bool SuppressPotentialChordKeys { get; set; }

    /// <summary>
    /// Gets process names retained for compatibility with legacy suppression requests.
    /// </summary>
    public IReadOnlyList<string> ExclusiveForegroundProcessNames { get; set; } = Array.Empty<string>();
}
