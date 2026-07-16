using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace DesktopManager.Cli;

internal enum McpToolSafetyDecisionKind {
    Allow,
    Deny,
    DryRun
}

internal sealed class McpToolSafetyDecision {
    private McpToolSafetyDecision(McpToolSafetyDecisionKind kind, string? message = null, object? result = null) {
        Kind = kind;
        Message = message;
        Result = result;
    }

    public McpToolSafetyDecisionKind Kind { get; }
    public string? Message { get; }
    public object? Result { get; }

    public static McpToolSafetyDecision Allow() {
        return new McpToolSafetyDecision(McpToolSafetyDecisionKind.Allow);
    }

    public static McpToolSafetyDecision Deny(string message) {
        return new McpToolSafetyDecision(McpToolSafetyDecisionKind.Deny, message);
    }

    public static McpToolSafetyDecision DryRun(object result) {
        return new McpToolSafetyDecision(McpToolSafetyDecisionKind.DryRun, result: result);
    }
}

internal sealed class McpSafetyPolicy {
    public McpSafetyPolicy(
        bool allowMutations,
        bool allowForegroundInput,
        bool dryRun,
        IReadOnlyList<string>? allowedProcessPatterns = null,
        IReadOnlyList<string>? deniedProcessPatterns = null,
        bool allowSystemSettings = false,
        bool allowExperimental = false) {
        AllowMutations = allowMutations;
        AllowForegroundInput = allowForegroundInput;
        DryRun = dryRun;
        AllowSystemSettings = allowSystemSettings;
        AllowExperimental = allowExperimental;
        AllowedProcessPatterns = NormalizePatterns(allowedProcessPatterns);
        DeniedProcessPatterns = NormalizePatterns(deniedProcessPatterns);
    }

    public bool AllowMutations { get; }
    public bool AllowForegroundInput { get; }
    public bool DryRun { get; }
    public bool AllowSystemSettings { get; }
    public bool AllowExperimental { get; }
    public bool ReadOnly => !AllowMutations;
    public IReadOnlyList<string> AllowedProcessPatterns { get; }
    public IReadOnlyList<string> DeniedProcessPatterns { get; }
    private bool HasProcessFilters => AllowedProcessPatterns.Count > 0 || DeniedProcessPatterns.Count > 0;

    public object ToModel() {
        return new {
            readOnly = ReadOnly,
            allowMutations = AllowMutations,
            allowForegroundInput = AllowForegroundInput,
            allowSystemSettings = AllowSystemSettings,
            allowExperimental = AllowExperimental,
            dryRun = DryRun,
            allowedProcesses = AllowedProcessPatterns,
            deniedProcesses = DeniedProcessPatterns
        };
    }

    public string BuildInstructions() {
        string mutationMode = DryRun
            ? "Mutating MCP tools are running in dry-run mode and will return simulated results without changing the desktop."
            : AllowMutations
                ? "Mutating MCP tools are enabled for this session."
                : "This MCP server is read-only by default; restart with --allow-mutations to enable mutating tools.";

        string foregroundMode = AllowForegroundInput
            ? "Focused foreground input fallback is enabled for zero-handle UI Automation text and key actions."
            : "Focused foreground input fallback is blocked unless the server is started with --allow-foreground-input.";

        string processScopeMode = BuildProcessScopeInstructions();
        string systemSettingsMode = AllowSystemSettings
            ? "System-wide settings mutations are enabled."
            : "System-wide settings mutations require --allow-system-settings.";
        string experimentalMode = AllowExperimental
            ? "Experimental tools are enabled."
            : "Experimental tools require --allow-experimental.";

        return mutationMode + " " + foregroundMode + " " + systemSettingsMode + " " + experimentalMode + " " + processScopeMode + " Use read-only inspection tools before mutating the desktop. Layouts and snapshots are stored per-user. Snapshots currently store window layout state only.";
    }

