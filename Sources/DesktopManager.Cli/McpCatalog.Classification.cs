using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static partial class McpCatalog {
    private static readonly Lazy<McpToolDefinition[]> ToolDefinitions = new(CreateTools);
    private static readonly Lazy<IReadOnlyDictionary<string, McpToolDefinition>> ToolDefinitionsByName = new(() =>
        ToolDefinitions.Value.ToDictionary(tool => tool.Name, StringComparer.Ordinal));

    private static readonly HashSet<string> LiveDesktopMutationToolNames = new(StringComparer.Ordinal) {
        "click_control",
        "focus_control",
        "set_control_enabled",
        "set_control_check_state",
        "set_matching_control_check_state",
        "set_control_selected_value",
        "set_matching_control_selected_value",
        "set_control_visibility",
        "set_control_text",
        "send_control_keys",
        "move_window",
        "place_window",
        "click_window_point",
        "drag_window_points",
        "scroll_window_point",
        "type_window_text",
        "send_window_keys",
        "focus_window",
        "start_window_keep_alive",
        "stop_window_keep_alive",
        "minimize_windows",
        "maximize_windows",
        "restore_windows",
        "close_windows",
        "set_window_topmost",
        "set_window_visibility",
        "set_window_transparency",
        "snap_window",
        "save_visual_baseline",
        "launch_process",
        "launch_and_wait_for_window",
        "apply_named_layout",
        "restore_saved_snapshot",
        "prepare_for_coding",
        "prepare_for_screen_sharing",
        "clean_up_distractions",
        "move_window_to_virtual_desktop"
    };

    private static readonly HashSet<string> GlobalDesktopMutationToolNames = new(StringComparer.Ordinal) {
        "set_clipboard_text",
        "set_desktop_background_color",
        "set_desktop_wallpaper_position",
        "start_desktop_slideshow",
        "stop_desktop_slideshow",
        "advance_desktop_slideshow",
        "set_monitor_wallpaper",
        "set_monitor_brightness",
        "set_monitor_hdr",
        "set_monitor_position",
        "set_monitor_resolution",
        "set_taskbar_position",
        "configure_audio_endpoint",
        "invoke_system_action",
        "configure_keep_awake",
        "apply_personalization",
        "configure_taskbar",
        "apply_workstation_profile",
        "set_radio_state",
        "set_airplane_mode"
    };

    private static readonly HashSet<string> ForegroundInputFallbackToolNames = new(StringComparer.Ordinal) {
        "set_control_text",
        "send_control_keys"
    };

    public static bool IsKnownTool(string name) {
        return ToolDefinitionsByName.Value.ContainsKey(name);
    }

    public static bool IsMutatingTool(string name) {
        return ToolDefinitionsByName.Value.TryGetValue(name, out McpToolDefinition? tool) && !tool.Annotations.ReadOnlyHint;
    }

    public static bool AffectsLiveDesktop(string name) {
        return LiveDesktopMutationToolNames.Contains(name);
    }

    public static bool AffectsGlobalDesktop(string name) {
        return GlobalDesktopMutationToolNames.Contains(name);
    }

    public static bool RequestsForegroundInputFallback(string name, JsonElement arguments) {
        return ForegroundInputFallbackToolNames.Contains(name) && ReadBool(arguments, "allowForegroundInput");
    }

    public static bool TryGetMutatingProcessScope(string name, JsonElement arguments, out string[] processPatterns, out string? error) {
        processPatterns = Array.Empty<string>();
        error = null;

        switch (name) {
            case "launch_process":
            case "launch_and_wait_for_window":
                string? filePath = ReadOptionalString(arguments, "filePath");
                if (string.IsNullOrWhiteSpace(filePath)) {
                    error = "Process-scoped MCP safety filters require a non-empty 'filePath' for launch tools.";
                    return false;
                }

                processPatterns = ExtractProcessPatternsFromFilePath(filePath);
                return processPatterns.Length > 0;
            case "click_control":
            case "set_matching_control_check_state":
            case "set_matching_control_selected_value":
            case "save_visual_baseline":
            case "set_control_text":
            case "send_control_keys":
            case "move_window":
            case "place_window":
            case "click_window_point":
            case "drag_window_points":
            case "scroll_window_point":
            case "type_window_text":
            case "send_window_keys":
            case "focus_window":
            case "start_window_keep_alive":
            case "minimize_windows":
            case "maximize_windows":
            case "restore_windows":
            case "close_windows":
            case "set_window_topmost":
            case "set_window_visibility":
            case "set_window_transparency":
            case "snap_window":
                string? processName = ReadOptionalString(arguments, "processName");
                if (!string.IsNullOrWhiteSpace(processName) && !string.Equals(processName.Trim(), "*", StringComparison.Ordinal)) {
                    processPatterns = new[] { processName.Trim() };
                    return true;
                }

                error = "Process-scoped MCP safety filters require an explicit 'processName' selector for this tool.";
                return false;
            case "stop_window_keep_alive":
                if (ReadBool(arguments, "allSessions")) {
                    error = "This tool can affect multiple applications and is blocked while MCP process allow/deny filters are active.";
                    return false;
                }

                string? keepAliveProcessName = ReadOptionalString(arguments, "processName");
                if (!string.IsNullOrWhiteSpace(keepAliveProcessName) && !string.Equals(keepAliveProcessName.Trim(), "*", StringComparison.Ordinal)) {
                    processPatterns = new[] { keepAliveProcessName.Trim() };
                    return true;
                }

                error = "Process-scoped MCP safety filters require an explicit 'processName' selector for this tool.";
                return false;
            case "focus_control":
            case "set_control_enabled":
            case "set_control_check_state":
            case "set_control_selected_value":
            case "set_control_visibility":
                return TryResolveProcessPatternsFromWindowHandle(arguments, "windowHandle", out processPatterns, out error);
            case "move_window_to_virtual_desktop":
                return TryResolveProcessPatternsFromWindowHandle(arguments, "handle", out processPatterns, out error);
            case "apply_named_layout":
            case "restore_saved_snapshot":
            case "prepare_for_coding":
            case "prepare_for_screen_sharing":
            case "clean_up_distractions":
                error = "This tool can affect multiple applications and is blocked while MCP process allow/deny filters are active.";
                return false;
            default:
                return true;
        }
    }

    private static bool TryResolveProcessPatternsFromWindowHandle(JsonElement arguments, string propertyName, out string[] processPatterns, out string? error) {
        processPatterns = Array.Empty<string>();
        error = null;

        string? handleValue = ReadOptionalString(arguments, propertyName);
        if (string.IsNullOrWhiteSpace(handleValue)) {
            error = $"Process-scoped MCP safety filters require a non-empty '{propertyName}' handle for this tool.";
            return false;
        }

        try {
            IntPtr handle = DesktopHandleParser.Parse(handleValue);
            WindowInfo? window = new DesktopAutomationService().GetWindow(handle, includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
            if (window == null || window.ProcessId == 0) {
                error = "The target window could not be resolved to a running process.";
                return false;
            }

            using Process process = Process.GetProcessById((int)window.ProcessId);
            if (string.IsNullOrWhiteSpace(process.ProcessName)) {
                error = "The target window process name could not be resolved.";
                return false;
            }

            processPatterns = new[] { process.ProcessName };
            return true;
        } catch (Exception ex) {
            error = $"The target window process could not be resolved: {ex.Message}";
            return false;
        }
    }

    public static object[] GetTools() {
        return ToolDefinitions.Value;
    }

    public static object[] GetTools(bool includeExperimental) {
        if (includeExperimental) {
            return GetTools();
        }

        return ToolDefinitions.Value
            .Where(tool => !RequiresExperimentalAccess(tool.Name))
            .ToArray();
    }
}
