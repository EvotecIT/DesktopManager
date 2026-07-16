using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static partial class McpCatalog {
    private static readonly HashSet<string> SystemSettingsToolNames = new(StringComparer.Ordinal) {
        "configure_audio_endpoint", "invoke_system_action", "configure_keep_awake", "apply_personalization", "configure_taskbar",
        "apply_workstation_profile", "set_radio_state", "set_airplane_mode"
    };

    private static readonly HashSet<string> ExperimentalToolNames = new(StringComparer.Ordinal) {
        "get_airplane_mode", "set_airplane_mode"
    };

    public static bool RequiresSystemSettingsAccess(string name) => SystemSettingsToolNames.Contains(name);
    public static bool RequiresExperimentalAccess(string name) => ExperimentalToolNames.Contains(name);

    private static McpToolDefinition[] CreateDesktopStateTools() {
        return new[] {
            CreateTool("get_system_state", "Get System State", "Return current power and interactive-session state.", CreateObjectSchema(), readOnly: true),
            CreateTool("get_audio_endpoints", "Get Audio Endpoints", "Return Core Audio endpoints with default roles, volume, and mute state.", CreateObjectSchema(new Dictionary<string, object> {
                ["flow"] = CreateStringSchema("Optional render, capture, or all data flow."),
                ["activeOnly"] = CreateBooleanSchema("Return active endpoints only.")
            }), readOnly: true),
            CreateTool("configure_audio_endpoint", "Configure Audio Endpoint", "Apply volume, mute, or default roles to one Core Audio endpoint.", CreateObjectSchema(new Dictionary<string, object> {
                ["deviceId"] = CreateStringSchema("Stable Windows audio endpoint identifier."),
                ["volume"] = CreateNumberSchema("Optional master volume from 0 through 100."),
                ["muted"] = CreateBooleanSchema("Optional explicit master mute state."),
                ["defaultRoles"] = CreateArraySchema("Optional default roles.", CreateStringSchema("console, multimedia, or communications."))
            }, new[] { "deviceId" }), readOnly: false, idempotent: true),
            CreateTool("get_personalization", "Get Personalization", "Capture current personalization settings and monitor wallpapers.", CreateObjectSchema(), readOnly: true),
            CreateTool("apply_personalization", "Apply Personalization", "Apply explicitly supplied personalization settings.", CreatePersonalizationSchema(), readOnly: false, idempotent: true),
            CreateTool("get_taskbars", "Get Taskbars", "Return taskbar windows, monitor mapping, visibility, edge, and auto-hide state.", CreateObjectSchema(), readOnly: true),
            CreateTool("configure_taskbar", "Configure Taskbar", "Apply taskbar visibility, edge, or global auto-hide state.", CreateObjectSchema(new Dictionary<string, object> {
                ["monitorIndex"] = CreateIntegerSchema("Monitor index for visibility or edge changes."),
                ["visible"] = CreateBooleanSchema("Optional explicit visibility."),
                ["position"] = CreateStringSchema("Optional left, top, right, or bottom edge."),
                ["autoHide"] = CreateBooleanSchema("Optional global auto-hide state.")
            }), readOnly: false, idempotent: true),
            CreateTool("get_workstation_profiles", "Get Workstation Profiles", "List named workstation profiles or load one profile.", CreateObjectSchema(new Dictionary<string, object> {
                ["name"] = CreateStringSchema("Optional stored profile name.")
            }), readOnly: true),
            CreateTool("save_workstation_profile", "Save Workstation Profile", "Capture and save display, personalization, taskbar, and active audio state.", CreateObjectSchema(new Dictionary<string, object> {
                ["name"] = CreateStringSchema("Profile name.")
            }, new[] { "name" }), readOnly: false, idempotent: true),
            CreateTool("apply_workstation_profile", "Apply Workstation Profile", "Apply a named workstation profile with monitor matching and rollback.", CreateObjectSchema(new Dictionary<string, object> {
                ["name"] = CreateStringSchema("Profile name."),
                ["allowMissingMonitors"] = CreateBooleanSchema("Allow saved monitors to be absent."),
                ["skipDisplays"] = CreateBooleanSchema("Skip display settings."),
                ["skipAudio"] = CreateBooleanSchema("Skip audio settings."),
                ["skipPersonalization"] = CreateBooleanSchema("Skip personalization settings."),
                ["includeMachinePolicies"] = CreateBooleanSchema("Also restore machine-wide lock-screen and Spotlight policies."),
                ["skipTaskbars"] = CreateBooleanSchema("Skip taskbar settings."),
                ["noRollback"] = CreateBooleanSchema("Disable rollback after failure.")
            }, new[] { "name" }), readOnly: false, idempotent: true),
            CreateTool("delete_workstation_profile", "Delete Workstation Profile", "Delete a named workstation profile.", CreateObjectSchema(new Dictionary<string, object> {
                ["name"] = CreateStringSchema("Profile name.")
            }, new[] { "name" }), readOnly: false, destructive: true, idempotent: true),
            CreateTool("list_radios", "List Radios", "Return radios through the supported Windows radio API.", CreateObjectSchema(), readOnly: true),
            CreateTool("set_radio_state", "Set Radio State", "Apply an explicit On or Off state through the supported Windows radio API.", CreateObjectSchema(new Dictionary<string, object> {
                ["kind"] = CreateStringSchema("WiFi, Bluetooth, MobileBroadband, or FM."),
                ["state"] = CreateStringSchema("On or Off."),
                ["name"] = CreateStringSchema("Optional exact Windows-provided radio name.")
            }, new[] { "kind", "state" }), readOnly: false, idempotent: true),
            CreateTool("get_airplane_mode", "Get Airplane Mode", "Read global airplane mode through an undocumented experimental Windows COM contract.", CreateObjectSchema(), readOnly: true),
            CreateTool("set_airplane_mode", "Set Airplane Mode", "Apply and verify an explicit global airplane-mode state through an undocumented experimental Windows COM contract.", CreateObjectSchema(new Dictionary<string, object> {
                ["state"] = CreateStringSchema("Enabled or Disabled.")
            }, new[] { "state" }), readOnly: false, destructive: true, idempotent: true),
            CreateTool("get_window_virtual_desktop", "Get Window Virtual Desktop", "Return supported virtual-desktop state for a top-level window.", CreateObjectSchema(new Dictionary<string, object> {
                ["handle"] = CreateStringSchema("Top-level window handle in decimal or hexadecimal format.")
            }, new[] { "handle" }), readOnly: true),
            CreateTool("move_window_to_virtual_desktop", "Move Window To Virtual Desktop", "Move a top-level window to a desktop identifier obtained from another window.", CreateObjectSchema(new Dictionary<string, object> {
                ["handle"] = CreateStringSchema("Top-level window handle in decimal or hexadecimal format."),
                ["desktopId"] = CreateStringSchema("Target virtual-desktop GUID.")
            }, new[] { "handle", "desktopId" }), readOnly: false, idempotent: true),
            CreateTool("invoke_system_action", "Invoke System Action", "Lock, sleep, hibernate, or sign out of the current interactive session.", CreateObjectSchema(new Dictionary<string, object> {
                ["action"] = CreateStringSchema("lock, sleep, hibernate, or signout."),
                ["force"] = CreateBooleanSchema("Force suspension or sign-out when supported.")
            }, new[] { "action" }), readOnly: false, destructive: true, idempotent: false),
            CreateTool("configure_keep_awake", "Configure Keep Awake", "Start, replace, or stop the MCP server's keep-awake lease without blocking the server.", CreateObjectSchema(new Dictionary<string, object> {
                ["enabled"] = CreateBooleanSchema("Explicit keep-awake state."),
                ["durationSeconds"] = CreateIntegerSchema("Optional duration from 1 through 86400 seconds."),
                ["display"] = CreateBooleanSchema("Keep the display on during keepawake."),
                ["awayMode"] = CreateBooleanSchema("Request away mode during keepawake.")
            }, new[] { "enabled" }), readOnly: false, idempotent: true)
        };
    }

    private static object CreatePersonalizationSchema() {
        return CreateObjectSchema(new Dictionary<string, object> {
            ["lockScreenImagePath"] = CreateStringSchema("Optional lock-screen image path."),
            ["disableLockScreenSlideshow"] = CreateBooleanSchema("Disable lock-screen slideshows."),
            ["disableWindowsSpotlight"] = CreateBooleanSchema("Disable Windows Spotlight features."),
            ["disableWindowsSpotlightOnLockScreen"] = CreateBooleanSchema("Disable Spotlight on the lock screen."),
            ["systemTheme"] = CreateStringSchema("Light or Dark system theme."),
            ["appsTheme"] = CreateStringSchema("Light or Dark app theme."),
            ["enableTransparency"] = CreateBooleanSchema("Enable transparency effects."),
            ["accentColor"] = CreateStringSchema("Accent ARGB value."),
            ["useAccentColorOnStartTaskbar"] = CreateBooleanSchema("Use accent color on Start and taskbar."),
            ["useAccentColorOnTitleBars"] = CreateBooleanSchema("Use accent color on title bars."),
            ["startLayout"] = CreateStringSchema("Default, MorePins, or MoreRecommendations."),
            ["startShowAllPins"] = CreateBooleanSchema("Show all Start pins."),
            ["startRecommendationsEnabled"] = CreateBooleanSchema("Enable Start recommendations."),
            ["taskbarAlignment"] = CreateStringSchema("Left or Center."),
            ["taskbarGrouping"] = CreateStringSchema("Always, WhenFull, or Never."),
            ["taskbarFlashingEnabled"] = CreateBooleanSchema("Enable taskbar flashing."),
            ["taskbarShareWindowEnabled"] = CreateBooleanSchema("Enable taskbar window sharing."),
            ["taskbarShowDesktopEnabled"] = CreateBooleanSchema("Enable Show Desktop."),
            ["taskbarRecentSearchesEnabled"] = CreateBooleanSchema("Enable recent searches on hover."),
            ["taskbarTaskViewButtonVisible"] = CreateBooleanSchema("Show Task View."),
            ["taskbarWidgetsButtonVisible"] = CreateBooleanSchema("Show Widgets."),
            ["dynamicLightingEnabled"] = CreateBooleanSchema("Enable Dynamic Lighting."),
            ["desktopWallpaperPath"] = CreateStringSchema("Desktop wallpaper path."),
            ["desktopWallpaperPosition"] = CreateStringSchema("Wallpaper position."),
            ["desktopBackgroundColor"] = CreateStringSchema("RGB value."),
            ["applyWallpaperToAllUsers"] = CreateBooleanSchema("Apply wallpaper to every user profile."),
            ["includeDefaultUserProfile"] = CreateBooleanSchema("Include the default profile in all-user wallpaper application.")
        });
    }

    private static bool TryCallDesktopStateTool(string name, JsonElement arguments, out object result) {
        switch (name) {
            case "get_system_state":
                result = new {
                    power = new SystemPowerService().GetStatus(),
                    session = new DesktopSessionService().GetCurrentSession()
                };
                return true;
            case "get_audio_endpoints":
                result = new AudioService().GetEndpoints(
                    ReadEnum(arguments, "flow", AudioDataFlow.All),
                    ReadBool(arguments, "activeOnly") ? AudioEndpointState.Active : AudioEndpointState.All);
                return true;
            case "configure_audio_endpoint":
                result = ConfigureAudioEndpoint(arguments);
                return true;
            case "get_personalization":
                result = new PersonalizationService().CaptureSnapshot();
                return true;
            case "apply_personalization":
                result = ApplyPersonalization(arguments);
                return true;
            case "get_taskbars":
                result = GetTaskbarState();
                return true;
            case "configure_taskbar":
                result = ConfigureTaskbar(arguments);
                return true;
            case "get_workstation_profiles":
                string? profileName = ReadOptionalString(arguments, "name");
                result = string.IsNullOrWhiteSpace(profileName)
                    ? WorkstationProfileStore.List()
                    : WorkstationProfileStore.Load(profileName!);
                return true;
            case "save_workstation_profile":
                result = new WorkstationProfileService().SaveProfile(ReadRequiredString(arguments, "name"));
                return true;
            case "apply_workstation_profile":
                result = ApplyWorkstationProfile(arguments);
                return true;
            case "delete_workstation_profile":
                result = new {
                    deleted = WorkstationProfileStore.Delete(ReadRequiredString(arguments, "name"))
                };
                return true;
            case "list_radios":
                using (var radios = new RadioService()) {
                    result = radios.GetRadiosAsync().GetAwaiter().GetResult();
                }
                return true;
            case "set_radio_state":
                DesktopRadioState requestedRadioState = ReadRequiredEnum<DesktopRadioState>(arguments, "state");
                if (requestedRadioState != DesktopRadioState.On && requestedRadioState != DesktopRadioState.Off) {
                    throw new CommandLineException("Property 'state' must be On or Off.");
                }
                using (var radios = new RadioService()) {
                    result = RequireAppliedRadioResults(radios.SetRadioStateAsync(
                        ReadRequiredEnum<DesktopRadioKind>(arguments, "kind"),
                        requestedRadioState,
                        ReadOptionalString(arguments, "name")).GetAwaiter().GetResult());
                }
                return true;
            case "get_airplane_mode":
                result = new {
                    state = new ExperimentalAirplaneModeService().GetState(),
                    experimental = true
                };
                return true;
            case "set_airplane_mode":
                result = new {
                    state = new ExperimentalAirplaneModeService().SetState(
                        ReadRequiredEnum<AirplaneModeState>(arguments, "state")),
                    experimental = true
                };
                return true;
            case "get_window_virtual_desktop":
                result = GetWindowVirtualDesktop(arguments);
                return true;
            case "move_window_to_virtual_desktop":
                result = MoveWindowToVirtualDesktop(arguments);
                return true;
            case "invoke_system_action":
                result = InvokeSystemAction(arguments);
                return true;
            case "configure_keep_awake":
                result = ConfigureKeepAwake(arguments);
                return true;
            default:
                result = null!;
                return false;
        }
    }

    private static AudioEndpointInfo ConfigureAudioEndpoint(JsonElement arguments) {
        string deviceId = ReadRequiredString(arguments, "deviceId");
        var service = new AudioService();
        double? volume = ReadDouble(arguments, "volume");
        bool? muted = ReadNullableBool(arguments, "muted");
        IReadOnlyList<string> roles = ReadStringList(arguments, "defaultRoles");
        if (!volume.HasValue && !muted.HasValue && roles.Count == 0) {
            throw new CommandLineException("At least one of volume, muted, or defaultRoles is required.");
        }
        if (volume.HasValue) {
            service.SetEndpointVolume(deviceId, (float)volume.Value);
        }
        if (muted.HasValue) {
            service.SetEndpointMute(deviceId, muted.Value);
        }
        if (roles.Count > 0) {
            service.SetDefaultAudioDevice(deviceId, roles.Select(ParseAudioRole).ToArray());
        }
        return service.GetEndpoint(deviceId);
    }

    private static PersonalizationSnapshot ApplyPersonalization(JsonElement arguments) {
        var settings = new PersonalizationSettings {
            LockScreenImagePath = ReadOptionalString(arguments, "lockScreenImagePath"),
            DisableLockScreenSlideshow = ReadNullableBool(arguments, "disableLockScreenSlideshow"),
            DisableWindowsSpotlight = ReadNullableBool(arguments, "disableWindowsSpotlight"),
            DisableWindowsSpotlightOnLockScreen = ReadNullableBool(arguments, "disableWindowsSpotlightOnLockScreen"),
            SystemTheme = ReadNullableEnum<SystemTheme>(arguments, "systemTheme"),
            AppsTheme = ReadNullableEnum<SystemTheme>(arguments, "appsTheme"),
            EnableTransparency = ReadNullableBool(arguments, "enableTransparency"),
            AccentColor = ReadOptionalColor(arguments, "accentColor"),
            UseAccentColorOnStartTaskbar = ReadNullableBool(arguments, "useAccentColorOnStartTaskbar"),
            UseAccentColorOnTitleBars = ReadNullableBool(arguments, "useAccentColorOnTitleBars"),
            StartLayout = ReadNullableEnum<StartLayoutPreference>(arguments, "startLayout"),
            StartShowAllPins = ReadNullableBool(arguments, "startShowAllPins"),
            StartRecommendationsEnabled = ReadNullableBool(arguments, "startRecommendationsEnabled"),
            TaskbarAlignment = ReadNullableEnum<TaskbarAlignmentPreference>(arguments, "taskbarAlignment"),
            TaskbarGrouping = ReadNullableEnum<TaskbarGroupingPreference>(arguments, "taskbarGrouping"),
            TaskbarFlashingEnabled = ReadNullableBool(arguments, "taskbarFlashingEnabled"),
            TaskbarShareWindowEnabled = ReadNullableBool(arguments, "taskbarShareWindowEnabled"),
            TaskbarShowDesktopEnabled = ReadNullableBool(arguments, "taskbarShowDesktopEnabled"),
            TaskbarRecentSearchesEnabled = ReadNullableBool(arguments, "taskbarRecentSearchesEnabled"),
            TaskbarTaskViewButtonVisible = ReadNullableBool(arguments, "taskbarTaskViewButtonVisible"),
            TaskbarWidgetsButtonVisible = ReadNullableBool(arguments, "taskbarWidgetsButtonVisible"),
            DynamicLightingEnabled = ReadNullableBool(arguments, "dynamicLightingEnabled"),
            DesktopWallpaperPath = ReadOptionalString(arguments, "desktopWallpaperPath"),
            DesktopWallpaperPosition = ReadNullableEnum<DesktopWallpaperPosition>(arguments, "desktopWallpaperPosition"),
            DesktopBackgroundColor = ReadOptionalColor(arguments, "desktopBackgroundColor"),
            ApplyWallpaperToAllUsers = ReadBool(arguments, "applyWallpaperToAllUsers"),
            IncludeDefaultUserProfile = !TryReadProperty(arguments, "includeDefaultUserProfile", out _) || ReadBool(arguments, "includeDefaultUserProfile")
        };
        var service = new PersonalizationService();
        service.Apply(settings);
        return service.CaptureSnapshot();
    }

    private static object GetTaskbarState() {
        var service = new TaskbarService();
        return new {
            autoHide = service.GetTaskbarAutoHide(),
            taskbars = service.GetTaskbars()
        };
    }

    private static object ConfigureTaskbar(JsonElement arguments) {
        int? monitorIndex = ReadInt(arguments, "monitorIndex");
        bool? visible = ReadNullableBool(arguments, "visible");
        TaskbarPosition? position = ReadNullableEnum<TaskbarPosition>(arguments, "position");
        bool? autoHide = ReadNullableBool(arguments, "autoHide");
        if (!visible.HasValue && !position.HasValue && !autoHide.HasValue) {
            throw new CommandLineException("At least one of visible, position, or autoHide is required.");
        }
        if ((visible.HasValue || position.HasValue) && !monitorIndex.HasValue) {
            throw new CommandLineException("Property 'monitorIndex' is required for visibility or position changes.");
        }

        var service = new TaskbarService();
        if (position.HasValue) {
            service.SetTaskbarPosition(monitorIndex!.Value, position.Value);
        }
        if (visible.HasValue) {
            service.SetTaskbarVisibility(monitorIndex!.Value, visible.Value);
        }
        if (autoHide.HasValue) {
            service.SetTaskbarAutoHide(autoHide.Value);
        }
        return GetTaskbarState();
    }

    private static WorkstationProfileApplyResult ApplyWorkstationProfile(JsonElement arguments) {
        WorkstationProfileApplyResult result = new WorkstationProfileService().ApplyProfile(
            ReadRequiredString(arguments, "name"),
            new WorkstationProfileApplyOptions {
                RequireAllMonitors = !ReadBool(arguments, "allowMissingMonitors"),
                ApplyDisplays = !ReadBool(arguments, "skipDisplays"),
                ApplyAudio = !ReadBool(arguments, "skipAudio"),
                ApplyPersonalization = !ReadBool(arguments, "skipPersonalization"),
                ApplyMachinePolicies = ReadBool(arguments, "includeMachinePolicies"),
                ApplyTaskbars = !ReadBool(arguments, "skipTaskbars"),
                RollbackOnFailure = !ReadBool(arguments, "noRollback")
            });
        return RequireSuccessfulWorkstationProfileApply(result);
    }

    internal static WorkstationProfileApplyResult RequireSuccessfulWorkstationProfileApply(WorkstationProfileApplyResult result) {
        if (result.Succeeded) {
            return result;
        }

        var details = new List<string> {
            string.IsNullOrWhiteSpace(result.Error) ? "The operation failed without an error message." : result.Error
        };
        if (result.RolledBack) {
            details.Add("Previous desktop state was restored.");
        }
        if (result.Warnings.Count > 0) {
            details.Add($"Warnings: {string.Join(" ", result.Warnings)}");
        }
        throw new CommandLineException($"Workstation profile application failed. {string.Join(" ", details)}");
    }

    internal static IReadOnlyList<DesktopRadioSetResult> RequireAppliedRadioResults(IReadOnlyList<DesktopRadioSetResult> results) {
        DesktopRadioSetResult[] failed = results.Where(item => !item.Applied).ToArray();
        if (failed.Length == 0) {
            return results;
        }

        string details = string.Join(", ", failed.Select(item =>
            $"{item.Radio.Name}: access {item.AccessStatus}, effective {item.Radio.State}"));
        throw new CommandLineException($"Windows did not apply one or more requested radio states. {details}");
    }

    private static object GetWindowVirtualDesktop(JsonElement arguments) {
        IntPtr handle = DesktopHandleParser.Parse(ReadRequiredString(arguments, "handle"));
        using var service = new VirtualDesktopService();
        return new {
            handle,
            desktopId = service.GetWindowDesktopId(handle),
            onCurrentDesktop = service.IsWindowOnCurrentDesktop(handle)
        };
    }

    private static object MoveWindowToVirtualDesktop(JsonElement arguments) {
        IntPtr handle = DesktopHandleParser.Parse(ReadRequiredString(arguments, "handle"));
        if (!Guid.TryParse(ReadRequiredString(arguments, "desktopId"), out Guid id)) {
            throw new CommandLineException("Property 'desktopId' must be a valid GUID.");
        }
        using (var service = new VirtualDesktopService()) {
            service.MoveWindowToDesktop(handle, id);
        }
        return new {
            handle,
            desktopId = id,
            moved = true
        };
    }

    private static object InvokeSystemAction(JsonElement arguments) {
        string action = ReadRequiredString(arguments, "action").ToLowerInvariant();
        var service = new SystemPowerService();
        switch (action) {
            case "lock":
                service.LockWorkstation();
                break;
            case "sleep":
                service.Suspend(false, ReadBool(arguments, "force"));
                break;
            case "hibernate":
                service.Suspend(true, ReadBool(arguments, "force"));
                break;
            case "signout":
                service.SignOut(ReadBool(arguments, "force"));
                break;
            default:
                throw new CommandLineException($"Unsupported system action '{action}'.");
        }
        return new { action, completed = true };
    }

    private static object ConfigureKeepAwake(JsonElement arguments) {
        KeepAwakeOptions options = KeepAwakeOptions.System;
        if (ReadBool(arguments, "display")) {
            options |= KeepAwakeOptions.Display;
        }
        if (ReadBool(arguments, "awayMode")) {
            options |= KeepAwakeOptions.AwayMode;
        }
        return McpKeepAwakeManager.Configure(
            ReadRequiredBool(arguments, "enabled"),
            options,
            ReadInt(arguments, "durationSeconds"));
    }

    private static T ReadEnum<T>(JsonElement arguments, string name, T fallback) where T : struct {
        string? value = ReadOptionalString(arguments, name);
        return string.IsNullOrWhiteSpace(value) ? fallback : ParseEnum<T>(value!, name);
    }

    private static T ReadRequiredEnum<T>(JsonElement arguments, string name) where T : struct {
        return ParseEnum<T>(ReadRequiredString(arguments, name), name);
    }

    private static T? ReadNullableEnum<T>(JsonElement arguments, string name) where T : struct {
        string? value = ReadOptionalString(arguments, name);
        return string.IsNullOrWhiteSpace(value) ? null : ParseEnum<T>(value!, name);
    }

    private static T ParseEnum<T>(string value, string name) where T : struct {
        if (Enum.TryParse(value, true, out T parsed)) {
            return parsed;
        }
        throw new CommandLineException($"Property '{name}' has unsupported value '{value}'.");
    }

    private static AudioRole ParseAudioRole(string value) {
        return ParseEnum<AudioRole>(value, "defaultRoles");
    }

    private static uint? ReadOptionalColor(JsonElement arguments, string name) {
        return TryReadProperty(arguments, name, out _) ? ReadColor(arguments, name) : null;
    }
}
