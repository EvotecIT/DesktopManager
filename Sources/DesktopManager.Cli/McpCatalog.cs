using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static class McpCatalog {
    private static readonly HashSet<string> KnownToolNames = new(StringComparer.Ordinal) {
        "get_active_window",
        "get_mouse_state",
        "get_clipboard_text",
        "get_elevation_status",
        "get_desktop_background_color",
        "set_desktop_background_color",
        "get_desktop_wallpaper_position",
        "set_desktop_wallpaper_position",
        "start_desktop_slideshow",
        "stop_desktop_slideshow",
        "advance_desktop_slideshow",
        "list_windows",
        "get_window_geometry",
        "get_window_process_info",
        "get_owner_window_process_info",
        "window_exists",
        "active_window_matches",
        "wait_for_window",
        "wait_for_window_close",
        "wait_for_window_to_lose_focus",
        "observe_window_text",
        "wait_for_observed_text",
        "get_focused_control",
        "wait_for_focused_control",
        "get_control_state",
        "list_window_controls",
        "diagnose_window_controls",
        "control_exists",
        "assert_control_value",
        "wait_for_control",
        "click_control",
        "focus_control",
        "set_control_enabled",
        "set_control_visibility",
        "set_control_text",
        "send_control_keys",
        "move_window",
        "click_window_point",
        "drag_window_points",
        "scroll_window_point",
        "type_window_text",
        "send_window_keys",
        "focus_window",
        "list_window_keep_alive",
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
        "list_monitors",
        "get_monitor_brightness",
        "get_monitor_wallpaper",
        "set_monitor_wallpaper",
        "set_monitor_brightness",
        "set_monitor_position",
        "set_monitor_resolution",
        "set_monitor_dpi_scaling",
        "set_taskbar_position",
        "screenshot_desktop",
        "screenshot_window",
        "launch_process",
        "launch_and_wait_for_window",
        "set_clipboard_text",
        "list_named_targets",
        "get_named_target",
        "save_window_target",
        "resolve_window_target",
        "list_named_control_targets",
        "get_named_control_target",
        "save_control_target",
        "resolve_control_target",
        "list_named_layouts",
        "save_current_layout",
        "apply_named_layout",
        "assert_window_layout",
        "list_named_snapshots",
        "save_current_snapshot",
        "restore_saved_snapshot",
        "prepare_for_coding",
        "prepare_for_screen_sharing",
        "clean_up_distractions"
    };

    private static readonly HashSet<string> MutatingToolNames = new(StringComparer.Ordinal) {
        "set_clipboard_text",
        "set_desktop_background_color",
        "set_desktop_wallpaper_position",
        "start_desktop_slideshow",
        "stop_desktop_slideshow",
        "advance_desktop_slideshow",
        "click_control",
        "focus_control",
        "set_control_enabled",
        "set_control_visibility",
        "set_control_text",
        "send_control_keys",
        "move_window",
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
        "set_monitor_brightness",
        "set_monitor_wallpaper",
        "set_monitor_position",
        "set_monitor_resolution",
        "set_monitor_dpi_scaling",
        "set_taskbar_position",
        "launch_process",
        "launch_and_wait_for_window",
        "save_window_target",
        "save_control_target",
        "save_current_layout",
        "apply_named_layout",
        "save_current_snapshot",
        "restore_saved_snapshot",
        "prepare_for_coding",
        "prepare_for_screen_sharing",
        "clean_up_distractions"
    };

    private static readonly HashSet<string> LiveDesktopMutationToolNames = new(StringComparer.Ordinal) {
        "click_control",
        "focus_control",
        "set_control_enabled",
        "set_control_visibility",
        "set_control_text",
        "send_control_keys",
        "move_window",
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
        "launch_process",
        "launch_and_wait_for_window",
        "apply_named_layout",
        "restore_saved_snapshot",
        "prepare_for_coding",
        "prepare_for_screen_sharing",
        "clean_up_distractions"
    };

    private static readonly HashSet<string> ForegroundInputFallbackToolNames = new(StringComparer.Ordinal) {
        "set_control_text",
        "send_control_keys"
    };

    public static bool IsKnownTool(string name) {
        return KnownToolNames.Contains(name);
    }

    public static bool IsMutatingTool(string name) {
        return MutatingToolNames.Contains(name);
    }

    public static bool AffectsLiveDesktop(string name) {
        return LiveDesktopMutationToolNames.Contains(name);
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
            case "set_control_text":
            case "send_control_keys":
            case "move_window":
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
            case "set_control_visibility":
                return TryResolveProcessPatternsFromWindowHandle(arguments, "windowHandle", out processPatterns, out error);
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
        return new object[] {
            CreateTool("get_active_window", "Get Active Window", "Return information about the currently focused window.", CreateObjectSchema(), readOnly: true),
            CreateTool("get_mouse_state", "Get Mouse State", "Return the current desktop mouse position, button state, and cursor visibility.", CreateObjectSchema(), readOnly: true),
            CreateTool("get_clipboard_text", "Get Clipboard Text", "Return the current Unicode clipboard text when available.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["retryCount"] = CreateIntegerSchema("Number of attempts to open the clipboard."),
                    ["retryDelayMs"] = CreateIntegerSchema("Delay between clipboard retry attempts in milliseconds.")
                }), readOnly: true),
            CreateTool("get_elevation_status", "Get Elevation Status", "Return whether the current DesktopManager host process is elevated.", CreateObjectSchema(), readOnly: true),
            CreateTool("get_desktop_background_color", "Get Desktop Background Color", "Return the current desktop background color.", CreateObjectSchema(), readOnly: true),
            CreateTool("set_desktop_background_color", "Set Desktop Background Color", "Set the desktop background color.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["color"] = CreateStringSchema("RGB color value as decimal, 0xRRGGBB, or #RRGGBB.")
                }, new[] { "color" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("get_desktop_wallpaper_position", "Get Desktop Wallpaper Position", "Return the current desktop wallpaper position.", CreateObjectSchema(), readOnly: true),
            CreateTool("set_desktop_wallpaper_position", "Set Desktop Wallpaper Position", "Set the desktop wallpaper position.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["position"] = CreateStringSchema("Wallpaper position: center, tile, stretch, fit, fill, or span.")
                }, new[] { "position" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("start_desktop_slideshow", "Start Desktop Slideshow", "Start a desktop wallpaper slideshow.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["imagePaths"] = CreateArraySchema("Wallpaper image paths for the slideshow.", CreateStringSchema("Image path."))
                }, new[] { "imagePaths" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("stop_desktop_slideshow", "Stop Desktop Slideshow", "Stop the active desktop wallpaper slideshow.", CreateObjectSchema(), readOnly: false, destructive: false, idempotent: true),
            CreateTool("advance_desktop_slideshow", "Advance Desktop Slideshow", "Advance the desktop wallpaper slideshow.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["direction"] = CreateStringSchema("Advance direction: forward or backward.")
                }, new[] { "direction" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("list_windows", "List Windows", "List visible desktop windows with optional filtering.", CreateWindowSelectorSchema(includeAll: false, includeEmpty: true), readOnly: true),
            CreateTool("get_window_geometry", "Get Window Geometry", "Return outer-window and client-area geometry for matching windows.", CreateWindowSelectorSchema(includeAll: true, includeEmpty: true), readOnly: true),
            CreateTool("get_window_process_info", "Get Window Process Info", "Return process metadata for one or more matching windows.", CreateWindowSelectorSchema(includeAll: true, includeEmpty: true), readOnly: true),
            CreateTool("get_owner_window_process_info", "Get Owner Window Process Info", "Return owner-window process metadata for one or more matching windows when available.", CreateWindowSelectorSchema(includeAll: true, includeEmpty: true), readOnly: true),
            CreateTool("window_exists", "Window Exists", "Check whether a matching window currently exists.", CreateWindowSelectorSchema(includeAll: false, includeEmpty: true), readOnly: true),
            CreateTool("active_window_matches", "Active Window Matches", "Check whether the current foreground window matches the selector.", CreateWindowSelectorSchema(includeAll: false, includeEmpty: true), readOnly: true),
            CreateTool("wait_for_window", "Wait For Window", "Wait for a matching window to appear.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["all"] = CreateBooleanSchema("Return all matching windows instead of the first match."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval in milliseconds.")
                }), readOnly: true),
            CreateTool("wait_for_window_close", "Wait For Window Close", "Wait for a matching window to close.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["all"] = CreateBooleanSchema("Track all matching windows instead of the first match."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval in milliseconds.")
                }), readOnly: true),
            CreateTool("wait_for_window_to_lose_focus", "Wait For Window To Lose Focus", "Wait for a matching window to no longer own foreground focus.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["all"] = CreateBooleanSchema("Track all matching windows instead of the first match."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval in milliseconds.")
                }), readOnly: true),
            CreateTool("observe_window_text", "Observe Window Text", "Return the best available text observation for a matching window.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["expectedText"] = CreateStringSchema("Optional text to prefer when present."),
                    ["maxLength"] = CreateIntegerSchema("Maximum observed text length."),
                    ["retryCount"] = CreateIntegerSchema("Observation retry count."),
                    ["retryDelayMs"] = CreateIntegerSchema("Delay between observation retries in milliseconds.")
                }), readOnly: true),
            CreateTool("wait_for_observed_text", "Wait For Observed Text", "Wait until observed window text contains the requested value.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["expectedText"] = CreateStringSchema("Text to wait for."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval in milliseconds."),
                    ["maxLength"] = CreateIntegerSchema("Maximum observed text length."),
                    ["retryCount"] = CreateIntegerSchema("Observation retry count."),
                    ["retryDelayMs"] = CreateIntegerSchema("Delay between observation retries in milliseconds.")
                }, new[] { "expectedText" }), readOnly: true),
            CreateTool("get_focused_control", "Get Focused Control", "Return focused-control metadata for a matching window.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles.")
                }), readOnly: true),
            CreateTool("wait_for_focused_control", "Wait For Focused Control", "Wait until a matching window exposes a focused control.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval in milliseconds.")
                }), readOnly: true),
            CreateTool("get_control_state", "Get Control State", "Return the observable state for a specific control handle.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowHandle"] = CreateStringSchema("Parent window handle in decimal or hexadecimal format."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format.")
                }, new[] { "windowHandle", "controlHandle" }), readOnly: true),
            CreateTool("list_window_controls", "List Window Controls", "List child controls for one or more matching windows.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["supportsBackgroundClick"] = CreateBooleanSchema("Filter by whether the control supports background-safe click or invoke actions."),
                    ["supportsBackgroundText"] = CreateBooleanSchema("Filter by whether the control supports background-safe text updates."),
                    ["supportsBackgroundKeys"] = CreateBooleanSchema("Filter by whether the control supports background-safe key delivery."),
                    ["supportsForegroundInputFallback"] = CreateBooleanSchema("Filter by whether the control supports explicit foreground input fallback."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["targetName"] = CreateStringSchema("Optional saved control target name."),
                    ["allWindows"] = CreateBooleanSchema("Enumerate controls for all matching windows.")
                }), readOnly: true),
            CreateTool("diagnose_window_controls", "Diagnose Window Controls", "Collect discovery diagnostics for matching window controls.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["supportsBackgroundClick"] = CreateBooleanSchema("Filter by whether the control supports background-safe click or invoke actions."),
                    ["supportsBackgroundText"] = CreateBooleanSchema("Filter by whether the control supports background-safe text updates."),
                    ["supportsBackgroundKeys"] = CreateBooleanSchema("Filter by whether the control supports background-safe key delivery."),
                    ["supportsForegroundInputFallback"] = CreateBooleanSchema("Filter by whether the control supports explicit foreground input fallback."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["targetName"] = CreateStringSchema("Optional saved control target name."),
                    ["allWindows"] = CreateBooleanSchema("Enumerate controls for all matching windows."),
                    ["sampleLimit"] = CreateIntegerSchema("Maximum number of sample controls to include in each diagnostic result."),
                    ["includeActionProbe"] = CreateBooleanSchema("Include a read-only UI Automation action-resolution probe for the first matched UIA control.")
                }), readOnly: true),
            CreateTool("control_exists", "Control Exists", "Check whether a matching control currently exists.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["supportsBackgroundClick"] = CreateBooleanSchema("Filter by whether the control supports background-safe click or invoke actions."),
                    ["supportsBackgroundText"] = CreateBooleanSchema("Filter by whether the control supports background-safe text updates."),
                    ["supportsBackgroundKeys"] = CreateBooleanSchema("Filter by whether the control supports background-safe key delivery."),
                    ["supportsForegroundInputFallback"] = CreateBooleanSchema("Filter by whether the control supports explicit foreground input fallback."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["targetName"] = CreateStringSchema("Optional saved control target name."),
                    ["allWindows"] = CreateBooleanSchema("Enumerate controls for all matching windows.")
                }), readOnly: true),
            CreateTool("assert_control_value", "Assert Control Value", "Assert that matched controls expose a specific value or text.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["supportsBackgroundClick"] = CreateBooleanSchema("Filter by whether the control supports background-safe click or invoke actions."),
                    ["supportsBackgroundText"] = CreateBooleanSchema("Filter by whether the control supports background-safe text updates."),
                    ["supportsBackgroundKeys"] = CreateBooleanSchema("Filter by whether the control supports background-safe key delivery."),
                    ["supportsForegroundInputFallback"] = CreateBooleanSchema("Filter by whether the control supports explicit foreground input fallback."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["targetName"] = CreateStringSchema("Optional saved control target name."),
                    ["expectedValue"] = CreateStringSchema("Expected control value or text."),
                    ["contains"] = CreateBooleanSchema("Use case-insensitive contains matching instead of exact equality."),
                    ["all"] = CreateBooleanSchema("Require all matching controls to satisfy the assertion."),
                    ["allWindows"] = CreateBooleanSchema("Enumerate controls for all matching windows.")
                }, new[] { "expectedValue" }), readOnly: true),
            CreateTool("wait_for_control", "Wait For Control", "Wait for a matching control to appear.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["supportsBackgroundClick"] = CreateBooleanSchema("Filter by whether the control supports background-safe click or invoke actions."),
                    ["supportsBackgroundText"] = CreateBooleanSchema("Filter by whether the control supports background-safe text updates."),
                    ["supportsBackgroundKeys"] = CreateBooleanSchema("Filter by whether the control supports background-safe key delivery."),
                    ["supportsForegroundInputFallback"] = CreateBooleanSchema("Filter by whether the control supports explicit foreground input fallback."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["targetName"] = CreateStringSchema("Optional saved control target name."),
                    ["all"] = CreateBooleanSchema("Return all matching controls instead of the first match."),
                    ["allWindows"] = CreateBooleanSchema("Enumerate controls for all matching windows."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval in milliseconds.")
                }), readOnly: true),
            CreateTool("click_control", "Click Control", "Click a matching child control.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["supportsBackgroundClick"] = CreateBooleanSchema("Filter by whether the control supports background-safe click or invoke actions."),
                    ["supportsBackgroundText"] = CreateBooleanSchema("Filter by whether the control supports background-safe text updates."),
                    ["supportsBackgroundKeys"] = CreateBooleanSchema("Filter by whether the control supports background-safe key delivery."),
                    ["supportsForegroundInputFallback"] = CreateBooleanSchema("Filter by whether the control supports explicit foreground input fallback."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["targetName"] = CreateStringSchema("Optional saved control target name."),
                    ["button"] = CreateStringSchema("Mouse button: left or right."),
                    ["all"] = CreateBooleanSchema("Apply to all matching controls."),
                    ["allWindows"] = CreateBooleanSchema("Target controls in all matching windows.")
                })), readOnly: false, destructive: false, idempotent: true),
            CreateTool("focus_control", "Focus Control", "Focus a specific control handle.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowHandle"] = CreateStringSchema("Parent window handle in decimal or hexadecimal format."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Ensure the parent window becomes foreground before focusing the control.")
                }), new[] { "windowHandle", "controlHandle" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_control_enabled", "Set Control Enabled", "Enable or disable a specific control handle.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowHandle"] = CreateStringSchema("Parent window handle in decimal or hexadecimal format."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["enabled"] = CreateBooleanSchema("True to enable the control; false to disable it.")
                }), new[] { "windowHandle", "controlHandle", "enabled" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_control_visibility", "Set Control Visibility", "Show or hide a specific control handle.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowHandle"] = CreateStringSchema("Parent window handle in decimal or hexadecimal format."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["visible"] = CreateBooleanSchema("True to show the control; false to hide it.")
                }), new[] { "windowHandle", "controlHandle", "visible" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_control_text", "Set Control Text", "Set text on a matching child control.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["supportsBackgroundClick"] = CreateBooleanSchema("Filter by whether the control supports background-safe click or invoke actions."),
                    ["supportsBackgroundText"] = CreateBooleanSchema("Filter by whether the control supports background-safe text updates."),
                    ["supportsBackgroundKeys"] = CreateBooleanSchema("Filter by whether the control supports background-safe key delivery."),
                    ["supportsForegroundInputFallback"] = CreateBooleanSchema("Filter by whether the control supports explicit foreground input fallback."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["allowForegroundInput"] = CreateBooleanSchema("Explicitly allow focused foreground input fallback for zero-handle UI Automation controls."),
                    ["targetName"] = CreateStringSchema("Optional saved control target name."),
                    ["text"] = CreateStringSchema("Text to set on the control."),
                    ["all"] = CreateBooleanSchema("Apply to all matching controls."),
                    ["allWindows"] = CreateBooleanSchema("Target controls in all matching windows.")
                }), new[] { "text" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("send_control_keys", "Send Control Keys", "Send keys to a matching child control.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["supportsBackgroundClick"] = CreateBooleanSchema("Filter by whether the control supports background-safe click or invoke actions."),
                    ["supportsBackgroundText"] = CreateBooleanSchema("Filter by whether the control supports background-safe text updates."),
                    ["supportsBackgroundKeys"] = CreateBooleanSchema("Filter by whether the control supports background-safe key delivery."),
                    ["supportsForegroundInputFallback"] = CreateBooleanSchema("Filter by whether the control supports explicit foreground input fallback."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["allowForegroundInput"] = CreateBooleanSchema("Explicitly allow focused foreground input fallback for zero-handle UI Automation controls."),
                    ["targetName"] = CreateStringSchema("Optional saved control target name."),
                    ["keys"] = new {
                        type = "array",
                        items = new { type = "string" },
                        description = "Virtual key names such as VK_CONTROL or VK_S."
                    },
                    ["all"] = CreateBooleanSchema("Apply to all matching controls."),
                    ["allWindows"] = CreateBooleanSchema("Target controls in all matching windows.")
                }), new[] { "keys" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("move_window", "Move Window", "Move and optionally resize a window by title, process, pid, class, or handle.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["monitor"] = CreateIntegerSchema("Target monitor index."),
                    ["x"] = CreateIntegerSchema("Left coordinate."),
                    ["y"] = CreateIntegerSchema("Top coordinate."),
                    ["width"] = CreateIntegerSchema("Window width."),
                    ["height"] = CreateIntegerSchema("Window height."),
                    ["activate"] = CreateBooleanSchema("Activate the window after moving."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                })), readOnly: false, destructive: false, idempotent: true),
            CreateTool("click_window_point", "Click Window Point", "Click a point relative to a matching window.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["targetName"] = CreateStringSchema("Saved reusable target name."),
                    ["x"] = CreateIntegerSchema("Horizontal coordinate relative to the window bounds."),
                    ["y"] = CreateIntegerSchema("Vertical coordinate relative to the window bounds."),
                    ["xRatio"] = CreateNumberSchema("Horizontal coordinate ratio from 0 to 1."),
                    ["yRatio"] = CreateNumberSchema("Vertical coordinate ratio from 0 to 1."),
                    ["button"] = CreateStringSchema("Mouse button: left or right."),
                    ["activate"] = CreateBooleanSchema("Activate the window before clicking."),
                    ["clientArea"] = CreateBooleanSchema("Interpret coordinates relative to the window client area."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                })), readOnly: false, destructive: false, idempotent: false),
            CreateTool("drag_window_points", "Drag Window Points", "Drag between two points relative to a matching window.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["startTargetName"] = CreateStringSchema("Saved reusable starting target name."),
                    ["endTargetName"] = CreateStringSchema("Saved reusable ending target name."),
                    ["startX"] = CreateIntegerSchema("Horizontal starting coordinate relative to the window bounds."),
                    ["startY"] = CreateIntegerSchema("Vertical starting coordinate relative to the window bounds."),
                    ["startXRatio"] = CreateNumberSchema("Horizontal starting coordinate ratio from 0 to 1."),
                    ["startYRatio"] = CreateNumberSchema("Vertical starting coordinate ratio from 0 to 1."),
                    ["endX"] = CreateIntegerSchema("Horizontal ending coordinate relative to the window bounds."),
                    ["endY"] = CreateIntegerSchema("Vertical ending coordinate relative to the window bounds."),
                    ["endXRatio"] = CreateNumberSchema("Horizontal ending coordinate ratio from 0 to 1."),
                    ["endYRatio"] = CreateNumberSchema("Vertical ending coordinate ratio from 0 to 1."),
                    ["button"] = CreateStringSchema("Mouse button: left or right."),
                    ["stepDelayMs"] = CreateIntegerSchema("Delay in milliseconds between drag steps."),
                    ["activate"] = CreateBooleanSchema("Activate the window before dragging."),
                    ["clientArea"] = CreateBooleanSchema("Interpret coordinates relative to the window client area."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                })), readOnly: false, destructive: false, idempotent: false),
            CreateTool("scroll_window_point", "Scroll Window Point", "Scroll the mouse wheel at a point relative to a matching window.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["targetName"] = CreateStringSchema("Saved reusable target name."),
                    ["x"] = CreateIntegerSchema("Horizontal coordinate relative to the window bounds."),
                    ["y"] = CreateIntegerSchema("Vertical coordinate relative to the window bounds."),
                    ["xRatio"] = CreateNumberSchema("Horizontal coordinate ratio from 0 to 1."),
                    ["yRatio"] = CreateNumberSchema("Vertical coordinate ratio from 0 to 1."),
                    ["delta"] = CreateIntegerSchema("Scroll delta. Positive scrolls up."),
                    ["activate"] = CreateBooleanSchema("Activate the window before scrolling."),
                    ["clientArea"] = CreateBooleanSchema("Interpret coordinates relative to the window client area."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), new[] { "delta" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("type_window_text", "Type Window Text", "Type or paste text into a matching window.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["text"] = CreateStringSchema("Text to send to the window."),
                    ["paste"] = CreateBooleanSchema("Use clipboard paste instead of typed characters."),
                    ["foregroundInput"] = CreateBooleanSchema("Require real foreground keyboard input and fail instead of falling back to background messages."),
                    ["physicalKeys"] = CreateBooleanSchema("Prefer layout-aware physical key presses for foreground typing and fall back to Unicode packets only when no keyboard mapping exists."),
                    ["hostedSession"] = CreateBooleanSchema("Enable the hosted-session typing profile for RDP, Hyper-V, and Remote Desktop Manager style targets."),
                    ["script"] = CreateBooleanSchema("Preserve multiline formatting and chunk long lines into smaller typed segments."),
                    ["chunkSize"] = CreateIntegerSchema("Maximum characters to send in each script chunk."),
                    ["lineDelayMs"] = CreateIntegerSchema("Delay in milliseconds after each scripted line break."),
                    ["delayMs"] = CreateIntegerSchema("Delay in milliseconds between typed characters."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), new[] { "text" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("send_window_keys", "Send Window Keys", "Send keys to a matching window after activating it.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["keys"] = CreateArraySchema("Keys to send to the window.", CreateStringSchema("Virtual key name or single character.")),
                    ["activate"] = CreateBooleanSchema("Activate the window before sending keys. Defaults to true."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), new[] { "keys" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("focus_window", "Focus Window", "Bring a matching window to the foreground.", CreateWindowMutationSelectorSchema(includeAll: true, includeEmpty: false), readOnly: false, destructive: false, idempotent: true),
            CreateTool("list_window_keep_alive", "List Window Keep Alive", "List windows that currently have active keep-alive timers.", CreateObjectSchema(), readOnly: true),
            CreateTool("start_window_keep_alive", "Start Window Keep Alive", "Start periodic keep-alive activity for one or more matching windows.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["intervalMs"] = CreateIntegerSchema("Keep-alive interval in milliseconds. Defaults to 60000."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("stop_window_keep_alive", "Stop Window Keep Alive", "Stop keep-alive activity for matching windows or all active keep-alive sessions.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match."),
                    ["allSessions"] = CreateBooleanSchema("Stop every active keep-alive session across the desktop.")
                }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("minimize_windows", "Minimize Windows", "Minimize one or more matching windows.", CreateWindowMutationSelectorSchema(includeAll: true, includeEmpty: false), readOnly: false, destructive: false, idempotent: true),
            CreateTool("maximize_windows", "Maximize Windows", "Maximize one or more matching windows.", CreateWindowMutationSelectorSchema(includeAll: true, includeEmpty: false), readOnly: false, destructive: false, idempotent: true),
            CreateTool("restore_windows", "Restore Windows", "Restore one or more matching windows to their normal state.", CreateWindowMutationSelectorSchema(includeAll: true, includeEmpty: false), readOnly: false, destructive: false, idempotent: true),
            CreateTool("close_windows", "Close Windows", "Close one or more matching windows.", CreateWindowMutationSelectorSchema(includeAll: true, includeEmpty: false), readOnly: false, destructive: true, idempotent: false),
            CreateTool("set_window_topmost", "Set Window Topmost", "Enable or disable the topmost flag for matching windows.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["topMost"] = CreateBooleanSchema("Whether the matching windows should become topmost."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), new[] { "topMost" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_window_visibility", "Set Window Visibility", "Show or hide matching windows.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["visible"] = CreateBooleanSchema("Whether the matching windows should be visible."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), new[] { "visible" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_window_transparency", "Set Window Transparency", "Set window transparency for matching windows.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["alpha"] = CreateIntegerSchema("Transparency alpha from 0 to 255."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), new[] { "alpha" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("snap_window", "Snap Window", "Snap one or more matching windows to a predefined monitor region.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["position"] = CreateStringSchema("One of left, right, top-left, top-right, bottom-left, bottom-right."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), new[] { "position" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("list_monitors", "List Monitors", "List connected monitors and their bounds.", CreateObjectSchema(
                CreateMonitorSelectorProperties()), readOnly: true),
            CreateTool("get_monitor_brightness", "Get Monitor Brightness", "Return brightness values for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorSelectorProperties()), readOnly: true),
            CreateTool("get_monitor_wallpaper", "Get Monitor Wallpaper", "Return wallpaper paths for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorSelectorProperties()), readOnly: true),
            CreateTool("set_monitor_wallpaper", "Set Monitor Wallpaper", "Set wallpaper for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["wallpaperPath"] = CreateStringSchema("Wallpaper file path."),
                    ["url"] = CreateStringSchema("Wallpaper URL."),
                    ["position"] = CreateStringSchema("Optional wallpaper position: center, tile, stretch, fit, fill, or span.")
                })), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_monitor_brightness", "Set Monitor Brightness", "Set brightness for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["brightness"] = CreateIntegerSchema("Brightness level to apply from 0 to 100.")
                }), new[] { "brightness" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_monitor_position", "Set Monitor Position", "Set monitor bounds for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["left"] = CreateIntegerSchema("Monitor left coordinate."),
                    ["top"] = CreateIntegerSchema("Monitor top coordinate."),
                    ["right"] = CreateIntegerSchema("Monitor right coordinate."),
                    ["bottom"] = CreateIntegerSchema("Monitor bottom coordinate.")
                }), new[] { "left", "top", "right", "bottom" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_monitor_resolution", "Set Monitor Resolution", "Set monitor resolution and optional orientation for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["width"] = CreateIntegerSchema("Monitor width in pixels."),
                    ["height"] = CreateIntegerSchema("Monitor height in pixels."),
                    ["orientation"] = CreateStringSchema("Optional display orientation: default, degrees90, degrees180, or degrees270.")
                }), new[] { "width", "height" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_monitor_dpi_scaling", "Set Monitor Dpi Scaling", "Set monitor DPI scaling for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["scalingPercent"] = CreateIntegerSchema("Scaling percentage to apply, such as 100, 125, or 150.")
                }), new[] { "scalingPercent" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_taskbar_position", "Set Taskbar Position", "Set taskbar edge and optional visibility for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["position"] = CreateStringSchema("Optional taskbar edge: left, top, right, or bottom."),
                    ["visible"] = CreateBooleanSchema("Optional taskbar visibility flag.")
                })), readOnly: false, destructive: false, idempotent: true),
            CreateTool("screenshot_desktop", "Screenshot Desktop", "Capture the desktop, a monitor, or a region to a PNG file.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["monitor"] = CreateIntegerSchema("Target monitor index."),
                    ["deviceId"] = CreateStringSchema("Target monitor device identifier."),
                    ["deviceName"] = CreateStringSchema("Target monitor device name."),
                    ["left"] = CreateIntegerSchema("Left coordinate for region capture."),
                    ["top"] = CreateIntegerSchema("Top coordinate for region capture."),
                    ["width"] = CreateIntegerSchema("Width for region capture."),
                    ["height"] = CreateIntegerSchema("Height for region capture."),
                    ["outputPath"] = CreateStringSchema("Optional PNG output path.")
                }), readOnly: true),
            CreateTool("screenshot_window", "Screenshot Window", "Capture a matching window to a PNG file.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["targetName"] = CreateStringSchema("Optional named target area to capture within the window."),
                    ["outputPath"] = CreateStringSchema("Optional PNG output path.")
                }), readOnly: true),
            CreateTool("launch_process", "Launch Process", "Start a desktop application or process.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["filePath"] = CreateStringSchema("Executable path or shell command."),
                    ["arguments"] = CreateStringSchema("Optional argument string."),
                    ["workingDirectory"] = CreateStringSchema("Optional working directory."),
                    ["waitForInputIdleMs"] = CreateIntegerSchema("Optional wait for UI input idle in milliseconds."),
                    ["waitForWindowMs"] = CreateIntegerSchema("Optional time to wait for a launched window in milliseconds."),
                    ["waitForWindowIntervalMs"] = CreateIntegerSchema("Polling interval while waiting for a launched window."),
                    ["windowTitle"] = CreateStringSchema("Optional launched-window title filter."),
                    ["windowClassName"] = CreateStringSchema("Optional launched-window class filter."),
                    ["requireWindow"] = CreateBooleanSchema("Require a launched window to be found before returning.")
                }, new[] { "filePath" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("set_clipboard_text", "Set Clipboard Text", "Replace the current Unicode clipboard text.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["text"] = CreateStringSchema("Text to place on the clipboard."),
                    ["retryCount"] = CreateIntegerSchema("Number of attempts to open the clipboard."),
                    ["retryDelayMs"] = CreateIntegerSchema("Delay between clipboard retry attempts in milliseconds.")
                }, new[] { "text" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("launch_and_wait_for_window", "Launch And Wait For Window", "Start a desktop application or process, then wait for a matching launched window.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["filePath"] = CreateStringSchema("Executable path or shell command."),
                    ["arguments"] = CreateStringSchema("Optional argument string."),
                    ["workingDirectory"] = CreateStringSchema("Optional working directory."),
                    ["waitForInputIdleMs"] = CreateIntegerSchema("Optional wait for UI input idle in milliseconds."),
                    ["launchWaitForWindowMs"] = CreateIntegerSchema("Optional launch-time correlation wait in milliseconds."),
                    ["launchWaitForWindowIntervalMs"] = CreateIntegerSchema("Polling interval while correlating the launched window."),
                    ["launchWindowTitle"] = CreateStringSchema("Optional launch-time window title filter."),
                    ["launchWindowClass"] = CreateStringSchema("Optional launch-time window class filter."),
                    ["windowTitle"] = CreateStringSchema("Optional final window title filter."),
                    ["windowClass"] = CreateStringSchema("Optional final window class filter."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows while waiting."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles while waiting."),
                    ["followProcessFamily"] = CreateBooleanSchema("When launch-time correlation did not resolve a concrete window process, allow the final wait to follow the launched app's same-name process family."),
                    ["all"] = CreateBooleanSchema("Return all matching windows instead of the first match."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait for the final window in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval while waiting for the final window.")
                }), new[] { "filePath" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("list_named_targets", "List Named Targets", "List saved reusable window-relative targets.", CreateObjectSchema(), readOnly: true),
            CreateTool("get_named_target", "Get Named Target", "Get a saved reusable window-relative target definition.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Target name.")
                }, new[] { "name" }), readOnly: true),
            CreateTool("save_window_target", "Save Window Target", "Save or update a reusable window-relative target definition.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Target name."),
                    ["description"] = CreateStringSchema("Optional target description."),
                    ["x"] = CreateIntegerSchema("Horizontal coordinate relative to the target bounds."),
                    ["y"] = CreateIntegerSchema("Vertical coordinate relative to the target bounds."),
                    ["xRatio"] = CreateNumberSchema("Horizontal coordinate ratio from 0 to 1."),
                    ["yRatio"] = CreateNumberSchema("Vertical coordinate ratio from 0 to 1."),
                    ["width"] = CreateIntegerSchema("Optional target area width in pixels."),
                    ["height"] = CreateIntegerSchema("Optional target area height in pixels."),
                    ["widthRatio"] = CreateNumberSchema("Optional target area width ratio from 0 to 1."),
                    ["heightRatio"] = CreateNumberSchema("Optional target area height ratio from 0 to 1."),
                    ["clientArea"] = CreateBooleanSchema("Interpret coordinates relative to the window client area.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("resolve_window_target", "Resolve Window Target", "Resolve a saved target against one or more live windows.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Target name."),
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }, new[] { "name" }), readOnly: true),
            CreateTool("list_named_control_targets", "List Named Control Targets", "List saved reusable control selector targets.", CreateObjectSchema(), readOnly: true),
            CreateTool("get_named_control_target", "Get Named Control Target", "Get a saved reusable control selector target definition.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Control target name.")
                }, new[] { "name" }), readOnly: true),
            CreateTool("save_control_target", "Save Control Target", "Save or update a reusable control selector target definition.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Control target name."),
                    ["description"] = CreateStringSchema("Optional control target description."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["supportsBackgroundClick"] = CreateBooleanSchema("Filter by whether the control supports background-safe click or invoke actions."),
                    ["supportsBackgroundText"] = CreateBooleanSchema("Filter by whether the control supports background-safe text updates."),
                    ["supportsBackgroundKeys"] = CreateBooleanSchema("Filter by whether the control supports background-safe key delivery."),
                    ["supportsForegroundInputFallback"] = CreateBooleanSchema("Filter by whether the control supports explicit foreground input fallback."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("resolve_control_target", "Resolve Control Target", "Resolve a saved control selector target against one or more live windows.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Control target name."),
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match."),
                    ["allControls"] = CreateBooleanSchema("Return all matching controls instead of only the first match per window.")
                }, new[] { "name" }), readOnly: true),
            CreateTool("list_named_layouts", "List Named Layouts", "List saved named layouts.", CreateObjectSchema(), readOnly: true),
            CreateTool("save_current_layout", "Save Current Layout", "Save the current desktop window layout under a given name.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Layout name.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("apply_named_layout", "Apply Named Layout", "Restore a previously saved named layout.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Layout name."),
                    ["validate"] = CreateBooleanSchema("Validate the layout before applying it.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("assert_window_layout", "Assert Window Layout", "Assert that the current desktop windows satisfy a saved named layout within configurable tolerances.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Layout name."),
                    ["positionTolerancePx"] = CreateIntegerSchema("Allowed left/top difference in pixels."),
                    ["sizeTolerancePx"] = CreateIntegerSchema("Allowed width/height difference in pixels."),
                    ["checkState"] = CreateBooleanSchema("Require saved window state values to match when present."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows while asserting."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles while asserting.")
                }), new[] { "name" }), readOnly: true),
            CreateTool("list_named_snapshots", "List Named Snapshots", "List saved named snapshots.", CreateObjectSchema(), readOnly: true),
            CreateTool("save_current_snapshot", "Save Current Snapshot", "Save the current desktop snapshot. Snapshots are windows-only for now.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Snapshot name.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("restore_saved_snapshot", "Restore Saved Snapshot", "Restore a previously saved snapshot. Snapshots are windows-only for now.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Snapshot name."),
                    ["validate"] = CreateBooleanSchema("Validate the snapshot before applying it.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("prepare_for_coding", "Prepare For Coding", "Apply a preferred layout when available and focus a likely editor or terminal window.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["layoutName"] = CreateStringSchema("Optional preferred layout to apply."),
                    ["windowTitle"] = CreateStringSchema("Optional explicit focus window title filter."),
                    ["processName"] = CreateStringSchema("Optional explicit focus process filter."),
                    ["className"] = CreateStringSchema("Optional explicit focus class filter."),
                    ["processId"] = CreateIntegerSchema("Optional explicit focus process identifier."),
                    ["handle"] = CreateStringSchema("Optional explicit focus window handle."),
                    ["activeWindow"] = CreateBooleanSchema("Focus the current active window when requested.")
                })), readOnly: false, destructive: false, idempotent: true),
            CreateTool("prepare_for_screen_sharing", "Prepare For Screen Sharing", "Apply a preferred layout, minimize common distractions, and focus a likely sharing window.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["layoutName"] = CreateStringSchema("Optional preferred layout to apply."),
                    ["windowTitle"] = CreateStringSchema("Optional explicit focus window title filter."),
                    ["processName"] = CreateStringSchema("Optional explicit focus process filter."),
                    ["className"] = CreateStringSchema("Optional explicit focus class filter."),
                    ["processId"] = CreateIntegerSchema("Optional explicit focus process identifier."),
                    ["handle"] = CreateStringSchema("Optional explicit focus window handle."),
                    ["activeWindow"] = CreateBooleanSchema("Focus the current active window when requested.")
                })), readOnly: false, destructive: false, idempotent: true),
            CreateTool("clean_up_distractions", "Clean Up Distractions", "Minimize common chat, mail, and messaging windows before focused work or sharing.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object>())), readOnly: false, destructive: false, idempotent: true)
        };
    }

    public static object[] GetResources() {
        return new object[] {
            new {
                name = "desktop_monitors",
                title = "Desktop Monitors",
                uri = "desktop://monitors",
                description = "Current connected monitor list as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_windows_visible",
                title = "Visible Windows",
                uri = "desktop://windows/visible",
                description = "Current visible windows as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_active_window",
                title = "Active Window",
                uri = "desktop://windows/active",
                description = "Current active window as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_layouts",
                title = "Named Layouts",
                uri = "desktop://layouts",
                description = "Saved named layouts as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_targets",
                title = "Named Targets",
                uri = "desktop://targets",
                description = "Saved reusable window-relative targets as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_control_targets",
                title = "Named Control Targets",
                uri = "desktop://control-targets",
                description = "Saved reusable control selector targets as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_snapshot_current",
                title = "Current Desktop Snapshot",
                uri = "desktop://snapshot/current",
                description = "Current windows and monitors summary as JSON.",
                mimeType = "application/json"
            }
        };
    }

    public static object[] GetPrompts() {
        return new object[] {
            new {
                name = "prepare_for_coding",
                title = "Prepare For Coding",
                description = "Arrange the desktop for focused coding work.",
                arguments = new object[] {
                    new {
                        name = "layoutName",
                        description = "Preferred named layout to apply before focusing the editor.",
                        required = false
                    }
                }
            },
            new {
                name = "prepare_for_screen_sharing",
                title = "Prepare For Screen Sharing",
                description = "Arrange the desktop for a clean screen sharing session.",
                arguments = new object[] {
                    new {
                        name = "layoutName",
                        description = "Preferred named layout to apply before sharing.",
                        required = false
                    }
                }
            },
            new {
                name = "clean_up_distractions",
                title = "Clean Up Distractions",
                description = "Hide or minimize noisy windows before focused work.",
                arguments = Array.Empty<object>()
            }
        };
    }

    public static bool TryCallTool(string name, JsonElement arguments, out object result, out string? error) {
        try {
            result = name switch {
                "get_active_window" => DesktopOperations.GetActiveWindow(),
                "get_mouse_state" => DesktopOperations.GetMouseState(),
                "get_clipboard_text" => DesktopOperations.GetClipboardText(
                    ReadInt(arguments, "retryCount"),
                    ReadInt(arguments, "retryDelayMs")),
                "get_elevation_status" => DesktopOperations.GetElevationStatus(),
                "get_desktop_background_color" => DesktopOperations.GetDesktopBackgroundColor(),
                "set_desktop_background_color" => DesktopOperations.SetDesktopBackgroundColor(
                    ReadColor(arguments, "color")),
                "get_desktop_wallpaper_position" => DesktopOperations.GetDesktopWallpaperPosition(),
                "set_desktop_wallpaper_position" => DesktopOperations.SetDesktopWallpaperPosition(
                    ReadWallpaperPosition(arguments, "position") ?? throw new CommandLineException("Property 'position' is required.")),
                "start_desktop_slideshow" => DesktopOperations.StartDesktopSlideshow(
                    ReadStringList(arguments, "imagePaths")),
                "stop_desktop_slideshow" => DesktopOperations.StopDesktopSlideshow(),
                "advance_desktop_slideshow" => DesktopOperations.AdvanceDesktopSlideshow(
                    ReadSlideshowDirection(arguments, "direction") ?? throw new CommandLineException("Property 'direction' is required.")),
                "list_windows" => DesktopOperations.ListWindows(ReadWindowCriteria(arguments, false)),
                "get_window_geometry" => DesktopOperations.GetWindowGeometry(ReadWindowCriteria(arguments, true)),
                "get_window_process_info" => DesktopOperations.GetWindowProcessInfo(ReadWindowCriteria(arguments, true), owner: false),
                "get_owner_window_process_info" => DesktopOperations.GetWindowProcessInfo(ReadWindowCriteria(arguments, true), owner: true),
                "window_exists" => DesktopOperations.WindowExists(ReadWindowCriteria(arguments, true)),
                "active_window_matches" => DesktopOperations.ActiveWindowMatches(ReadWindowCriteria(arguments, true)),
                "wait_for_window" => DesktopOperations.WaitForWindow(
                    ReadWindowCriteria(arguments, true),
                    ReadInt(arguments, "timeoutMs") ?? 10000,
                    ReadInt(arguments, "intervalMs") ?? 200),
                "wait_for_window_close" => DesktopOperations.WaitForWindowToClose(
                    ReadWindowCriteria(arguments, true),
                    ReadInt(arguments, "timeoutMs") ?? 10000,
                    ReadInt(arguments, "intervalMs") ?? 200),
                "wait_for_window_to_lose_focus" => DesktopOperations.WaitForWindowToLoseFocus(
                    ReadWindowCriteria(arguments, true),
                    ReadInt(arguments, "timeoutMs") ?? 10000,
                    ReadInt(arguments, "intervalMs") ?? 200),
                "observe_window_text" => DesktopOperations.ObserveWindowText(
                    ReadWindowCriteria(arguments, true),
                    ReadOptionalString(arguments, "expectedText"),
                    ReadInt(arguments, "maxLength"),
                    ReadInt(arguments, "retryCount"),
                    ReadInt(arguments, "retryDelayMs"))!,
                "wait_for_observed_text" => DesktopOperations.WaitForObservedText(
                    ReadWindowCriteria(arguments, true),
                    ReadRequiredString(arguments, "expectedText"),
                    ReadInt(arguments, "timeoutMs") ?? 10000,
                    ReadInt(arguments, "intervalMs") ?? 200,
                    ReadInt(arguments, "maxLength"),
                    ReadInt(arguments, "retryCount"),
                    ReadInt(arguments, "retryDelayMs")),
                "get_focused_control" => DesktopOperations.GetFocusedControl(ReadWindowCriteria(arguments, true))!,
                "wait_for_focused_control" => DesktopOperations.WaitForFocusedControl(
                    ReadWindowCriteria(arguments, true),
                    ReadInt(arguments, "timeoutMs") ?? 10000,
                    ReadInt(arguments, "intervalMs") ?? 200),
                "get_control_state" => DesktopOperations.GetControlState(
                    ReadRequiredString(arguments, "windowHandle"),
                    ReadRequiredString(arguments, "controlHandle")),
                "move_window" => DesktopOperations.MoveWindow(
                    ReadWindowCriteria(arguments, true),
                    ReadInt(arguments, "monitor"),
                    ReadInt(arguments, "x"),
                    ReadInt(arguments, "y"),
                    ReadInt(arguments, "width"),
                    ReadInt(arguments, "height"),
                    ReadBool(arguments, "activate"),
                    ReadMutationArtifactOptions(arguments)),
                "click_window_point" => CallClickWindowPoint(arguments),
                "drag_window_points" => CallDragWindowPoints(arguments),
                "scroll_window_point" => CallScrollWindowPoint(arguments),
                "focus_window" => DesktopOperations.FocusWindow(ReadWindowCriteria(arguments, true), ReadMutationArtifactOptions(arguments)),
                "list_window_keep_alive" => DesktopOperations.ListWindowKeepAlive(),
                "start_window_keep_alive" => DesktopOperations.StartWindowKeepAlive(
                    ReadWindowCriteria(arguments, true),
                    ReadPositiveInteger(arguments, "intervalMs") ?? 60000),
                "stop_window_keep_alive" => StopWindowKeepAlive(arguments),
                "minimize_windows" => DesktopOperations.MinimizeWindows(ReadWindowCriteria(arguments, true), ReadMutationArtifactOptions(arguments)),
                "maximize_windows" => DesktopOperations.MaximizeWindows(ReadWindowCriteria(arguments, true), ReadMutationArtifactOptions(arguments)),
                "restore_windows" => DesktopOperations.RestoreWindows(ReadWindowCriteria(arguments, true), ReadMutationArtifactOptions(arguments)),
                "close_windows" => DesktopOperations.CloseWindows(ReadWindowCriteria(arguments, true), ReadMutationArtifactOptions(arguments)),
                "set_window_topmost" => DesktopOperations.SetWindowTopMost(
                    ReadWindowCriteria(arguments, true),
                    ReadRequiredBool(arguments, "topMost"),
                    ReadMutationArtifactOptions(arguments)),
                "set_window_visibility" => DesktopOperations.SetWindowVisibility(
                    ReadWindowCriteria(arguments, true),
                    ReadRequiredBool(arguments, "visible"),
                    ReadMutationArtifactOptions(arguments)),
                "set_window_transparency" => DesktopOperations.SetWindowTransparency(
                    ReadWindowCriteria(arguments, true),
                    ReadByte(arguments, "alpha"),
                    ReadMutationArtifactOptions(arguments)),
                "snap_window" => DesktopOperations.SnapWindow(ReadWindowCriteria(arguments, true), ReadRequiredString(arguments, "position"), ReadMutationArtifactOptions(arguments)),
                "list_monitors" => DesktopOperations.ListMonitors(
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "get_monitor_brightness" => DesktopOperations.GetMonitorBrightness(
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "get_monitor_wallpaper" => DesktopOperations.GetMonitorWallpaper(
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "set_monitor_wallpaper" => DesktopOperations.SetMonitorWallpaper(
                    ReadOptionalString(arguments, "wallpaperPath"),
                    ReadOptionalString(arguments, "url"),
                    ReadWallpaperPosition(arguments, "position"),
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "set_monitor_brightness" => DesktopOperations.SetMonitorBrightness(
                    ReadInt(arguments, "brightness") ?? throw new CommandLineException("Property 'brightness' is required."),
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "set_monitor_position" => DesktopOperations.SetMonitorPosition(
                    ReadInt(arguments, "left") ?? throw new CommandLineException("Property 'left' is required."),
                    ReadInt(arguments, "top") ?? throw new CommandLineException("Property 'top' is required."),
                    ReadInt(arguments, "right") ?? throw new CommandLineException("Property 'right' is required."),
                    ReadInt(arguments, "bottom") ?? throw new CommandLineException("Property 'bottom' is required."),
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "set_monitor_resolution" => DesktopOperations.SetMonitorResolution(
                    ReadInt(arguments, "width") ?? throw new CommandLineException("Property 'width' is required."),
                    ReadInt(arguments, "height") ?? throw new CommandLineException("Property 'height' is required."),
                    ReadDisplayOrientation(arguments, "orientation"),
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "set_monitor_dpi_scaling" => DesktopOperations.SetMonitorDpiScaling(
                    ReadInt(arguments, "scalingPercent") ?? throw new CommandLineException("Property 'scalingPercent' is required."),
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "set_taskbar_position" => DesktopOperations.SetTaskbarPosition(
                    ReadTaskbarPosition(arguments, "position"),
                    ReadNullableBool(arguments, "visible"),
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "screenshot_desktop" => DesktopOperations.CaptureDesktopScreenshot(
                    ReadInt(arguments, "monitor"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName"),
                    ReadInt(arguments, "left"),
                    ReadInt(arguments, "top"),
                    ReadInt(arguments, "width"),
                    ReadInt(arguments, "height"),
                    ReadOptionalString(arguments, "outputPath")),
                "screenshot_window" => string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "targetName"))
                    ? DesktopOperations.CaptureWindowScreenshot(ReadWindowCriteria(arguments, true), ReadOptionalString(arguments, "outputPath"))
                    : DesktopOperations.CaptureWindowTargetScreenshot(ReadWindowCriteria(arguments, true), ReadRequiredString(arguments, "targetName"), ReadOptionalString(arguments, "outputPath")),
                "launch_process" => DesktopOperations.LaunchProcess(
                    ReadRequiredString(arguments, "filePath"),
                    ReadOptionalString(arguments, "arguments"),
                    ReadOptionalString(arguments, "workingDirectory"),
                    ReadInt(arguments, "waitForInputIdleMs"),
                    ReadInt(arguments, "waitForWindowMs"),
                    ReadInt(arguments, "waitForWindowIntervalMs"),
                    ReadOptionalString(arguments, "windowTitle"),
                    ReadOptionalString(arguments, "windowClassName"),
                    ReadBool(arguments, "requireWindow")),
                "set_clipboard_text" => DesktopOperations.SetClipboardText(
                    ReadRequiredString(arguments, "text"),
                    ReadInt(arguments, "retryCount"),
                    ReadInt(arguments, "retryDelayMs")),
                "launch_and_wait_for_window" => DesktopOperations.LaunchAndWaitForWindow(
                    ReadRequiredString(arguments, "filePath"),
                    ReadOptionalString(arguments, "arguments"),
                    ReadOptionalString(arguments, "workingDirectory"),
                    ReadInt(arguments, "waitForInputIdleMs"),
                    ReadInt(arguments, "launchWaitForWindowMs"),
                    ReadInt(arguments, "launchWaitForWindowIntervalMs"),
                    ReadOptionalString(arguments, "launchWindowTitle"),
                    ReadOptionalString(arguments, "launchWindowClass"),
                    ReadOptionalString(arguments, "windowTitle"),
                    ReadOptionalString(arguments, "windowClass"),
                    ReadBool(arguments, "includeHidden"),
                    ReadBool(arguments, "includeEmpty"),
                    ReadBool(arguments, "all"),
                    ReadBool(arguments, "followProcessFamily"),
                    ReadInt(arguments, "timeoutMs") ?? 10000,
                    ReadInt(arguments, "intervalMs") ?? 200,
                    ReadMutationArtifactOptions(arguments)),
                "list_named_targets" => DesktopOperations.ListWindowTargets(),
                "get_named_target" => DesktopOperations.GetWindowTarget(ReadRequiredString(arguments, "name")),
                "save_window_target" => DesktopOperations.SaveWindowTarget(
                    ReadRequiredString(arguments, "name"),
                    ReadOptionalString(arguments, "description"),
                    ReadInt(arguments, "x"),
                    ReadInt(arguments, "y"),
                    ReadDouble(arguments, "xRatio"),
                    ReadDouble(arguments, "yRatio"),
                    ReadInt(arguments, "width"),
                    ReadInt(arguments, "height"),
                    ReadDouble(arguments, "widthRatio"),
                    ReadDouble(arguments, "heightRatio"),
                    ReadBool(arguments, "clientArea")),
                "resolve_window_target" => DesktopOperations.ResolveWindowTargets(ReadWindowCriteria(arguments, true), ReadRequiredString(arguments, "name")),
                "list_named_control_targets" => DesktopOperations.ListControlTargets(),
                "get_named_control_target" => DesktopOperations.GetControlTarget(ReadRequiredString(arguments, "name")),
                "save_control_target" => DesktopOperations.SaveControlTarget(
                    ReadRequiredString(arguments, "name"),
                    ReadControlCriteria(arguments),
                    ReadOptionalString(arguments, "description")),
                "resolve_control_target" => DesktopOperations.ResolveControlTargets(
                    ReadWindowCriteria(arguments, true),
                    ReadRequiredString(arguments, "name"),
                    ReadBool(arguments, "allControls")),
                "list_window_controls" => string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "targetName"))
                    ? DesktopOperations.ListControls(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadControlCriteria(arguments),
                        ReadBool(arguments, "allWindows"))
                    : DesktopOperations.ListControlTargets(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadRequiredString(arguments, "targetName"),
                        ReadBool(arguments, "allWindows"),
                        ReadBool(arguments, "all")),
                "diagnose_window_controls" => string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "targetName"))
                    ? DesktopOperations.DiagnoseControls(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadControlCriteria(arguments),
                        ReadBool(arguments, "allWindows"),
                        ReadInt(arguments, "sampleLimit") ?? 10,
                        ReadBool(arguments, "includeActionProbe"))
                    : DesktopOperations.DiagnoseControlTargets(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadRequiredString(arguments, "targetName"),
                        ReadBool(arguments, "allWindows"),
                        ReadInt(arguments, "sampleLimit") ?? 10,
                        ReadBool(arguments, "includeActionProbe")),
                "control_exists" => string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "targetName"))
                    ? DesktopOperations.ControlExists(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadControlCriteria(arguments),
                        ReadBool(arguments, "allWindows"))
                    : DesktopOperations.ControlTargetExists(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadRequiredString(arguments, "targetName"),
                        ReadBool(arguments, "allWindows"),
                        ReadBool(arguments, "all")),
                "assert_control_value" => string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "targetName"))
                    ? DesktopOperations.AssertControlValue(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadControlCriteria(arguments),
                        ReadRequiredString(arguments, "expectedValue"),
                        ReadBool(arguments, "contains"),
                        ReadBool(arguments, "allWindows"))
                    : DesktopOperations.AssertControlTargetValue(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadRequiredString(arguments, "targetName"),
                        ReadRequiredString(arguments, "expectedValue"),
                        ReadBool(arguments, "contains"),
                        ReadBool(arguments, "allWindows"),
                        ReadBool(arguments, "all")),
                "wait_for_control" => string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "targetName"))
                    ? DesktopOperations.WaitForControl(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadControlCriteria(arguments),
                        ReadInt(arguments, "timeoutMs") ?? 10000,
                        ReadInt(arguments, "intervalMs") ?? 200,
                        ReadBool(arguments, "allWindows"))
                    : DesktopOperations.WaitForControlTarget(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadRequiredString(arguments, "targetName"),
                        ReadInt(arguments, "timeoutMs") ?? 10000,
                        ReadInt(arguments, "intervalMs") ?? 200,
                        ReadBool(arguments, "allWindows"),
                        ReadBool(arguments, "all")),
                "click_control" => string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "targetName"))
                    ? DesktopOperations.ClickControl(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadControlCriteria(arguments),
                        ReadOptionalString(arguments, "button") ?? "left",
                        ReadBool(arguments, "allWindows"),
                        ReadMutationArtifactOptions(arguments))
                    : DesktopOperations.ClickControlTarget(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadRequiredString(arguments, "targetName"),
                        ReadOptionalString(arguments, "button") ?? "left",
                        ReadBool(arguments, "allWindows"),
                        ReadBool(arguments, "all"),
                        ReadMutationArtifactOptions(arguments)),
                "focus_control" => DesktopOperations.FocusControl(
                    ReadRequiredString(arguments, "windowHandle"),
                    ReadRequiredString(arguments, "controlHandle"),
                    ReadBool(arguments, "ensureForegroundWindow")),
                "set_control_enabled" => DesktopOperations.SetControlEnabled(
                    ReadRequiredString(arguments, "windowHandle"),
                    ReadRequiredString(arguments, "controlHandle"),
                    ReadRequiredBool(arguments, "enabled")),
                "set_control_visibility" => DesktopOperations.SetControlVisibility(
                    ReadRequiredString(arguments, "windowHandle"),
                    ReadRequiredString(arguments, "controlHandle"),
                    ReadRequiredBool(arguments, "visible")),
                "set_control_text" => string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "targetName"))
                    ? DesktopOperations.SetControlText(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadControlCriteria(arguments),
                        ReadRequiredString(arguments, "text"),
                        ReadBool(arguments, "allWindows"),
                        ReadMutationArtifactOptions(arguments))
                    : DesktopOperations.SetControlTargetText(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadRequiredString(arguments, "targetName"),
                        ReadRequiredString(arguments, "text"),
                        ReadBool(arguments, "ensureForegroundWindow"),
                        ReadBool(arguments, "allowForegroundInput"),
                        ReadBool(arguments, "allWindows"),
                        ReadBool(arguments, "all"),
                        ReadMutationArtifactOptions(arguments)),
                "send_control_keys" => string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "targetName"))
                    ? DesktopOperations.SendControlKeys(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadControlCriteria(arguments),
                        ReadStringList(arguments, "keys"),
                        ReadBool(arguments, "allWindows"),
                        ReadMutationArtifactOptions(arguments))
                    : DesktopOperations.SendControlTargetKeys(
                        ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                        ReadRequiredString(arguments, "targetName"),
                        ReadStringList(arguments, "keys"),
                        ReadBool(arguments, "ensureForegroundWindow"),
                        ReadBool(arguments, "allowForegroundInput"),
                        ReadBool(arguments, "allWindows"),
                        ReadBool(arguments, "all"),
                        ReadMutationArtifactOptions(arguments)),
                "type_window_text" => DesktopOperations.TypeWindowText(
                    ReadWindowCriteria(arguments, true),
                    new WindowTextCommandOptions {
                        Text = ReadRequiredString(arguments, "text"),
                        Paste = ReadBool(arguments, "paste"),
                        DelayMilliseconds = ReadInt(arguments, "delayMs") ?? (ReadBool(arguments, "hostedSession") ? 35 : 0),
                        ForegroundInput = ReadBool(arguments, "foregroundInput") || ReadBool(arguments, "physicalKeys") || ReadBool(arguments, "hostedSession"),
                        PhysicalKeys = ReadBool(arguments, "physicalKeys"),
                        HostedSession = ReadBool(arguments, "hostedSession"),
                        ScriptMode = ReadBool(arguments, "script"),
                        ScriptChunkLength = ReadInt(arguments, "chunkSize") ?? 120,
                        ScriptLineDelayMilliseconds = ReadInt(arguments, "lineDelayMs") ?? (ReadBool(arguments, "hostedSession") && ReadBool(arguments, "script") ? 120 : 0)
                    },
                    ReadMutationArtifactOptions(arguments)),
                "send_window_keys" => DesktopOperations.SendWindowKeys(
                    ReadWindowCriteria(arguments, true),
                    ReadStringList(arguments, "keys"),
                    ReadNullableBool(arguments, "activate") ?? true,
                    ReadMutationArtifactOptions(arguments)),
                "list_named_layouts" => DesktopOperations.ListLayouts(),
                "save_current_layout" => DesktopOperations.SaveLayout(ReadRequiredString(arguments, "name")),
                "apply_named_layout" => DesktopOperations.ApplyLayout(ReadRequiredString(arguments, "name"), ReadBool(arguments, "validate")),
                "assert_window_layout" => DesktopOperations.AssertWindowLayout(
                    ReadRequiredString(arguments, "name"),
                    ReadInt(arguments, "positionTolerancePx") ?? 50,
                    ReadInt(arguments, "sizeTolerancePx") ?? 50,
                    ReadBool(arguments, "includeHidden"),
                    ReadBool(arguments, "includeEmpty"),
                    ReadNullableBool(arguments, "checkState") ?? true,
                    ReadMutationArtifactOptions(arguments)),
                "list_named_snapshots" => DesktopOperations.ListSnapshots(),
                "save_current_snapshot" => DesktopOperations.SaveSnapshot(ReadRequiredString(arguments, "name")),
                "restore_saved_snapshot" => DesktopOperations.RestoreSnapshot(ReadRequiredString(arguments, "name"), ReadBool(arguments, "validate")),
                "prepare_for_coding" => DesktopOperations.PrepareForCoding(
                    ReadOptionalString(arguments, "layoutName"),
                    ReadWorkflowFocusCriteria(arguments),
                    ReadMutationArtifactOptions(arguments)),
                "prepare_for_screen_sharing" => DesktopOperations.PrepareForScreenSharing(
                    ReadOptionalString(arguments, "layoutName"),
                    ReadWorkflowFocusCriteria(arguments),
                    ReadMutationArtifactOptions(arguments)),
                "clean_up_distractions" => DesktopOperations.CleanUpDistractions(ReadMutationArtifactOptions(arguments)),
                _ => throw new CommandLineException($"Unknown tool '{name}'.")
            };
            error = null;
            return true;
        } catch (CommandLineException ex) {
            result = new { error = ex.Message };
            error = ex.Message;
            return false;
        }
    }

    public static object ReadResource(string uri) {
        return uri switch {
            "desktop://monitors" => DesktopOperations.ListMonitors(connectedOnly: true),
            "desktop://windows/visible" => DesktopOperations.ListWindows(new WindowSelectionCriteria()),
            "desktop://windows/active" => DesktopOperations.GetActiveWindow(),
            "desktop://layouts" => DesktopOperations.ListLayouts(),
            "desktop://targets" => DesktopOperations.ListWindowTargets(),
            "desktop://control-targets" => DesktopOperations.ListControlTargets(),
            "desktop://snapshot/current" => DesktopOperations.GetCurrentSnapshotSummary(),
            _ => throw new CommandLineException($"Unknown resource '{uri}'.")
        };
    }

    public static object GetPrompt(string name, JsonElement arguments) {
        string? layoutName = ReadOptionalString(arguments, "layoutName");
        return name switch {
            "prepare_for_coding" => BuildPrompt("Prepare the desktop for focused coding work.", layoutName, "Start by listing named layouts. If the requested layout exists, apply it. Then inspect visible windows and focus the main editor or terminal window. If the layout is missing, explain the gap and suggest the nearest saved layout."),
            "prepare_for_screen_sharing" => BuildPrompt("Prepare the desktop for a clean screen sharing session.", layoutName, "Start by listing named layouts. If the requested layout exists, apply it. Then inspect visible windows, minimize obviously distracting windows, and focus the application that should be shared."),
            "clean_up_distractions" => BuildPrompt("Clean up distracting windows before focused work.", null, "Inspect visible windows first. Minimize obvious distractions such as chat, mail, or utility windows when appropriate, but avoid closing anything. Explain what changed."),
            _ => throw new CommandLineException($"Unknown prompt '{name}'.")
        };
    }

    private static object BuildPrompt(string summary, string? layoutName, string instructions) {
        string layoutText = string.IsNullOrWhiteSpace(layoutName) ? "No preferred layout was provided." : $"Preferred layout: {layoutName}.";
        return new {
            description = summary,
            messages = new[] {
                new {
                    role = "user",
                    content = new {
                        type = "text",
                        text = $"{summary} {layoutText} {instructions}"
                    }
                }
            }
        };
    }

    private static WindowSelectionCriteria ReadWorkflowFocusCriteria(JsonElement element) {
        return new WindowSelectionCriteria {
            TitlePattern = ReadOptionalString(element, "windowTitle") ?? "*",
            ProcessNamePattern = ReadOptionalString(element, "processName") ?? "*",
            ClassNamePattern = ReadOptionalString(element, "className") ?? "*",
            ProcessId = ReadInt(element, "processId"),
            Handle = ReadOptionalString(element, "handle"),
            Active = ReadBool(element, "activeWindow"),
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = false,
            All = false
        };
    }

    private static WindowSelectionCriteria ReadWindowCriteria(JsonElement element, bool includeEmptyDefault) {
        return ReadWindowCriteria(element, includeEmptyDefault, "windowTitle", "processName", "className", "processId", "handle");
    }

    private static WindowSelectionCriteria ReadWindowCriteria(JsonElement element, bool includeEmptyDefault, string titleProperty, string processNameProperty, string classNameProperty, string processIdProperty, string handleProperty) {
        return new WindowSelectionCriteria {
            TitlePattern = ReadOptionalString(element, titleProperty) ?? "*",
            ProcessNamePattern = ReadOptionalString(element, processNameProperty) ?? "*",
            ClassNamePattern = ReadOptionalString(element, classNameProperty) ?? "*",
            ProcessId = ReadInt(element, processIdProperty),
            Handle = ReadOptionalString(element, handleProperty),
            Active = ReadBool(element, "activeWindow"),
            IncludeHidden = ReadBool(element, "includeHidden"),
            IncludeCloaked = !ReadBool(element, "excludeCloaked"),
            IncludeOwned = !ReadBool(element, "excludeOwned"),
            IncludeEmptyTitles = ReadNullableBool(element, "includeEmpty") ?? includeEmptyDefault,
            All = ReadBool(element, "all")
        };
    }

    private static object StopWindowKeepAlive(JsonElement arguments) {
        bool allSessions = ReadBool(arguments, "allSessions");
        if (allSessions) {
            if (HasWindowSelector(arguments) || ReadBool(arguments, "all")) {
                throw new CommandLineException("Cannot combine 'allSessions' with window selectors or 'all'.");
            }

            return DesktopOperations.StopAllWindowKeepAlive();
        }

        return DesktopOperations.StopWindowKeepAlive(ReadWindowCriteria(arguments, true));
    }

    private static bool HasWindowSelector(JsonElement arguments) {
        return !string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "windowTitle")) ||
               !string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "processName")) ||
               !string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "className")) ||
               ReadInt(arguments, "processId").HasValue ||
               !string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "handle")) ||
               ReadBool(arguments, "activeWindow") ||
               ReadNullableBool(arguments, "includeEmpty").HasValue ||
               ReadBool(arguments, "includeHidden") ||
               ReadBool(arguments, "excludeCloaked") ||
               ReadBool(arguments, "excludeOwned");
    }

    private static ControlSelectionCriteria ReadControlCriteria(JsonElement element) {
        return new ControlSelectionCriteria {
            ClassNamePattern = ReadOptionalString(element, "controlClassName") ?? "*",
            TextPattern = ReadOptionalString(element, "controlText") ?? "*",
            ValuePattern = ReadOptionalString(element, "controlValue") ?? "*",
            Id = ReadInt(element, "controlId"),
            Handle = ReadOptionalString(element, "controlHandle"),
            AutomationIdPattern = ReadOptionalString(element, "controlAutomationId") ?? "*",
            ControlTypePattern = ReadOptionalString(element, "controlType") ?? "*",
            FrameworkIdPattern = ReadOptionalString(element, "controlFrameworkId") ?? "*",
            IsEnabled = ReadNullableBool(element, "isEnabled"),
            IsKeyboardFocusable = ReadNullableBool(element, "isKeyboardFocusable"),
            SupportsBackgroundClick = ReadNullableBool(element, "supportsBackgroundClick"),
            SupportsBackgroundText = ReadNullableBool(element, "supportsBackgroundText"),
            SupportsBackgroundKeys = ReadNullableBool(element, "supportsBackgroundKeys"),
            SupportsForegroundInputFallback = ReadNullableBool(element, "supportsForegroundInputFallback"),
            EnsureForegroundWindow = ReadBool(element, "ensureForegroundWindow"),
            AllowForegroundInputFallback = ReadBool(element, "allowForegroundInput"),
            UiAutomation = ReadBool(element, "uiAutomation"),
            IncludeUiAutomation = ReadBool(element, "includeUiAutomation"),
            All = ReadBool(element, "all")
        };
    }

    private static object CallClickWindowPoint(JsonElement arguments) {
        WindowSelectionCriteria criteria = ReadWindowCriteria(arguments, true);
        string? targetName = ReadOptionalString(arguments, "targetName");
        if (!string.IsNullOrWhiteSpace(targetName)) {
            return DesktopOperations.ClickWindowTarget(
                criteria,
                targetName,
                ReadOptionalString(arguments, "button") ?? "left",
                ReadBool(arguments, "activate"),
                ReadMutationArtifactOptions(arguments));
        }

        return DesktopOperations.ClickWindowPoint(
            criteria,
            ReadInt(arguments, "x"),
            ReadInt(arguments, "y"),
            ReadDouble(arguments, "xRatio"),
            ReadDouble(arguments, "yRatio"),
            ReadOptionalString(arguments, "button") ?? "left",
            ReadBool(arguments, "activate"),
            ReadBool(arguments, "clientArea"),
            ReadMutationArtifactOptions(arguments));
    }

    private static object CallDragWindowPoints(JsonElement arguments) {
        WindowSelectionCriteria criteria = ReadWindowCriteria(arguments, true);
        string? startTargetName = ReadOptionalString(arguments, "startTargetName");
        if (!string.IsNullOrWhiteSpace(startTargetName)) {
            return DesktopOperations.DragWindowTargets(
                criteria,
                startTargetName,
                ReadRequiredString(arguments, "endTargetName"),
                ReadOptionalString(arguments, "button") ?? "left",
                ReadInt(arguments, "stepDelayMs") ?? 0,
                ReadBool(arguments, "activate"),
                ReadMutationArtifactOptions(arguments));
        }

        return DesktopOperations.DragWindowPoints(
            criteria,
            ReadInt(arguments, "startX"),
            ReadInt(arguments, "startY"),
            ReadDouble(arguments, "startXRatio"),
            ReadDouble(arguments, "startYRatio"),
            ReadInt(arguments, "endX"),
            ReadInt(arguments, "endY"),
            ReadDouble(arguments, "endXRatio"),
            ReadDouble(arguments, "endYRatio"),
            ReadOptionalString(arguments, "button") ?? "left",
            ReadInt(arguments, "stepDelayMs") ?? 0,
            ReadBool(arguments, "activate"),
            ReadBool(arguments, "clientArea"),
            ReadMutationArtifactOptions(arguments));
    }

    private static object CallScrollWindowPoint(JsonElement arguments) {
        WindowSelectionCriteria criteria = ReadWindowCriteria(arguments, true);
        string? targetName = ReadOptionalString(arguments, "targetName");
        int delta = ReadInt(arguments, "delta") ?? throw new CommandLineException("Property 'delta' is required.");
        if (!string.IsNullOrWhiteSpace(targetName)) {
            return DesktopOperations.ScrollWindowTarget(
                criteria,
                targetName,
                delta,
                ReadBool(arguments, "activate"),
                ReadMutationArtifactOptions(arguments));
        }

        return DesktopOperations.ScrollWindowPoint(
            criteria,
            ReadInt(arguments, "x"),
            ReadInt(arguments, "y"),
            ReadDouble(arguments, "xRatio"),
            ReadDouble(arguments, "yRatio"),
            delta,
            ReadBool(arguments, "activate"),
            ReadBool(arguments, "clientArea"),
            ReadMutationArtifactOptions(arguments));
    }

    private static object CreateTool(string name, string title, string description, object inputSchema, bool readOnly, bool destructive = false, bool idempotent = false) {
        return new {
            name,
            title,
            description,
            inputSchema,
            annotations = new {
                title,
                readOnlyHint = readOnly,
                destructiveHint = destructive,
                idempotentHint = idempotent,
                openWorldHint = false
            }
        };
    }

    private static object CreateWindowSelectorSchema(bool includeAll, bool includeEmpty) {
        var properties = new Dictionary<string, object> {
            ["windowTitle"] = CreateStringSchema("Window title filter."),
            ["processName"] = CreateStringSchema("Process name filter."),
            ["className"] = CreateStringSchema("Window class filter."),
            ["processId"] = CreateIntegerSchema("Process identifier."),
            ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
            ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
            ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
            ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
            ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows.")
        };

        if (includeEmpty) {
            properties["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles.");
        }

        if (includeAll) {
            properties["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.");
        }

        return CreateObjectSchema(properties);
    }

    private static object CreateWindowMutationSelectorSchema(bool includeAll, bool includeEmpty) {
        var properties = new Dictionary<string, object> {
            ["windowTitle"] = CreateStringSchema("Window title filter."),
            ["processName"] = CreateStringSchema("Process name filter."),
            ["className"] = CreateStringSchema("Window class filter."),
            ["processId"] = CreateIntegerSchema("Process identifier."),
            ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
            ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
            ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
            ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
            ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows.")
        };

        if (includeEmpty) {
            properties["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles.");
        }

        if (includeAll) {
            properties["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.");
        }

        return CreateObjectSchema(AddMutationArtifactProperties(properties));
    }

    private static Dictionary<string, object> CreateMonitorSelectorProperties() {
        return new Dictionary<string, object> {
            ["connectedOnly"] = CreateBooleanSchema("Return only connected monitors."),
            ["primaryOnly"] = CreateBooleanSchema("Return only the primary monitor."),
            ["index"] = CreateIntegerSchema("Specific monitor index to return."),
            ["deviceId"] = CreateStringSchema("Specific monitor device identifier to return."),
            ["deviceName"] = CreateStringSchema("Specific monitor device name to return.")
        };
    }

    private static Dictionary<string, object> CreateMonitorMutationProperties(Dictionary<string, object> properties) {
        foreach (KeyValuePair<string, object> property in CreateMonitorSelectorProperties()) {
            properties[property.Key] = property.Value;
        }

        return properties;
    }

    private static Dictionary<string, object> AddMutationArtifactProperties(Dictionary<string, object> properties) {
        properties["captureBefore"] = CreateBooleanSchema("Capture a best-effort screenshot before the mutation.");
        properties["captureAfter"] = CreateBooleanSchema("Capture a best-effort screenshot after the mutation.");
        properties["artifactDirectory"] = CreateStringSchema("Optional directory for mutation screenshots.");
        properties["verifyAfter"] = CreateBooleanSchema("Re-query the mutated target and report the observed postcondition after the mutation.");
        properties["verificationTolerancePixels"] = CreateIntegerSchema("Optional geometry verification tolerance in pixels. Providing it also enables post-mutation verification.");
        return properties;
    }

    private static object CreateObjectSchema(Dictionary<string, object>? properties = null, string[]? required = null) {
        return new {
            type = "object",
            properties = properties ?? new Dictionary<string, object>(),
            required = required ?? Array.Empty<string>()
        };
    }

    private static object CreateStringSchema(string description) {
        return new {
            type = "string",
            description
        };
    }

    private static object CreateIntegerSchema(string description) {
        return new {
            type = "integer",
            description
        };
    }

    private static object CreateNumberSchema(string description) {
        return new {
            type = "number",
            description
        };
    }

    private static object CreateBooleanSchema(string description) {
        return new {
            type = "boolean",
            description
        };
    }

    private static object CreateArraySchema(string description, object items) {
        return new {
            type = "array",
            description,
            items
        };
    }

    private static string[] ExtractProcessPatternsFromFilePath(string filePath) {
        string trimmed = filePath.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) {
            return Array.Empty<string>();
        }

        string leaf = trimmed;
        int lastSlash = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
        if (lastSlash >= 0 && lastSlash < trimmed.Length - 1) {
            leaf = trimmed.Substring(lastSlash + 1);
        }

        if (string.IsNullOrWhiteSpace(leaf)) {
            return Array.Empty<string>();
        }

        string withoutExtension = leaf.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? leaf.Substring(0, leaf.Length - 4)
            : leaf;

        if (string.Equals(leaf, withoutExtension, StringComparison.OrdinalIgnoreCase)) {
            return new[] { leaf };
        }

        return new[] { leaf, withoutExtension };
    }

    private static string ReadRequiredString(JsonElement element, string propertyName) {
        return ReadOptionalString(element, propertyName) ?? throw new CommandLineException($"Property '{propertyName}' is required.");
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static IReadOnlyList<string> ReadStringList(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return Array.Empty<string>();
        }

        if (property.ValueKind == JsonValueKind.Array) {
            List<string> values = new();
            foreach (JsonElement item in property.EnumerateArray()) {
                if (item.ValueKind == JsonValueKind.Null) {
                    continue;
                }

                values.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.ToString());
            }

            return values;
        }

        string? single = ReadOptionalString(element, propertyName);
        return string.IsNullOrWhiteSpace(single) ? Array.Empty<string>() : new[] { single };
    }

    private static int? ReadInt(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int numericValue)) {
            return numericValue;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out int textValue)) {
            return textValue;
        }

        throw new CommandLineException($"Property '{propertyName}' expects an integer value.");
    }

    private static int? ReadPositiveInteger(JsonElement element, string propertyName) {
        int? value = ReadInt(element, propertyName);
        if (value.HasValue && value.Value <= 0) {
            throw new CommandLineException($"Property '{propertyName}' expects a value greater than 0.");
        }

        return value;
    }

    private static double? ReadDouble(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out double numericValue)) {
            return numericValue;
        }

        if (property.ValueKind == JsonValueKind.String && double.TryParse(property.GetString(), out double textValue)) {
            return textValue;
        }

        throw new CommandLineException($"Property '{propertyName}' expects a numeric value.");
    }

    private static byte ReadByte(JsonElement element, string propertyName) {
        int value = ReadInt(element, propertyName) ?? throw new CommandLineException($"Property '{propertyName}' is required.");
        if (value < byte.MinValue || value > byte.MaxValue) {
            throw new CommandLineException($"Property '{propertyName}' expects a value from 0 to 255.");
        }

        return (byte)value;
    }

    private static DisplayOrientation? ReadDisplayOrientation(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseOptionalDisplayOrientation(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static uint ReadColor(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseRequiredColor(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static bool ReadBool(JsonElement element, string propertyName) {
        return ReadNullableBool(element, propertyName) ?? false;
    }

    private static bool ReadRequiredBool(JsonElement element, string propertyName) {
        bool? value = ReadNullableBool(element, propertyName);
        return value ?? throw new CommandLineException($"Property '{propertyName}' is required.");
    }

    private static MutationArtifactOptions? ReadMutationArtifactOptions(JsonElement element) {
        bool captureBefore = ReadBool(element, "captureBefore");
        bool captureAfter = ReadBool(element, "captureAfter");
        string? artifactDirectory = ReadOptionalString(element, "artifactDirectory");
        bool verifyAfter = ReadBool(element, "verifyAfter") || ReadInt(element, "verificationTolerancePixels").HasValue;
        int verificationTolerancePixels = ReadInt(element, "verificationTolerancePixels") ?? 10;
        if (!captureBefore && !captureAfter && string.IsNullOrWhiteSpace(artifactDirectory) && !verifyAfter) {
            return null;
        }

        return new MutationArtifactOptions {
            CaptureBefore = captureBefore,
            CaptureAfter = captureAfter,
            ArtifactDirectory = artifactDirectory,
            VerifyAfter = verifyAfter,
            VerificationTolerancePixels = verificationTolerancePixels
        };
    }

    private static bool? ReadNullableBool(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        if (property.ValueKind == JsonValueKind.True) {
            return true;
        }

        if (property.ValueKind == JsonValueKind.False) {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out bool parsed)) {
            return parsed;
        }

        throw new CommandLineException($"Property '{propertyName}' expects a boolean value.");
    }

    private static TaskbarPosition? ReadTaskbarPosition(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseOptionalTaskbarPosition(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static DesktopWallpaperPosition? ReadWallpaperPosition(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseOptionalWallpaperPosition(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static DesktopSlideshowDirection? ReadSlideshowDirection(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseOptionalSlideshowDirection(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static bool TryReadProperty(JsonElement element, string propertyName, out JsonElement property) {
        property = default;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out property);
    }
}
