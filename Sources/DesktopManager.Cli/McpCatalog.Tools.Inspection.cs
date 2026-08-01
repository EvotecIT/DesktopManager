using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static partial class McpCatalog {
    private static McpToolDefinition[] CreateTools() {
        return CreateInspectionAndControlTools()
            .Concat(CreateWindowAndStorageTools())
            .Concat(CreateDesktopStateTools())
            .Concat(CreateDeviceManagementTools())
            .ToArray();
    }

    private static McpToolDefinition[] CreateInspectionAndControlTools() {
        return new McpToolDefinition[] {
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
            CreateTool("wait_for_window_visual_change", "Wait For Window Visual Change", "Wait until a matching window, client area, or saved target region changes visually.", CreateObjectSchema(
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
                    ["targetName"] = CreateStringSchema("Saved reusable target name to monitor instead of the full window."),
                    ["clientArea"] = CreateBooleanSchema("Compare the window client area when no target name is provided."),
                    ["minimumChangedRatio"] = CreateNumberSchema("Minimum changed-sample ratio from 0 to 1 required to treat the window as visually changed."),
                    ["differenceThreshold"] = CreateIntegerSchema("Per-sample RGB difference threshold from 0 to 255."),
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
            CreateTool("get_focused_control", "Get Focused Control", "Return focused-control metadata and bounded plain text for a matching window. UI Automation password controls are never read.", CreateObjectSchema(
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
                    ["expectedText"] = CreateStringSchema("Optional text to search for across the complete UI Automation document range."),
                    ["maxLength"] = CreateIntegerSchema("Maximum focused-control value length.")
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
            CreateTool("set_control_check_state", "Set Control Check State", "Check or uncheck a specific checkbox or radio button control handle.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowHandle"] = CreateStringSchema("Parent window handle in decimal or hexadecimal format."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["checked"] = CreateBooleanSchema("True to check the control; false to clear it.")
                }), new[] { "windowHandle", "controlHandle", "checked" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_matching_control_check_state", "Set Matching Control Check State", "Check or uncheck matching checkbox-style controls using window and control selectors.", CreateObjectSchema(
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
                    ["all"] = CreateBooleanSchema("Apply to all matching controls."),
                    ["allWindows"] = CreateBooleanSchema("Target controls in all matching windows."),
                    ["checked"] = CreateBooleanSchema("True to check the control; false to clear it.")
                }), new[] { "checked" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_control_selected_value", "Set Control Selected Value", "Select a combo-box item by its displayed text for a specific control handle.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowHandle"] = CreateStringSchema("Parent window handle in decimal or hexadecimal format."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["selectedValue"] = CreateStringSchema("Displayed item text to select.")
                }), new[] { "windowHandle", "controlHandle", "selectedValue" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_matching_control_selected_value", "Set Matching Control Selected Value", "Select a displayed item on matching combo-box-style controls using window and control selectors.", CreateObjectSchema(
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
                    ["all"] = CreateBooleanSchema("Apply to all matching controls."),
                    ["allWindows"] = CreateBooleanSchema("Target controls in all matching windows."),
                    ["selectedValue"] = CreateStringSchema("Displayed item text to select.")
                }), new[] { "selectedValue" }), readOnly: false, destructive: false, idempotent: true),
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
        };
    }
}
