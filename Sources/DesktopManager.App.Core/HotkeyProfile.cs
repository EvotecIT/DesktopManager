using System.Collections.Generic;

namespace DesktopManager.App.Core;

/// <summary>
/// Root configuration document for DesktopManager hotkey profiles.
/// </summary>
public sealed class HotkeyProfile {
    /// <summary>Schema version for migration decisions.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Whether the profile should register hotkeys when the host starts.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether the hotkey host should register itself for Windows sign-in startup.</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>Whether closing the window should keep the hotkey host running in the notification area.</summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>Hotkey registration backend. Use RegisterHotKey unless explicitly testing hooks.</summary>
    public string HotkeyBackend { get; set; } = HotkeyBackendKinds.RegisterHotKey;

    /// <summary>Foreground process names where low-level hook chords should be captured exclusively.</summary>
    public List<string> LowLevelHookExclusiveProcessNames { get; set; } = new();

    /// <summary>Friendly profile name shown in the tray application.</summary>
    public string ProfileName { get; set; } = "Workstation";

    /// <summary>Configured functions in registration order.</summary>
    public List<HotkeyFunctionDefinition> Functions { get; set; } = new();
}

/// <summary>
/// Supported hotkey registration backend names.
/// </summary>
public static class HotkeyBackendKinds {
    /// <summary>Use the standard Windows RegisterHotKey API.</summary>
    public const string RegisterHotKey = "RegisterHotKey";

    /// <summary>Use the experimental low-level keyboard hook backend.</summary>
    public const string LowLevelKeyboardHook = "LowLevelKeyboardHook";

    /// <summary>Use the out-of-process native hotkey host backend.</summary>
    public const string NativeHotkeyHost = "NativeHotkeyHost";
}
