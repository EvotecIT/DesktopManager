using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DesktopManager;

/// <summary>
/// Commands sent from DesktopManager hosts to the external hotkey helper.
/// </summary>
public sealed class ExternalHotkeyHostCommand {
    /// <summary>Command type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Registration identifier owned by the client process.</summary>
    public int RegistrationId { get; set; }

    /// <summary>Hotkey modifier bitmask.</summary>
    public int Modifiers { get; set; }

    /// <summary>Virtual-key code.</summary>
    public int Key { get; set; }

    /// <summary>
    /// Legacy request to suppress in-progress chord modifiers. Standalone modifiers are preserved.
    /// </summary>
    public bool SuppressPotentialChordKeys { get; set; }

    /// <summary>Foreground process names retained for compatibility with legacy suppression requests.</summary>
    public List<string> ExclusiveForegroundProcessNames { get; set; } = new();
}

/// <summary>
/// Events sent from the external hotkey helper to DesktopManager hosts.
/// </summary>
public sealed class ExternalHotkeyHostEvent {
    /// <summary>Event type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Registration identifier owned by the client process.</summary>
    public int RegistrationId { get; set; }

    /// <summary>Foreground window handle captured by the hook.</summary>
    public long ForegroundWindowHandle { get; set; }

    /// <summary>Error or diagnostic message.</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Known external hotkey helper command names.
/// </summary>
public static class ExternalHotkeyHostCommandTypes {
    /// <summary>Registers a hotkey.</summary>
    public const string Register = "register";

    /// <summary>Unregisters a hotkey.</summary>
    public const string Unregister = "unregister";

    /// <summary>Requests helper shutdown.</summary>
    public const string Shutdown = "shutdown";
}

/// <summary>
/// Known external hotkey helper event names.
/// </summary>
public static class ExternalHotkeyHostEventTypes {
    /// <summary>The helper has installed its hook and is ready for commands.</summary>
    public const string Ready = "ready";

    /// <summary>A hotkey was registered.</summary>
    public const string Registered = "registered";

    /// <summary>A hotkey was unregistered.</summary>
    public const string Unregistered = "unregistered";

    /// <summary>A registered hotkey fired.</summary>
    public const string Triggered = "triggered";

    /// <summary>The helper could not process a command.</summary>
    public const string Error = "error";
}

/// <summary>
/// Source-generated JSON context for the external hotkey host protocol.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ExternalHotkeyHostCommand))]
[JsonSerializable(typeof(ExternalHotkeyHostEvent))]
public partial class ExternalHotkeyHostJsonContext : JsonSerializerContext {
}
