using System.Text.Json;

namespace DesktopManager.Cli;

internal static partial class McpCatalog {
    public static bool TryCallTool(string name, JsonElement arguments, out object result, out string? error) {
        try {
            if (TryCallDesktopStateTool(name, arguments, out result)) {
                error = null;
                return true;
            }

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
                "wait_for_window_visual_change" => DesktopOperations.WaitForWindowVisualChange(
                    ReadWindowCriteria(arguments, true),
                    ReadOptionalString(arguments, "targetName"),
                    ReadBool(arguments, "clientArea"),
                    ReadInt(arguments, "timeoutMs") ?? 10000,
                    ReadInt(arguments, "intervalMs") ?? 200,
                    ReadDouble(arguments, "minimumChangedRatio") ?? 0.01,
                    ReadInt(arguments, "differenceThreshold") ?? 24),
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
                "place_window" => DesktopOperations.PlaceWindow(
                    ReadWindowCriteria(arguments, true),
                    ReadWindowPlacement(arguments, "placement"),
                    ReadWindowMonitorTarget(arguments, "monitorTarget"),
                    ReadInt(arguments, "monitor"),
                    ReadInt(arguments, "x"),
                    ReadInt(arguments, "y"),
                    ReadInt(arguments, "width"),
                    ReadInt(arguments, "height"),
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
                "get_monitor_advanced_color" => DesktopOperations.GetMonitorAdvancedColor(
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
                "set_monitor_hdr" => DesktopOperations.SetMonitorHdr(
                    ReadRequiredBool(arguments, "enabled"),
                    ReadNullableBool(arguments, "connectedOnly"),
                    ReadNullableBool(arguments, "primaryOnly"),
                    ReadInt(arguments, "index"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName")),
                "set_monitor_position" => DesktopOperations.SetMonitorPosition(
                    ReadInt(arguments, "left") ?? throw new CommandLineException("Property 'left' is required."),
                    ReadInt(arguments, "top") ?? throw new CommandLineException("Property 'top' is required."),
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
                "list_named_visual_baselines" => DesktopOperations.ListVisualBaselines(),
                "get_named_visual_baseline" => DesktopOperations.GetVisualBaseline(ReadRequiredString(arguments, "name")),
                "save_visual_baseline" => DesktopOperations.SaveVisualBaseline(
                    ReadRequiredString(arguments, "name"),
                    ReadWindowCriteria(arguments, true),
                    ReadOptionalString(arguments, "targetName"),
                    ReadBool(arguments, "clientArea"),
                    ReadOptionalString(arguments, "description")),
                "assert_visual_baseline" => DesktopOperations.AssertVisualBaseline(
                    ReadRequiredString(arguments, "name"),
                    ReadWindowCriteria(arguments, true),
                    ReadOptionalString(arguments, "targetName"),
                    TryReadProperty(arguments, "clientArea", out _) ? ReadBool(arguments, "clientArea") : null,
                    ReadDouble(arguments, "maxChangedRatio") ?? 0.01,
                    ReadInt(arguments, "differenceThreshold") ?? 24),
                "resolve_visual_baseline" => DesktopOperations.ResolveVisualBaseline(
                    ReadRequiredString(arguments, "name"),
                    ReadWindowCriteria(arguments, true),
                    ReadBool(arguments, "clientArea"),
                    ReadDouble(arguments, "maxAverageDifference") ?? 12.0,
                    ReadInt(arguments, "differenceThreshold") ?? 24,
                    ReadInt(arguments, "scanStep") ?? 8),
                "read_window_text" => DesktopOperations.ReadWindowText(
                    ReadWindowCriteria(arguments, true),
                    ReadOptionalString(arguments, "targetName"),
                    ReadBool(arguments, "clientArea"),
                    ReadOptionalString(arguments, "languageTag")),
                "resolve_window_text" => DesktopOperations.ResolveWindowText(
                    ReadWindowCriteria(arguments, true),
                    ReadRequiredString(arguments, "queryText"),
                    ReadBool(arguments, "contains"),
                    ReadOptionalString(arguments, "targetName"),
                    ReadBool(arguments, "clientArea"),
                    ReadOptionalString(arguments, "languageTag")),
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
                "set_control_check_state" => DesktopOperations.SetControlCheckState(
                    ReadRequiredString(arguments, "windowHandle"),
                    ReadRequiredString(arguments, "controlHandle"),
                    ReadRequiredBool(arguments, "checked")),
                "set_matching_control_check_state" => DesktopOperations.SetControlCheckState(
                    ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                    ReadControlCriteria(arguments),
                    ReadRequiredBool(arguments, "checked"),
                    ReadBool(arguments, "allWindows"),
                    ReadMutationArtifactOptions(arguments)),
                "set_control_selected_value" => DesktopOperations.SetControlSelectedValue(
                    ReadRequiredString(arguments, "windowHandle"),
                    ReadRequiredString(arguments, "controlHandle"),
                    ReadRequiredString(arguments, "selectedValue")),
                "set_matching_control_selected_value" => DesktopOperations.SetControlSelectedValue(
                    ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                    ReadControlCriteria(arguments),
                    ReadRequiredString(arguments, "selectedValue"),
                    ReadBool(arguments, "allWindows"),
                    ReadMutationArtifactOptions(arguments)),
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
}
