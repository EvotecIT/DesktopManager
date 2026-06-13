using System.Text.Json.Serialization;

namespace DesktopManager.App.Core;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(HotkeyProfile))]
internal sealed partial class HotkeyProfileJsonContext : JsonSerializerContext {
}