    public McpToolSafetyDecision EvaluateToolCall(string name, JsonElement arguments) {
        if (!McpCatalog.IsKnownTool(name)) {
            return McpToolSafetyDecision.Allow();
        }

        if (McpCatalog.RequiresExperimentalAccess(name) && !AllowExperimental) {
            return McpToolSafetyDecision.Deny(
                $"The requested tool '{name}' uses an experimental Windows contract. Restart with --allow-experimental to expose it.");
        }

        if (!McpCatalog.IsMutatingTool(name)) {
            return McpToolSafetyDecision.Allow();
        }

        if (AllowMutations && McpCatalog.RequiresSystemSettingsAccess(name) && !AllowSystemSettings) {
            return McpToolSafetyDecision.Deny(
                $"The requested mutation '{name}' changes system-wide desktop state. Restart with --allow-system-settings in addition to --allow-mutations.");
        }

        if (HasProcessFilters && McpCatalog.AffectsGlobalDesktop(name)) {
            return McpToolSafetyDecision.Deny(
                $"The requested mutation '{name}' affects global desktop state and cannot be constrained by MCP process allow/deny filters. Restart without process filters to permit global desktop mutations.");
        }

        if (HasProcessFilters && McpCatalog.AffectsLiveDesktop(name)) {
            if (!McpCatalog.TryGetMutatingProcessScope(name, arguments, out string[] processPatterns, out string? scopeError)) {
                return McpToolSafetyDecision.Deny(scopeError ?? "This MCP server requires an explicit process-scoped target for the requested mutating tool.");
            }

            for (int index = 0; index < processPatterns.Length; index++) {
                string processPattern = processPatterns[index];
                if (MatchesAnyPattern(processPattern, DeniedProcessPatterns)) {
                    return McpToolSafetyDecision.Deny($"The requested mutation targets '{processPattern}', which is blocked by the MCP denied-process policy.");
                }
            }

            if (AllowedProcessPatterns.Count > 0 && !MatchesAnyPattern(processPatterns, AllowedProcessPatterns)) {
                string requested = string.Join(", ", processPatterns);
                return McpToolSafetyDecision.Deny($"The requested mutation targets '{requested}', which is outside the MCP allowed-process policy.");
            }
        }

        bool requestsForegroundInput = McpCatalog.RequestsForegroundInputFallback(name, arguments);
        if (requestsForegroundInput && !AllowForegroundInput && !DryRun) {
            return McpToolSafetyDecision.Deny("This MCP server blocks focused foreground input fallback. Restart with --allow-foreground-input to permit this request.");
        }

        if (DryRun) {
            return McpToolSafetyDecision.DryRun(new {
                success = true,
                applied = false,
                dryRun = true,
                toolName = name,
                requestedForegroundInputFallback = requestsForegroundInput,
                requestedProcesses = HasProcessFilters && McpCatalog.AffectsLiveDesktop(name) && McpCatalog.TryGetMutatingProcessScope(name, arguments, out string[] scopedProcesses, out _)
                    ? scopedProcesses
                    : Array.Empty<string>(),
                safetyMode = "dry-run",
                message = "Mutation skipped because the MCP server is running in dry-run mode.",
                policy = ToModel()
            });
        }

        if (!AllowMutations) {
            return McpToolSafetyDecision.Deny("This MCP server is running in read-only mode. Restart with --allow-mutations or use --dry-run to preview mutating requests safely.");
        }

        return McpToolSafetyDecision.Allow();
    }

    private string BuildProcessScopeInstructions() {
        if (!HasProcessFilters) {
            return "No process allow/deny filters are active for this session.";
        }

        string allowed = AllowedProcessPatterns.Count > 0
            ? "Allowed processes: " + string.Join(", ", AllowedProcessPatterns) + "."
            : "No explicit process allowlist is active.";
        string denied = DeniedProcessPatterns.Count > 0
            ? "Denied processes: " + string.Join(", ", DeniedProcessPatterns) + "."
            : "No explicit process denylist is active.";

        return allowed + " " + denied + " Process-scoped desktop mutations must resolve to an explicit process target, and global desktop mutations are blocked while these filters are active.";
    }

    private static string[] NormalizePatterns(IReadOnlyList<string>? patterns) {
        if (patterns == null || patterns.Count == 0) {
            return Array.Empty<string>();
        }

        var normalized = new List<string>();
        for (int index = 0; index < patterns.Count; index++) {
            string? pattern = patterns[index];
            if (string.IsNullOrWhiteSpace(pattern)) {
                continue;
            }

            normalized.Add(pattern.Trim());
        }

        return normalized.ToArray();
    }

    private static bool MatchesAnyPattern(string value, IReadOnlyList<string> patterns) {
        for (int index = 0; index < patterns.Count; index++) {
            if (MatchesPattern(value, patterns[index])) {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesAnyPattern(IReadOnlyList<string> values, IReadOnlyList<string> patterns) {
        for (int valueIndex = 0; valueIndex < values.Count; valueIndex++) {
            if (MatchesAnyPattern(values[valueIndex], patterns)) {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesPattern(string value, string pattern) {
        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
