using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Options for low-level keyboard hotkey capture.
/// </summary>
public sealed class LowLevelKeyboardHotkeyOptions {
    /// <summary>
    /// Gets or sets whether the hook should suppress modifier keys while a registered chord is being formed.
    /// </summary>
    public bool SuppressPotentialChordKeys { get; set; }

    /// <summary>
    /// Gets process names where exclusive suppression is allowed. Empty means all foreground processes.
    /// </summary>
    public IReadOnlyList<string> ExclusiveForegroundProcessNames { get; set; } = Array.Empty<string>();
}
