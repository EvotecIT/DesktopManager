using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DesktopManager.Tests;

internal sealed class McpTestClient : IDisposable {
    private static readonly byte[] HeaderSeparator = Encoding.ASCII.GetBytes("\r\n\r\n");
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
        byte[] header = Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n");

        _process.StandardInput.BaseStream.Write(header, 0, header.Length);
        _process.StandardInput.BaseStream.Write(payload, 0, payload.Length);
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
        var headerBuffer = new List<byte>();
        while (true) {
            int value = stream.ReadByte();
            if (value == -1) {
                throw new EndOfStreamException("Unexpected end of stream while reading MCP headers.");
            }

            headerBuffer.Add((byte)value);
            if (EndsWith(headerBuffer, HeaderSeparator)) {
                break;
            }
        }

        string headers = Encoding.ASCII.GetString(headerBuffer.ToArray());
        int contentLength = 0;
        foreach (string line in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)) {
            if (!line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            string valueText = line.Substring("Content-Length:".Length).Trim();
            if (!int.TryParse(valueText, out contentLength) || contentLength < 0) {
                throw new InvalidDataException("Invalid Content-Length header.");
            }
        }

        if (contentLength <= 0) {
            throw new InvalidDataException("Missing Content-Length header.");
        }

        byte[] payload = new byte[contentLength];
        int offset = 0;
        while (offset < payload.Length) {
            int read = stream.Read(payload, offset, payload.Length - offset);
            if (read <= 0) {
                throw new EndOfStreamException("Unexpected end of stream while reading an MCP message body.");
            }

            offset += read;
        }

        return JsonDocument.Parse(payload);
    }

    private static bool EndsWith(List<byte> source, byte[] suffix) {
        if (source.Count < suffix.Length) {
            return false;
        }

        for (int index = 0; index < suffix.Length; index++) {
            if (source[source.Count - suffix.Length + index] != suffix[index]) {
                return false;
            }
        }

        return true;
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
