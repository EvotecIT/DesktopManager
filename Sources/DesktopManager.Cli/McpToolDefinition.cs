using System.Text.Json.Serialization;

namespace DesktopManager.Cli;

/// <summary>
/// Describes one MCP tool and its safety annotations. The catalog uses this model as the
/// single source of truth for advertised metadata and mutation classification.
/// </summary>
internal sealed class McpToolDefinition {
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("inputSchema")]
    public required object InputSchema { get; init; }

    [JsonPropertyName("annotations")]
    public required McpToolAnnotations Annotations { get; init; }
}

/// <summary>
/// Carries the MCP safety hints used both by clients and the server-side safety policy.
/// </summary>
internal sealed class McpToolAnnotations {
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("readOnlyHint")]
    public bool ReadOnlyHint { get; init; }

    [JsonPropertyName("destructiveHint")]
    public bool DestructiveHint { get; init; }

    [JsonPropertyName("idempotentHint")]
    public bool IdempotentHint { get; init; }

    [JsonPropertyName("openWorldHint")]
    public bool OpenWorldHint { get; init; }
}
