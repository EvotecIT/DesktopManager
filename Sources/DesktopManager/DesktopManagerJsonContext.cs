using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopManager;

/// <summary>
/// Source-generated JSON metadata for DesktopManager's persisted state, so trimmed and Native AOT
/// consumers can read and write it without reflection.
/// </summary>
[JsonSerializable(typeof(DesktopWindowTargetDefinition))]
[JsonSerializable(typeof(DesktopControlTargetDefinition))]
[JsonSerializable(typeof(DesktopVisualBaselineDefinition))]
[JsonSerializable(typeof(PersonalizationSnapshot))]
[JsonSerializable(typeof(WindowLayout))]
[JsonSerializable(typeof(WorkspaceRecoverySettings))]
[JsonSerializable(typeof(WorkstationProfile))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(IEnumerable<string>))]
internal sealed partial class DesktopManagerJsonContext : JsonSerializerContext {
    /// <summary>Indented output with otherwise default options.</summary>
    internal static DesktopManagerJsonContext Indented { get; } = new(new JsonSerializerOptions {
        WriteIndented = true
    });

    /// <summary>Indented output with case-insensitive property matching, used by workstation profiles.</summary>
    internal static DesktopManagerJsonContext IndentedCaseInsensitive { get; } = new(new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    });
}
