using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DesktopManager.Tests;

internal sealed class McpTestClient : IDisposable {
    private const int MaxMessageBytes = 16 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly Process _process;

    private McpTestClient(Process process) {
        _process = process;
    }

    public static McpTestClient Start(string arguments = "mcp serve") {
        string executablePath = FindCliExecutablePath();
        var startInfo = new ProcessStartInfo(executablePath, arguments) {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process? process = Process.Start(startInfo);
        if (process == null) {
            throw new InvalidOperationException("Failed to start the DesktopManager CLI MCP server.");
        }

        return new McpTestClient(process);
    }

    public JsonElement SendRequest(int id, string method, object? parameters = null) {
        JsonDocument response = SendRequestCore(id, method, parameters);
        try {
            JsonElement root = response.RootElement;
            Assert.AreEqual("2.0", root.GetProperty("jsonrpc").GetString());
            Assert.AreEqual(id, root.GetProperty("id").GetInt32());
            if (root.TryGetProperty("error", out JsonElement error)) {
                Assert.Fail("MCP request failed: " + error.GetProperty("message").GetString());
            }

            return root.GetProperty("result").Clone();
        } finally {
            response.Dispose();
        }
    }

    public JsonElement SendRequestExpectError(int id, string method, object? parameters = null) {
        JsonDocument response = SendRequestCore(id, method, parameters);
        try {
            JsonElement root = response.RootElement;
            Assert.AreEqual("2.0", root.GetProperty("jsonrpc").GetString());
            Assert.AreEqual(id, root.GetProperty("id").GetInt32());
            Assert.IsTrue(root.TryGetProperty("error", out JsonElement error), "Expected the MCP request to return an error.");
            return error.Clone();
        } finally {
            response.Dispose();
        }
    }

    public JsonDocument SendRaw(string json) {
        byte[] payload = StrictUtf8.GetBytes(json);
        _process.StandardInput.BaseStream.Write(payload, 0, payload.Length);
        _process.StandardInput.BaseStream.WriteByte((byte)'\n');
        _process.StandardInput.BaseStream.Flush();
        return ReadResponse(_process.StandardOutput.BaseStream);
    }

    public JsonElement CallTool(int id, string name, object arguments) {
        JsonElement result = CallToolResponse(id, name, arguments);
        if (result.TryGetProperty("isError", out JsonElement isErrorElement) && isErrorElement.GetBoolean()) {
            string message = result.TryGetProperty("message", out JsonElement messageElement)
                ? messageElement.GetString() ?? "Tool call failed."
                : "Tool call failed.";
            Assert.Fail(message);
        }

        return result.GetProperty("structuredContent").Clone();
    }

    public JsonElement CallToolExpectError(int id, string name, object arguments) {
        JsonElement result = CallToolResponse(id, name, arguments);
        Assert.IsTrue(result.TryGetProperty("isError", out JsonElement isErrorElement) && isErrorElement.GetBoolean(), "Expected the tool call to return an MCP tool error.");
        return result.Clone();
    }

    private JsonDocument SendRequestCore(int id, string method, object? parameters = null) {
        var request = parameters == null
            ? new Dictionary<string, object?> {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method
            }
            : new Dictionary<string, object?> {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method,
                ["params"] = parameters
            };

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(request);

        _process.StandardInput.BaseStream.Write(payload, 0, payload.Length);
        _process.StandardInput.BaseStream.WriteByte((byte)'\n');
        _process.StandardInput.BaseStream.Flush();

        return ReadResponse(_process.StandardOutput.BaseStream);
    }

    private JsonElement CallToolResponse(int id, string name, object arguments) {
        JsonElement result = SendRequest(id, "tools/call", new Dictionary<string, object?> {
            ["name"] = name,
            ["arguments"] = arguments
        });
        return result.Clone();
    }

    public void Dispose() {
        try {
            if (!_process.HasExited) {
                _process.StandardInput.Close();
            }
        } catch {
            // Ignore shutdown failures while disposing the helper process.
        }

        try {
            if (!_process.WaitForExit(2000)) {
                _process.Kill();
                _process.WaitForExit(2000);
            }
        } catch {
            // Ignore cleanup failures in test teardown.
        }

        _process.Dispose();
    }

    private static JsonDocument ReadResponse(Stream stream) {
        var payload = new List<byte>();
        while (true) {
            int value = stream.ReadByte();
            if (value == -1) {
                throw new EndOfStreamException("Unexpected end of stream before the MCP newline delimiter.");
            }

            if (value == '\n') {
                break;
            }

            payload.Add((byte)value);
            if (payload.Count > MaxMessageBytes) {
                throw new InvalidDataException($"MCP response exceeds the {MaxMessageBytes}-byte limit.");
            }
        }

        if (payload.Count > 0 && payload[payload.Count - 1] == '\r') {
            payload.RemoveAt(payload.Count - 1);
        }

        return JsonDocument.Parse(StrictUtf8.GetString(payload.ToArray()));
    }

    private static string FindCliExecutablePath() {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null) {
            DirectoryInfo cliDebugDirectory = new(Path.Combine(current.FullName, "Sources", "DesktopManager.Cli", "bin", "Debug"));
            if (cliDebugDirectory.Exists) {
                FileInfo? debugCandidate = cliDebugDirectory
                    .EnumerateDirectories("net8.0-windows*")
                    .OrderByDescending(directory => directory.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(directory => new FileInfo(Path.Combine(directory.FullName, "DesktopManager.Cli.exe")))
                    .FirstOrDefault(file => file.Exists);
                if (debugCandidate != null) {
                    return debugCandidate.FullName;
                }
            }

            DirectoryInfo cliReleaseDirectory = new(Path.Combine(current.FullName, "Sources", "DesktopManager.Cli", "bin", "Release"));
            if (cliReleaseDirectory.Exists) {
                FileInfo? releaseCandidate = cliReleaseDirectory
                    .EnumerateDirectories("net8.0-windows*")
                    .OrderByDescending(directory => directory.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(directory => new FileInfo(Path.Combine(directory.FullName, "DesktopManager.Cli.exe")))
                    .FirstOrDefault(file => file.Exists);
                if (releaseCandidate != null) {
                    return releaseCandidate.FullName;
                }
            }

            current = current.Parent;
        }

        Assert.Inconclusive("DesktopManager.Cli.exe was not found. Build the CLI project before running MCP transport tests.");
        return string.Empty;
    }
}
