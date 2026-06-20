using System.Text.Json;

namespace DesktopManager.App.Core;

/// <summary>
/// Parses hotkey runtime and execution JSONL diagnostics into concise operator summaries.
/// </summary>
public static class HotkeyDiagnosticLineParser {
    /// <summary>
    /// Attempts to parse one JSONL diagnostic line for the requested hotkey or function name.
    /// </summary>
    /// <param name="json">One JSON diagnostic line.</param>
    /// <param name="hotkey">The hotkey gesture to match.</param>
    /// <param name="functionName">The function name to match.</param>
    /// <param name="summary">The parsed summary when a matching line is found.</param>
    /// <returns>True when the line is valid and matches the requested function or hotkey.</returns>
    public static bool TryParse(
        string json,
        string? hotkey,
        string? functionName,
        out HotkeyDiagnosticSummary summary) {
        summary = new HotkeyDiagnosticSummary();
        if (string.IsNullOrWhiteSpace(json)) {
            return false;
        }

        try {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            string lineHotkey = GetString(root, "Hotkey");
            string lineFunction = GetString(root, "FunctionName");
            if (!Matches(lineHotkey, hotkey) && !Matches(lineFunction, functionName)) {
                return false;
            }

            string eventName = GetString(root, "EventName");
            summary = new HotkeyDiagnosticSummary {
                Found = true,
                Timestamp = GetTimestamp(root),
                EventName = string.IsNullOrWhiteSpace(eventName) ? "execution" : eventName
            };

            if (string.IsNullOrWhiteSpace(eventName)) {
                PopulateExecutionSummary(root, summary, lineFunction, lineHotkey);
            } else {
                PopulateRuntimeSummary(root, summary, lineFunction, lineHotkey, eventName);
            }

            return true;
        } catch (JsonException) {
            return false;
        }
    }

    private static void PopulateRuntimeSummary(
        JsonElement root,
        HotkeyDiagnosticSummary summary,
        string functionName,
        string hotkey,
        string eventName) {
        JsonElement details = TryGetObject(root, "Details");
        string source = GetString(details, "Source");
        string backend = GetString(details, "Backend");
        string message = GetString(root, "Message");
        string functionLabel = GetFunctionLabel(functionName, hotkey);

        summary.Summary = eventName switch {
            "registered" => $"{functionLabel} registered with {NonEmpty(backend, "unknown backend")}.",
            "registration-failed" or "low-level-registration-failed" => $"{functionLabel} registration failed: {NonEmpty(message, "no error captured")}.",
            "queued" => $"{functionLabel} queued from {NonEmpty(source, "unknown source")}.",
            "started" => $"{functionLabel} started from {NonEmpty(source, "unknown source")}.",
            "completed" => $"{functionLabel} completed from {NonEmpty(source, "unknown source")}.",
            "failed" => $"{functionLabel} failed from {NonEmpty(source, "unknown source")}: {NonEmpty(message, "no error captured")}.",
            "dropped" => $"{functionLabel} was dropped: {NonEmpty(message, "no reason captured")}.",
            _ => $"{functionLabel} event '{eventName}'."
        };

        string capturedHandle = NonEmpty(GetString(details, "CapturedHandle"), GetString(details, "WindowHandle"));
        string diagnosticPath = GetString(details, "DiagnosticPath");
        string verified = GetString(details, "Verified");
        string attempts = GetString(details, "Attempts");
        summary.Details = JoinDetails(
            Pair("source", source),
            Pair("backend", backend),
            Pair("handle", capturedHandle),
            Pair("verified", verified),
            Pair("attempts", attempts),
            Pair("diagnostic", diagnosticPath),
            Pair("message", message));
    }

    private static void PopulateExecutionSummary(
        JsonElement root,
        HotkeyDiagnosticSummary summary,
        string functionName,
        string hotkey) {
        string functionLabel = GetFunctionLabel(functionName, hotkey);
        string verified = GetString(root, "Verified");
        string attempt = GetString(root, "Attempt");
        string error = GetString(root, "Error");
        string resolvedHandle = GetString(root, "ResolvedHandle");

        summary.Summary = string.IsNullOrWhiteSpace(error)
            ? $"{functionLabel} execution diagnostic captured."
            : $"{functionLabel} execution failed: {error}.";
        summary.Details = JoinDetails(
            Pair("verified", verified),
            Pair("attempt", attempt),
            Pair("handle", resolvedHandle),
            Pair("error", error));
    }

    private static string GetFunctionLabel(string functionName, string hotkey) {
        if (!string.IsNullOrWhiteSpace(functionName) && !string.IsNullOrWhiteSpace(hotkey)) {
            return $"{functionName} ({hotkey})";
        }

        return NonEmpty(functionName, NonEmpty(hotkey, "Hotkey"));
    }

    private static string Pair(string name, string value) {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"{name}: {value}";
    }

    private static string JoinDetails(params string[] values) {
        return string.Join(" | ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string NonEmpty(string value, string fallback) {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static bool Matches(string value, string? expected) {
        return !string.IsNullOrWhiteSpace(value) &&
            !string.IsNullOrWhiteSpace(expected) &&
            string.Equals(value.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset? GetTimestamp(JsonElement root) {
        string value = GetString(root, "Timestamp");
        return DateTimeOffset.TryParse(value, out DateTimeOffset timestamp) ? timestamp : null;
    }

    private static JsonElement TryGetObject(JsonElement root, string propertyName) {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(propertyName, out JsonElement value) &&
            value.ValueKind == JsonValueKind.Object) {
            return value;
        }

        return default;
    }

    private static string GetString(JsonElement root, string propertyName) {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(propertyName, out JsonElement value)) {
            return string.Empty;
        }

        return value.ValueKind switch {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }
}
