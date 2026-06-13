using System.Text.Json;
using DesktopManager.App.Core;

namespace DesktopManager.App;

internal static class HotkeyDiagnosticsWriter {
    private static readonly object WriteLock = new();
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented = false
    };

    public static string Write(HotkeyExecutionDiagnostic diagnostic) {
        return WriteJsonLine(diagnostic);
    }

    public static string WriteRuntimeEvent(
        string eventName,
        HotkeyFunctionDefinition? function = null,
        string? message = null,
        object? details = null) {
        return WriteJsonLine(new HotkeyRuntimeDiagnostic {
            EventName = eventName,
            FunctionName = function?.Name ?? string.Empty,
            Hotkey = function?.Hotkey ?? string.Empty,
            Placement = function?.WindowAction.Placement ?? string.Empty,
            Target = function?.WindowAction.Target ?? string.Empty,
            Monitor = function?.WindowAction.Monitor ?? string.Empty,
            MonitorIndex = function?.WindowAction.MonitorIndex,
            Message = message,
            Details = details
        });
    }

    private static string WriteJsonLine(object value) {
        string directory = GetDiagnosticDirectory();
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, $"hotkeys-{DateTimeOffset.Now:yyyyMMdd}.jsonl");
        string json = JsonSerializer.Serialize(value, SerializerOptions);
        lock (WriteLock) {
            File.AppendAllText(path, json + Environment.NewLine);
        }

        return path;
    }

    private static string GetDiagnosticDirectory() {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Evotec", "DesktopManager", "Diagnostics");
    }
}
