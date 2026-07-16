using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DesktopManager.Cli;

internal sealed class McpServer {
    private const string ProtocolVersion = "2025-06-18";
    private const int MaxMessageBytes = 16 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly JsonSerializerOptions McpJsonOptions = new(JsonUtilities.SerializerOptions) {
        WriteIndented = false
    };
    private readonly McpSafetyPolicy _safetyPolicy;

    public McpServer(McpSafetyPolicy? safetyPolicy = null) {
        _safetyPolicy = safetyPolicy ?? new McpSafetyPolicy(allowMutations: false, allowForegroundInput: false, dryRun: false);
    }

    public int Run() {
        using Stream input = Console.OpenStandardInput();
        using Stream output = Console.OpenStandardOutput();

        while (true) {
            string? payload;
            try {
                payload = ReadMessage(input);
            } catch (Exception ex) when (ex is InvalidDataException || ex is EndOfStreamException) {
                WriteError(output, null, -32700, ex.Message);
                return 1;
            }

            if (payload == null) {
                return 0;
            }

            JsonDocument document;
            try {
                document = JsonDocument.Parse(payload);
            } catch (JsonException ex) {
                WriteError(output, null, -32700, "Invalid JSON payload: " + ex.Message);
                continue;
            }

            using (document) {
                JsonElement root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Array) {
                    WriteError(output, null, -32600, "MCP protocol 2025-06-18 does not support JSON-RPC batches.");
                } else {
                    ProcessMessageSafely(root, output);
                }
            }
        }
    }

    private void ProcessMessageSafely(JsonElement message, Stream output) {
        JsonElement? id = TryGetRequestId(message);
        try {
            ProcessMessage(message, output);
        } catch (Exception ex) {
            if (id.HasValue) {
                WriteError(output, id, -32603, "MCP request failed: " + ex.Message);
            }
        }
    }

    private void ProcessMessage(JsonElement message, Stream output) {
        JsonElement? requestId = TryGetRequestId(message);
        if (message.ValueKind != JsonValueKind.Object) {
            WriteError(output, requestId, -32600, "Each JSON-RPC request must be an object.");
            return;
        }

        if (!message.TryGetProperty("jsonrpc", out JsonElement versionElement) ||
            versionElement.ValueKind != JsonValueKind.String ||
            !string.Equals(versionElement.GetString(), "2.0", StringComparison.Ordinal)) {
            WriteError(output, requestId, -32600, "JSON-RPC version must be '2.0'.");
            return;
        }

        if (!message.TryGetProperty("method", out JsonElement methodElement) || methodElement.ValueKind != JsonValueKind.String) {
            WriteError(output, requestId, -32600, "JSON-RPC requests require a string method name.");
            return;
        }

        string? method = methodElement.GetString();
        bool hasId = message.TryGetProperty("id", out JsonElement idElement);
        JsonElement parameters = message.TryGetProperty("params", out JsonElement paramsElement) ? paramsElement : default;

        if (!hasId && !string.Equals(method, "notifications/initialized", StringComparison.Ordinal)) {
            return;
        }

        switch (method) {
            case "initialize":
                WriteSuccess(output, idElement, new {
                    protocolVersion = ProtocolVersion,
                    capabilities = new {
                        prompts = new { listChanged = false },
                        resources = new { subscribe = false, listChanged = false },
                        tools = new { listChanged = false }
                    },
                    serverInfo = new {
                        name = "DesktopManager.Cli",
                        version = "0.1.0"
                    },
                    safetyPolicy = _safetyPolicy.ToModel(),
                    instructions = _safetyPolicy.BuildInstructions()
                });
                return;
            case "notifications/initialized":
                return;
            case "ping":
                WriteSuccess(output, idElement, new { });
                return;
            case "tools/list":
                WriteSuccess(output, idElement, new {
                    tools = McpCatalog.GetTools(_safetyPolicy.AllowExperimental)
                });
                return;
            case "tools/call":
                HandleToolCall(output, idElement, parameters);
                return;
            case "resources/list":
                WriteSuccess(output, idElement, new {
                    resources = McpCatalog.GetResources()
                });
                return;
            case "resources/templates/list":
                WriteSuccess(output, idElement, new {
                    resourceTemplates = Array.Empty<object>()
                });
                return;
            case "resources/read":
                HandleResourceRead(output, idElement, parameters);
                return;
            case "prompts/list":
                WriteSuccess(output, idElement, new {
                    prompts = McpCatalog.GetPrompts()
                });
                return;
            case "prompts/get":
                HandlePromptGet(output, idElement, parameters);
                return;
            default:
                if (hasId) {
                    WriteError(output, idElement, -32601, $"Method '{method}' is not supported.");
                }
                return;
        }
    }

    private void HandleToolCall(Stream output, JsonElement id, JsonElement parameters) {
        if (parameters.ValueKind != JsonValueKind.Object) {
            WriteError(output, id, -32602, "Tool calls require an object params value.");
            return;
        }

        if (!parameters.TryGetProperty("name", out JsonElement nameElement)) {
            WriteError(output, id, -32602, "Tool calls require a tool name.");
            return;
        }

        string name = nameElement.GetString() ?? string.Empty;
        JsonElement arguments = parameters.TryGetProperty("arguments", out JsonElement argsElement) ? argsElement : default;
        McpToolSafetyDecision safetyDecision = _safetyPolicy.EvaluateToolCall(name, arguments);
        if (safetyDecision.Kind == McpToolSafetyDecisionKind.Deny) {
            object deniedResult = new {
                error = safetyDecision.Message ?? "The requested tool call is blocked by the active MCP safety policy."
            };
            WriteSuccess(output, id, new {
                content = new[] {
                    new {
                        type = "text",
                        text = JsonUtilities.Serialize(deniedResult)
                    }
                },
                structuredContent = deniedResult,
                isError = true,
                message = safetyDecision.Message
            });
            return;
        }

        if (safetyDecision.Kind == McpToolSafetyDecisionKind.DryRun) {
            object dryRunResult = safetyDecision.Result ?? new {
                dryRun = true,
                message = "Mutation skipped because the MCP server is running in dry-run mode."
            };
            WriteSuccess(output, id, new {
                content = new[] {
                    new {
                        type = "text",
                        text = JsonUtilities.Serialize(dryRunResult)
                    }
                },
                structuredContent = dryRunResult,
                isError = false
            });
            return;
        }

        bool success = McpCatalog.TryCallTool(name, arguments, out object result, out string? error);
        WriteSuccess(output, id, new {
            content = new[] {
                new {
                    type = "text",
                    text = JsonUtilities.Serialize(result)
                }
            },
            structuredContent = result,
            isError = !success,
            message = error
        });
    }

    private void HandleResourceRead(Stream output, JsonElement id, JsonElement parameters) {
        if (parameters.ValueKind != JsonValueKind.Object) {
            WriteError(output, id, -32602, "Resource reads require an object params value.");
            return;
        }

        if (!parameters.TryGetProperty("uri", out JsonElement uriElement)) {
            WriteError(output, id, -32602, "Resource reads require a uri.");
            return;
        }

        try {
            string uri = uriElement.GetString() ?? string.Empty;
            object content = McpCatalog.ReadResource(uri);
            WriteSuccess(output, id, new {
                contents = new[] {
                    new {
                        uri,
                        mimeType = "application/json",
                        text = JsonUtilities.Serialize(content)
                    }
                }
            });
        } catch (CommandLineException ex) {
            WriteError(output, id, -32602, ex.Message);
        }
    }

    private void HandlePromptGet(Stream output, JsonElement id, JsonElement parameters) {
        if (parameters.ValueKind != JsonValueKind.Object) {
            WriteError(output, id, -32602, "Prompt requests require an object params value.");
            return;
        }

        if (!parameters.TryGetProperty("name", out JsonElement nameElement)) {
            WriteError(output, id, -32602, "Prompt requests require a name.");
            return;
        }

        JsonElement arguments = parameters.TryGetProperty("arguments", out JsonElement argsElement) ? argsElement : default;
        try {
            WriteSuccess(output, id, McpCatalog.GetPrompt(nameElement.GetString() ?? string.Empty, arguments));
        } catch (CommandLineException ex) {
            WriteError(output, id, -32602, ex.Message);
        }
    }

    private static string? ReadMessage(Stream input) {
        var payload = new List<byte>();
        while (true) {
            int value = input.ReadByte();
            if (value == -1) {
                return payload.Count == 0 ? null : throw new EndOfStreamException("Unexpected end of stream before the MCP newline delimiter.");
            }

            if (value == '\n') {
                break;
            }

            payload.Add((byte)value);
            if (payload.Count > MaxMessageBytes) {
                throw new InvalidDataException($"MCP message exceeds the {MaxMessageBytes}-byte limit.");
            }
        }

        if (payload.Count > 0 && payload[payload.Count - 1] == '\r') {
            payload.RemoveAt(payload.Count - 1);
        }

        try {
            return StrictUtf8.GetString(payload.ToArray());
        } catch (DecoderFallbackException ex) {
            throw new InvalidDataException("MCP messages must contain valid UTF-8.", ex);
        }
    }

    private static void WriteSuccess(Stream output, JsonElement id, object result) {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new {
            jsonrpc = "2.0",
            id = JsonSerializer.Deserialize<object>(id.GetRawText(), McpJsonOptions),
            result
        }, McpJsonOptions);
        WriteMessage(output, payload);
    }

    private static void WriteError(Stream output, JsonElement? id, int code, string message) {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new {
            jsonrpc = "2.0",
            id = id.HasValue
                ? JsonSerializer.Deserialize<object>(id.Value.GetRawText(), McpJsonOptions)
                : null,
            error = new {
                code,
                message
            }
        }, McpJsonOptions);
        WriteMessage(output, payload);
    }

    private static void WriteMessage(Stream output, byte[] payload) {
        output.Write(payload, 0, payload.Length);
        output.WriteByte((byte)'\n');
        output.Flush();
    }

    private static JsonElement? TryGetRequestId(JsonElement message) {
        if (message.ValueKind == JsonValueKind.Object && message.TryGetProperty("id", out JsonElement id)) {
            return id;
        }

        return null;
    }

}
