using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Configures the out-of-process hotkey host used for foreground-sensitive shortcut capture.
/// </summary>
public sealed class ExternalHotkeyHostOptions {
    /// <summary>Optional full path to the helper executable.</summary>
    public string? HelperPath { get; set; }

    /// <summary>
    /// Legacy request to consume required modifiers while a registered chord is in progress.
    /// The helper keeps standalone modifiers visible to the foreground app and consumes only matched shortcut keys.
    /// </summary>
    public bool SuppressPotentialChordKeys { get; set; }

    /// <summary>Foreground process names retained for compatibility with legacy suppression requests.</summary>
    public IReadOnlyList<string> ExclusiveForegroundProcessNames { get; set; } = Array.Empty<string>();

    /// <summary>Milliseconds to wait for the helper process to report readiness.</summary>
    public int StartupTimeoutMilliseconds { get; set; } = 5000;
}
