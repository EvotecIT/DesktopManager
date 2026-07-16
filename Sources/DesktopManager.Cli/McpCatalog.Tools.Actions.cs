using System.Collections.Generic;

namespace DesktopManager.Cli;

internal static partial class McpCatalog {
    private static McpToolDefinition[] CreateWindowAndStorageTools() {
        return new McpToolDefinition[] {
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
            CreateTool("place_window", "Place Window", "Apply reliable semantic monitor placement to one or more matching windows.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["placement"] = CreateStringSchema("Placement: restore, maximize, left-half, right-half, or exact-rectangle."),
                    ["monitorTarget"] = CreateStringSchema("Semantic monitor target: current, top-left, top-right, bottom-left, or bottom-right."),
                    ["monitor"] = CreateIntegerSchema("Explicit target monitor index."),
                    ["x"] = CreateIntegerSchema("Exact rectangle left coordinate."),
                    ["y"] = CreateIntegerSchema("Exact rectangle top coordinate."),
                    ["width"] = CreateIntegerSchema("Exact rectangle width."),
                    ["height"] = CreateIntegerSchema("Exact rectangle height."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), new[] { "placement" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("click_window_point", "Click Window Point", "Click a point relative to a matching window.", CreateObjectSchema(
                AddMutationArtifactProperties(new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["targetName"] = CreateStringSchema("Saved reusable target name."),
                    ["visualBaselineName"] = CreateStringSchema("Saved visual baseline name used as a reusable anchor."),
                    ["ocrText"] = CreateStringSchema("Visible text to resolve through Windows OCR before clicking."),
                    ["ocrTargetName"] = CreateStringSchema("Optional saved target name that narrows the OCR search region."),
                    ["ocrContains"] = CreateBooleanSchema("Treat the OCR text query as a substring instead of requiring an exact match."),
                    ["ocrLanguageTag"] = CreateStringSchema("Optional OCR language tag such as en-US."),
                    ["x"] = CreateIntegerSchema("Horizontal coordinate relative to the window bounds."),
                    ["y"] = CreateIntegerSchema("Vertical coordinate relative to the window bounds."),
                    ["xRatio"] = CreateNumberSchema("Horizontal coordinate ratio from 0 to 1."),
                    ["yRatio"] = CreateNumberSchema("Vertical coordinate ratio from 0 to 1."),
                    ["button"] = CreateStringSchema("Mouse button: left or right."),
                    ["activate"] = CreateBooleanSchema("Activate the window before clicking."),
                    ["clientArea"] = CreateBooleanSchema("Interpret coordinates relative to the window client area."),
                    ["baselineMaxAverageDifference"] = CreateNumberSchema("Maximum sampled average difference allowed when resolving a saved visual baseline."),
                    ["baselineDifferenceThreshold"] = CreateIntegerSchema("Per-sample average channel difference that counts as a changed pixel while resolving a saved visual baseline."),
                    ["baselineScanStep"] = CreateIntegerSchema("Coarse scan step used while resolving a saved visual baseline."),
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
                    ["startVisualBaselineName"] = CreateStringSchema("Saved visual baseline name used as the reusable starting anchor."),
                    ["endVisualBaselineName"] = CreateStringSchema("Saved visual baseline name used as the reusable ending anchor."),
                    ["startOcrText"] = CreateStringSchema("Visible text to resolve through Windows OCR before choosing the starting drag point."),
                    ["endOcrText"] = CreateStringSchema("Visible text to resolve through Windows OCR before choosing the ending drag point."),
                    ["startOcrTargetName"] = CreateStringSchema("Optional saved target name that narrows the OCR search region for the starting point."),
                    ["endOcrTargetName"] = CreateStringSchema("Optional saved target name that narrows the OCR search region for the ending point."),
                    ["ocrContains"] = CreateBooleanSchema("Treat the OCR text queries as substrings instead of requiring exact matches."),
                    ["ocrLanguageTag"] = CreateStringSchema("Optional OCR language tag such as en-US."),
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
                    ["baselineMaxAverageDifference"] = CreateNumberSchema("Maximum sampled average difference allowed when resolving saved visual baselines."),
                    ["baselineDifferenceThreshold"] = CreateIntegerSchema("Per-sample average channel difference that counts as a changed pixel while resolving saved visual baselines."),
                    ["baselineScanStep"] = CreateIntegerSchema("Coarse scan step used while resolving saved visual baselines."),
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
                    ["visualBaselineName"] = CreateStringSchema("Saved visual baseline name used as a reusable anchor."),
                    ["ocrText"] = CreateStringSchema("Visible text to resolve through Windows OCR before scrolling."),
                    ["ocrTargetName"] = CreateStringSchema("Optional saved target name that narrows the OCR search region."),
                    ["ocrContains"] = CreateBooleanSchema("Treat the OCR text query as a substring instead of requiring an exact match."),
                    ["ocrLanguageTag"] = CreateStringSchema("Optional OCR language tag such as en-US."),
                    ["x"] = CreateIntegerSchema("Horizontal coordinate relative to the window bounds."),
                    ["y"] = CreateIntegerSchema("Vertical coordinate relative to the window bounds."),
                    ["xRatio"] = CreateNumberSchema("Horizontal coordinate ratio from 0 to 1."),
                    ["yRatio"] = CreateNumberSchema("Vertical coordinate ratio from 0 to 1."),
                    ["delta"] = CreateIntegerSchema("Scroll delta. Positive scrolls up."),
                    ["activate"] = CreateBooleanSchema("Activate the window before scrolling."),
                    ["clientArea"] = CreateBooleanSchema("Interpret coordinates relative to the window client area."),
                    ["baselineMaxAverageDifference"] = CreateNumberSchema("Maximum sampled average difference allowed when resolving a saved visual baseline."),
                    ["baselineDifferenceThreshold"] = CreateIntegerSchema("Per-sample average channel difference that counts as a changed pixel while resolving a saved visual baseline."),
                    ["baselineScanStep"] = CreateIntegerSchema("Coarse scan step used while resolving a saved visual baseline."),
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
            CreateTool("get_monitor_advanced_color", "Get Monitor Advanced Color", "Return HDR and advanced-color state for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorSelectorProperties()), readOnly: true),
            CreateTool("get_monitor_wallpaper", "Get Monitor Wallpaper", "Return wallpaper paths for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorSelectorProperties()), readOnly: true),
            CreateTool("set_monitor_wallpaper", "Set Monitor Wallpaper", "Set wallpaper for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["wallpaperPath"] = CreateStringSchema("Wallpaper file path."),
                    ["url"] = CreateStringSchema("Wallpaper URL."),
                    ["position"] = CreateStringSchema("Optional wallpaper position: center, tile, stretch, fit, fill, or span.")
                })), readOnly: false, destructive: false, idempotent: true, openWorld: true),
            CreateTool("set_monitor_brightness", "Set Monitor Brightness", "Set brightness for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["brightness"] = CreateIntegerSchema("Brightness level to apply from 0 to 100.")
                }), new[] { "brightness" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_monitor_hdr", "Set Monitor HDR", "Enable or disable HDR for one or more matching monitors when supported.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["enabled"] = CreateBooleanSchema("True to enable HDR; false to disable it.")
                }), new[] { "enabled" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_monitor_position", "Set Monitor Position", "Set the top-left coordinate for one or more matching monitors without changing resolution.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["left"] = CreateIntegerSchema("Monitor left coordinate."),
                    ["top"] = CreateIntegerSchema("Monitor top coordinate.")
                }), new[] { "left", "top" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_monitor_resolution", "Set Monitor Resolution", "Set monitor resolution and optional orientation for one or more matching monitors.", CreateObjectSchema(
                CreateMonitorMutationProperties(new Dictionary<string, object> {
                    ["width"] = CreateIntegerSchema("Monitor width in pixels."),
                    ["height"] = CreateIntegerSchema("Monitor height in pixels."),
                    ["orientation"] = CreateStringSchema("Optional display orientation: default, degrees90, degrees180, or degrees270.")
                }), new[] { "width", "height" }), readOnly: false, destructive: false, idempotent: true),
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
                }), readOnly: false, destructive: false, idempotent: false),
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
                }), readOnly: false, destructive: false, idempotent: false),
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
            CreateTool("list_named_visual_baselines", "List Named Visual Baselines", "List saved reusable visual baselines.", CreateObjectSchema(), readOnly: true),
            CreateTool("get_named_visual_baseline", "Get Named Visual Baseline", "Get a saved visual baseline definition and image metadata.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Visual baseline name.")
                }, new[] { "name" }), readOnly: true),
            CreateTool("save_visual_baseline", "Save Visual Baseline", "Save a reusable visual baseline from a live window, client area, or named target region.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Visual baseline name."),
                    ["description"] = CreateStringSchema("Optional baseline description."),
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["targetName"] = CreateStringSchema("Optional saved target name to capture instead of the whole window."),
                    ["clientArea"] = CreateBooleanSchema("Capture the client area when no saved target is supplied.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("assert_visual_baseline", "Assert Visual Baseline", "Compare a live window, client area, or named target region against a saved visual baseline.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Visual baseline name."),
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["targetName"] = CreateStringSchema("Optional saved target name to compare instead of the stored baseline target."),
                    ["clientArea"] = CreateBooleanSchema("Compare the client area when no saved target is supplied."),
                    ["maxChangedRatio"] = CreateNumberSchema("Maximum sampled changed-pixel ratio allowed for a successful match."),
                    ["differenceThreshold"] = CreateIntegerSchema("Per-sample average channel difference that counts as a visual change.")
                }, new[] { "name" }), readOnly: true),
            CreateTool("resolve_visual_baseline", "Resolve Visual Baseline", "Search a live window or client area for the best location of a saved visual baseline image.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Visual baseline name."),
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["clientArea"] = CreateBooleanSchema("Search inside the client area instead of the whole window."),
                    ["maxAverageDifference"] = CreateNumberSchema("Maximum sampled average difference allowed for a successful match."),
                    ["differenceThreshold"] = CreateIntegerSchema("Per-sample average channel difference that counts as a changed pixel."),
                    ["scanStep"] = CreateIntegerSchema("Coarse scan step used during the initial search.")
                }, new[] { "name" }), readOnly: true),
            CreateTool("read_window_text", "Read Window Text", "Run Windows OCR over a live window, client area, or named target region and return recognized text with bounds.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["targetName"] = CreateStringSchema("Optional saved target name to OCR instead of the whole window."),
                    ["clientArea"] = CreateBooleanSchema("Read text from the client area when no saved target is supplied."),
                    ["languageTag"] = CreateStringSchema("Optional OCR language tag such as en-US.")
                }), readOnly: true),
            CreateTool("resolve_window_text", "Resolve Window Text", "Run Windows OCR over a live window capture and return the best match coordinates for visible text.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["queryText"] = CreateStringSchema("Visible text to resolve."),
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["targetName"] = CreateStringSchema("Optional saved target name to OCR instead of the whole window."),
                    ["clientArea"] = CreateBooleanSchema("Read text from the client area when no saved target is supplied."),
                    ["contains"] = CreateBooleanSchema("Treat the query text as a substring instead of requiring an exact OCR match."),
                    ["languageTag"] = CreateStringSchema("Optional OCR language tag such as en-US.")
                }, new[] { "queryText" }), readOnly: true),
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
}
