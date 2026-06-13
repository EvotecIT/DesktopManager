using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Configures the out-of-process hotkey host used for foreground-sensitive shortcut capture.
/// </summary>
public sealed class ExternalHotkeyHostOptions {
    /// <summary>Optional full path to the helper executable.</summary>
    public string? HelperPath { get; set; }

    /// <summary>Whether required modifier keys should be consumed while a registered chord is in progress.</summary>
    public bool SuppressPotentialChordKeys { get; set; }

    /// <summary>Foreground process names where modifier suppression is allowed.</summary>
    public IReadOnlyList<string> ExclusiveForegroundProcessNames { get; set; } = Array.Empty<string>();

    /// <summary>Milliseconds to wait for the helper process to report readiness.</summary>
    public int StartupTimeoutMilliseconds { get; set; } = 5000;
}
