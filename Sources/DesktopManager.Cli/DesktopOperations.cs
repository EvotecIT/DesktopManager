using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace DesktopManager.Cli;

internal static partial class DesktopOperations {
    public static IReadOnlyList<WindowResult> ListWindows(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => new DesktopAutomationService().GetWindows(CreateWindowQuery(criteria)).Select(MapWindow).ToArray());
    }

    public static WindowResult GetActiveWindow() {
        return ExecuteCore(() => {
            WindowInfo? window = new DesktopAutomationService().GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
            if (window == null) {
                throw new InvalidOperationException("The active window could not be resolved.");
            }

            return MapWindow(window);
        });
    }

    public static MouseStateResult GetMouseState() {
        return ExecuteCore(() => MapMouseState(new DesktopAutomationService().GetMouseState()));
    }

    public static ClipboardTextResult GetClipboardText(int? retryCount = null, int? retryDelayMilliseconds = null) {
        return ExecuteCore(() => {
            string? text = new DesktopAutomationService().GetClipboardText(retryCount ?? 5, retryDelayMilliseconds ?? 50);
            return new ClipboardTextResult {
                HasText = text != null,
                Text = text
            };
        });
    }

    public static ClipboardTextResult SetClipboardText(string text, int? retryCount = null, int? retryDelayMilliseconds = null) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            automation.SetClipboardText(text, retryCount ?? 5, retryDelayMilliseconds ?? 50);
            return new ClipboardTextResult {
                HasText = true,
                Text = automation.GetClipboardText(retryCount ?? 5, retryDelayMilliseconds ?? 50)
            };
        });
    }

    public static ElevationStatusResult GetElevationStatus() {
        return ExecuteCore(() => new ElevationStatusResult {
            IsElevated = new DesktopAutomationService().IsElevated()
        });
    }

    public static FocusedControlObservationResult? GetFocusedControl(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => {
            DesktopFocusedControlObservation? observation = new DesktopAutomationService().GetFocusedControlObservation(CreateWindowQuery(criteria));
            return observation == null ? null : MapFocusedControlObservation(observation);
        });
    }

    public static FocusedControlObservationResult WaitForFocusedControl(WindowSelectionCriteria criteria, int timeoutMilliseconds, int intervalMilliseconds) {
        return ExecuteCore(() => MapFocusedControlObservation(new DesktopAutomationService().WaitForFocusedControlObservation(
            CreateWindowQuery(criteria),
            timeoutMilliseconds,
            intervalMilliseconds)));
    }

    public static WindowTextObservationResult? ObserveWindowText(WindowSelectionCriteria criteria, string? expectedText = null, int? maxLength = null, int? retryCount = null, int? retryDelayMilliseconds = null) {
        return ExecuteCore(() => {
            DesktopWindowTextObservation? observation = new DesktopAutomationService().ObserveWindowText(
                CreateWindowQuery(criteria),
                expectedText,
                CreateTextObservationOptions(maxLength, retryCount, retryDelayMilliseconds));
            return observation == null ? null : MapWindowTextObservation(observation);
        });
    }

    public static WindowTextObservationResult WaitForObservedText(WindowSelectionCriteria criteria, string expectedText, int timeoutMilliseconds, int intervalMilliseconds, int? maxLength = null, int? retryCount = null, int? retryDelayMilliseconds = null) {
        return ExecuteCore(() => MapWindowTextObservation(new DesktopAutomationService().WaitForObservedText(
            CreateWindowQuery(criteria),
            expectedText,
            timeoutMilliseconds,
            intervalMilliseconds,
            CreateTextObservationOptions(maxLength, retryCount, retryDelayMilliseconds))));
    }

    public static WaitCompletionResult WaitForWindowToClose(WindowSelectionCriteria criteria, int timeoutMilliseconds, int intervalMilliseconds) {
        return ExecuteCore(() => MapWaitCompletion(new DesktopAutomationService().WaitForWindowToClose(
            CreateWindowQuery(criteria),
            timeoutMilliseconds,
            intervalMilliseconds,
            criteria.All)));
    }

    public static WaitCompletionResult WaitForWindowToLoseFocus(WindowSelectionCriteria criteria, int timeoutMilliseconds, int intervalMilliseconds) {
        return ExecuteCore(() => MapWaitCompletion(new DesktopAutomationService().WaitForWindowToLoseFocus(
            CreateWindowQuery(criteria),
            timeoutMilliseconds,
            intervalMilliseconds,
            criteria.All)));
    }

    public static ControlStateResult GetControlState(string windowHandle, string controlHandle) {
        return ExecuteCore(() => {
            DesktopControlState? state = new DesktopAutomationService().GetControlState(
                DesktopHandleParser.Parse(windowHandle),
                DesktopHandleParser.Parse(controlHandle));
            if (state == null) {
                throw new InvalidOperationException("The requested control could not be resolved.");
            }

            return MapControlState(state);
        });
    }

    public static ControlStateResult FocusControl(string windowHandle, string controlHandle, bool ensureForegroundWindow) {
        return ExecuteCore(() => MapControlState(new DesktopAutomationService().FocusControl(
            DesktopHandleParser.Parse(windowHandle),
            DesktopHandleParser.Parse(controlHandle),
            ensureForegroundWindow)));
    }

    public static ControlStateResult SetControlEnabled(string windowHandle, string controlHandle, bool enabled) {
        return ExecuteCore(() => MapControlState(new DesktopAutomationService().SetControlEnabled(
            DesktopHandleParser.Parse(windowHandle),
            DesktopHandleParser.Parse(controlHandle),
            enabled)));
    }

    public static ControlStateResult SetControlVisibility(string windowHandle, string controlHandle, bool visible) {
        return ExecuteCore(() => MapControlState(new DesktopAutomationService().SetControlVisibility(
            DesktopHandleParser.Parse(windowHandle),
            DesktopHandleParser.Parse(controlHandle),
            visible)));
    }

    public static IReadOnlyList<WindowGeometryResult> GetWindowGeometry(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetWindowGeometry(CreateWindowQuery(criteria), criteria.All)
            .Select(MapWindowGeometry)
            .ToArray());
    }

    public static IReadOnlyList<WindowProcessInfoResult> GetWindowProcessInfo(WindowSelectionCriteria criteria, bool owner) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            IReadOnlyList<WindowInfo> windows = automation.GetWindows(CreateWindowQuery(criteria));
            if (!criteria.All && windows.Count > 1) {
                windows = new[] { windows[0] };
            }

            var results = new List<WindowProcessInfoResult>(windows.Count);
            foreach (WindowInfo window in windows) {
                WindowProcessInfo? info = owner
                    ? automation.GetOwnerWindowProcessInfo(window)
                    : automation.GetWindowProcessInfo(window);
                if (info == null) {
                    continue;
                }

                results.Add(MapWindowProcessInfo(window, info, owner));
            }

            return results.ToArray();
        });
    }

    public static IReadOnlyList<WindowResult> ListWindowKeepAlive() {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetWindowKeepAliveWindows()
            .Select(MapWindow)
            .ToArray());
    }

    public static WindowChangeResult StartWindowKeepAlive(WindowSelectionCriteria criteria, int intervalMilliseconds) {
        if (intervalMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds), "The keep-alive interval must be greater than zero.");
        }

        return ExecuteWindowMutation(
            "keep-alive-start",
            criteria,
            "window-keep-alive",
            artifactOptions: null,
            automation => automation.StartWindowKeepAlive(
                CreateWindowQuery(criteria),
                TimeSpan.FromMilliseconds(intervalMilliseconds),
                criteria.All));
    }

    public static WindowChangeResult StopWindowKeepAlive(WindowSelectionCriteria criteria) {
        return ExecuteWindowMutation(
            "keep-alive-stop",
            criteria,
            "window-keep-alive",
            artifactOptions: null,
            automation => automation.StopWindowKeepAlive(CreateWindowQuery(criteria), criteria.All));
    }

    public static WindowChangeResult StopAllWindowKeepAlive() {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            Stopwatch stopwatch = Stopwatch.StartNew();
            IReadOnlyList<WindowInfo> windows = automation.GetWindowKeepAliveWindows();
            automation.StopAllWindowKeepAlive();
            stopwatch.Stop();
            return BuildWindowChangeResult(
                "keep-alive-stop",
                windows,
                (int)stopwatch.ElapsedMilliseconds,
                "window-keep-alive",
                targetName: null,
                targetKind: null,
                beforeScreenshots: Array.Empty<ScreenshotResult>(),
                afterScreenshots: Array.Empty<ScreenshotResult>(),
                artifactWarnings: Array.Empty<string>(),
                verification: null);
        });
    }

    public static WindowChangeResult MoveWindow(WindowSelectionCriteria criteria, int? monitorIndex, int? x, int? y, int? width, int? height, bool activate, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "move",
            criteria,
            activate ? "window-management-activate" : "window-management",
            artifactOptions,
            automation => automation.MoveWindows(CreateWindowQuery(criteria), monitorIndex, x, y, width, height, activate, criteria.All),
            verify: (automation, windows, options) => BuildWindowPostconditionVerificationResult(
                "move",
                windows,
                ObserveWindowsByHandle(automation, windows),
                SafeGetActiveWindowInfo(automation),
                options.VerificationTolerancePixels,
                monitorIndex,
                x,
                y,
                width,
                height,
                activate));
    }

    public static WindowChangeResult FocusWindow(WindowSelectionCriteria criteria, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "focus",
            criteria,
            "window-management-activate",
            artifactOptions,
            automation => automation.FocusWindows(CreateWindowQuery(criteria), criteria.All),
            verify: (automation, windows, options) => BuildWindowPostconditionVerificationResult(
                "focus",
                windows,
                ObserveWindowsByHandle(automation, windows),
                SafeGetActiveWindowInfo(automation),
                options.VerificationTolerancePixels,
                requireForegroundMatch: true));
    }

    public static WindowChangeResult ClickWindowPoint(WindowSelectionCriteria criteria, int? x, int? y, double? xRatio, double? yRatio, string button, bool activate, bool clientArea, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteWindowMutation(
            "click-point",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.ClickWindowPoint(CreateWindowQuery(criteria), x, y, xRatio, yRatio, mouseButton, activate, clientArea, criteria.All));
    }

    public static WindowChangeResult ClickWindowTarget(WindowSelectionCriteria criteria, string targetName, string button, bool activate, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteWindowMutation(
            "click-target",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.ClickWindowTarget(CreateWindowQuery(criteria), targetName, mouseButton, activate, criteria.All),
            targetName,
            "window-target");
    }

    public static WindowChangeResult DragWindowPoints(WindowSelectionCriteria criteria, int? startX, int? startY, double? startXRatio, double? startYRatio, int? endX, int? endY, double? endXRatio, double? endYRatio, string button, int stepDelayMilliseconds, bool activate, bool clientArea, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteWindowMutation(
            "drag-point",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.DragWindowPoints(CreateWindowQuery(criteria), startX, startY, startXRatio, startYRatio, endX, endY, endXRatio, endYRatio, mouseButton, stepDelayMilliseconds, activate, clientArea, criteria.All));
    }

    public static WindowChangeResult DragWindowTargets(WindowSelectionCriteria criteria, string startTargetName, string endTargetName, string button, int stepDelayMilliseconds, bool activate, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteWindowMutation(
            "drag-target",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.DragWindowTargets(CreateWindowQuery(criteria), startTargetName, endTargetName, mouseButton, stepDelayMilliseconds, activate, criteria.All),
            $"{startTargetName}->{endTargetName}",
            "window-target-pair");
    }

    public static WindowChangeResult ScrollWindowPoint(WindowSelectionCriteria criteria, int? x, int? y, double? xRatio, double? yRatio, int delta, bool activate, bool clientArea, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "scroll-point",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.ScrollWindowPoint(CreateWindowQuery(criteria), x, y, xRatio, yRatio, delta, activate, clientArea, criteria.All));
    }

    public static WindowChangeResult ScrollWindowTarget(WindowSelectionCriteria criteria, string targetName, int delta, bool activate, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "scroll-target",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.ScrollWindowTarget(CreateWindowQuery(criteria), targetName, delta, activate, criteria.All),
            targetName,
            "window-target");
    }

    public static WindowChangeResult MinimizeWindows(WindowSelectionCriteria criteria, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "minimize",
            criteria,
            "window-management",
            artifactOptions,
            automation => automation.MinimizeWindows(CreateWindowQuery(criteria), criteria.All),
            verify: (automation, windows, options) => BuildWindowPostconditionVerificationResult(
                "minimize",
                windows,
                ObserveWindowsByHandle(automation, windows),
                SafeGetActiveWindowInfo(automation),
                options.VerificationTolerancePixels));
    }

    public static WindowChangeResult MaximizeWindows(WindowSelectionCriteria criteria, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "maximize",
            criteria,
            "window-management",
            artifactOptions,
            automation => {
                IReadOnlyList<WindowInfo> windows = ResolveWindowsForMutation(automation, criteria);
                foreach (WindowInfo window in windows) {
                    automation.MaximizeWindow(window.Handle);
                }

                return windows;
            },
            verify: (automation, windows, options) => BuildWindowStateVerificationResult(
                "maximize",
                windows.Select(MapWindow).ToArray(),
                ObserveWindowsByHandle(automation, windows).Select(MapWindow).ToArray(),
                SafeGetActiveWindowInfo(automation) == null ? null : MapWindow(SafeGetActiveWindowInfo(automation)!),
                options.VerificationTolerancePixels,
                "Maximize"));
    }

    public static WindowChangeResult RestoreWindows(WindowSelectionCriteria criteria, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "restore",
            criteria,
            "window-management",
            artifactOptions,
            automation => {
                IReadOnlyList<WindowInfo> windows = ResolveWindowsForMutation(automation, criteria);
                foreach (WindowInfo window in windows) {
                    automation.RestoreWindow(window.Handle);
                }

                return windows;
            },
            verify: (automation, windows, options) => BuildWindowStateVerificationResult(
                "restore",
                windows.Select(MapWindow).ToArray(),
                ObserveWindowsByHandle(automation, windows).Select(MapWindow).ToArray(),
                SafeGetActiveWindowInfo(automation) == null ? null : MapWindow(SafeGetActiveWindowInfo(automation)!),
                options.VerificationTolerancePixels,
                "Normal"));
    }

    public static WindowChangeResult CloseWindows(WindowSelectionCriteria criteria, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "close",
            criteria,
            "window-management",
            artifactOptions,
            automation => {
                IReadOnlyList<WindowInfo> windows = ResolveWindowsForMutation(automation, criteria);
                foreach (WindowInfo window in windows) {
                    automation.CloseWindow(window.Handle);
                }

                return windows;
            },
            verify: (automation, windows, options) => BuildWindowClosedVerificationResult(
                "close",
                windows.Select(MapWindow).ToArray(),
                ObserveWindowsByHandle(automation, windows).Select(MapWindow).ToArray(),
                options.VerificationTolerancePixels));
    }

    public static WindowChangeResult SetWindowTopMost(WindowSelectionCriteria criteria, bool topMost, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            topMost ? "set-topmost" : "clear-topmost",
            criteria,
            "window-management",
            artifactOptions,
            automation => {
                IReadOnlyList<WindowInfo> windows = ResolveWindowsForMutation(automation, criteria);
                foreach (WindowInfo window in windows) {
                    automation.SetWindowTopMost(window.Handle, topMost);
                }

                return windows;
            },
            verify: (automation, windows, options) => BuildWindowBooleanVerificationResult(
                topMost ? "set-topmost" : "clear-topmost",
                windows.Select(MapWindow).ToArray(),
                ObserveWindowsByHandle(automation, windows).Select(MapWindow).ToArray(),
                options.VerificationTolerancePixels,
                "topmost",
                window => window.IsTopMost,
                topMost));
    }

    public static WindowChangeResult SetWindowVisibility(WindowSelectionCriteria criteria, bool visible, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            visible ? "show-window" : "hide-window",
            criteria,
            "window-management",
            artifactOptions,
            automation => {
                IReadOnlyList<WindowInfo> windows = ResolveWindowsForMutation(automation, criteria);
                foreach (WindowInfo window in windows) {
                    automation.SetWindowVisibility(window.Handle, visible);
                }

                return windows;
            },
            verify: (automation, windows, options) => BuildWindowBooleanVerificationResult(
                visible ? "show-window" : "hide-window",
                windows.Select(MapWindow).ToArray(),
                ObserveWindowsByHandle(automation, windows).Select(MapWindow).ToArray(),
                options.VerificationTolerancePixels,
                "visible",
                window => window.IsVisible,
                visible));
    }

    public static WindowChangeResult SetWindowTransparency(WindowSelectionCriteria criteria, byte alpha, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "set-transparency",
            criteria,
            "window-management",
            artifactOptions,
            automation => {
                IReadOnlyList<WindowInfo> windows = ResolveWindowsForMutation(automation, criteria);
                foreach (WindowInfo window in windows) {
                    automation.SetWindowTransparency(window.Handle, alpha);
                }

                return windows;
            });
    }

    public static WindowChangeResult SnapWindow(WindowSelectionCriteria criteria, string position, MutationArtifactOptions? artifactOptions = null) {
        if (!TryParseSnapPosition(position, out SnapPosition snapPosition)) {
            throw new CommandLineException($"Unsupported snap position '{position}'.");
        }

        return ExecuteWindowMutation(
            "snap",
            criteria,
            "window-management",
            artifactOptions,
            automation => automation.SnapWindows(CreateWindowQuery(criteria), snapPosition, criteria.All));
    }

    public static WindowChangeResult TypeWindowText(WindowSelectionCriteria criteria, WindowTextCommandOptions options, MutationArtifactOptions? artifactOptions = null) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.Text == null) {
            throw new CommandLineException("Text is required.");
        }

        if (options.Paste && options.ForegroundInput) {
            throw new CommandLineException("Cannot combine '--paste' with '--foreground-input'.");
        }

        if (options.Paste && options.PhysicalKeys) {
            throw new CommandLineException("Cannot combine '--paste' with '--physical-keys'.");
        }

        if (options.Paste && options.ScriptMode) {
            throw new CommandLineException("Cannot combine '--paste' with '--script'.");
        }

        string action = options.Paste
            ? "paste-text"
            : options.ScriptMode && options.HostedSession
                ? "type-script-hosted-session"
            : options.ScriptMode && options.PhysicalKeys
                ? "type-script-physical-keys"
            : options.ScriptMode && options.ForegroundInput
                ? "type-script-foreground"
            : options.ScriptMode
                ? "type-script"
            : options.HostedSession
                ? "type-text-hosted-session"
            : options.PhysicalKeys
                ? "type-text-physical-keys"
            : options.ForegroundInput
                ? "type-text-foreground"
                : "type-text";
        string safetyMode = options.Paste
            ? "window-text-paste"
            : options.ScriptMode && options.HostedSession
                ? "window-script-hosted-session-input"
            : options.ScriptMode && options.PhysicalKeys
                ? "window-script-physical-key-input"
            : options.ScriptMode && options.ForegroundInput
                ? "window-script-foreground-input"
            : options.ScriptMode
                ? "window-script-input"
            : options.HostedSession
                ? "window-text-hosted-session-input"
            : options.PhysicalKeys
                ? "window-text-physical-key-input"
            : options.ForegroundInput
                ? "window-text-foreground-input"
                : "window-text-input";

        return ExecuteWindowMutation(
            action,
            criteria,
            safetyMode,
            artifactOptions,
            automation => automation.TypeWindowText(CreateWindowQuery(criteria), options.Text, options.Paste, options.DelayMilliseconds, options.ForegroundInput, options.PhysicalKeys, options.HostedSession, options.ScriptMode, options.ScriptChunkLength, options.ScriptLineDelayMilliseconds, criteria.All));
    }

    public static WindowChangeResult SendWindowKeys(WindowSelectionCriteria criteria, IReadOnlyList<string> keys, bool activate, MutationArtifactOptions? artifactOptions = null) {
        VirtualKey[] virtualKeys = ParseVirtualKeys(keys);
        return ExecuteWindowMutation(
            "send-window-keys",
            criteria,
            "window-key-input",
            artifactOptions,
            automation => automation.SendWindowKeys(CreateWindowQuery(criteria), virtualKeys, activate, criteria.All));
    }

    public static IReadOnlyList<MonitorResult> ListMonitors(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        return ExecuteCore(() => new DesktopAutomationService().GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName)
            .Select(monitor => new MonitorResult {
                Index = monitor.Index,
                DeviceName = monitor.DeviceName,
                DeviceString = monitor.DeviceString,
                DeviceId = monitor.DeviceId,
                IsConnected = monitor.IsConnected,
                IsPrimary = monitor.IsPrimary,
                Left = monitor.PositionLeft,
                Top = monitor.PositionTop,
                Right = monitor.PositionRight,
                Bottom = monitor.PositionBottom,
                Manufacturer = monitor.Manufacturer,
                SerialNumber = monitor.SerialNumber
            })
            .ToArray());
    }

    public static IReadOnlyList<MonitorBrightnessResult> GetMonitorBrightness(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            return automation.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName)
                .Select(monitor => new MonitorBrightnessResult {
                    Index = monitor.Index,
                    DeviceName = monitor.DeviceName,
                    DeviceId = monitor.DeviceId,
                    IsPrimary = monitor.IsPrimary,
                    Brightness = automation.GetMonitorBrightness(monitor.DeviceId)
                })
                .ToArray();
        });
    }

    public static IReadOnlyList<MonitorWallpaperResult> GetMonitorWallpaper(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            return automation.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName)
                .Select(monitor => new MonitorWallpaperResult {
                    Index = monitor.Index,
                    DeviceName = monitor.DeviceName,
                    DeviceId = monitor.DeviceId,
                    IsPrimary = monitor.IsPrimary,
                    Wallpaper = automation.GetMonitorWallpaper(monitor.DeviceId)
                })
                .ToArray();
        });
    }

    public static IReadOnlyList<MonitorWallpaperResult> SetMonitorWallpaper(string? wallpaperPath, string? url, DesktopWallpaperPosition? wallpaperPosition = null, bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        if (string.IsNullOrWhiteSpace(wallpaperPath) && string.IsNullOrWhiteSpace(url)) {
            throw new CommandLineException("Either wallpaperPath or url must be provided.");
        }

        if (!string.IsNullOrWhiteSpace(wallpaperPath) && !string.IsNullOrWhiteSpace(url)) {
            throw new CommandLineException("Specify only one of wallpaperPath or url.");
        }

        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            IReadOnlyList<Monitor> monitors = automation.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
            foreach (Monitor monitor in monitors) {
                if (!string.IsNullOrWhiteSpace(wallpaperPath)) {
                    automation.SetMonitorWallpaper(monitor.DeviceId, wallpaperPath);
                } else {
                    automation.SetMonitorWallpaperFromUrl(monitor.DeviceId, url!);
                }
            }

            if (wallpaperPosition.HasValue) {
                automation.SetDesktopWallpaperPosition(wallpaperPosition.Value);
            }

            return monitors.Select(monitor => new MonitorWallpaperResult {
                Index = monitor.Index,
                DeviceName = monitor.DeviceName,
                DeviceId = monitor.DeviceId,
                IsPrimary = monitor.IsPrimary,
                Wallpaper = automation.GetMonitorWallpaper(monitor.DeviceId)
            }).ToArray();
        });
    }

    public static IReadOnlyList<MonitorBrightnessResult> SetMonitorBrightness(int brightness, bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            IReadOnlyList<Monitor> monitors = automation.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
            foreach (Monitor monitor in monitors) {
                automation.SetMonitorBrightness(monitor.DeviceId, brightness);
            }

            return monitors.Select(monitor => new MonitorBrightnessResult {
                Index = monitor.Index,
                DeviceName = monitor.DeviceName,
                DeviceId = monitor.DeviceId,
                IsPrimary = monitor.IsPrimary,
                Brightness = automation.GetMonitorBrightness(monitor.DeviceId)
            }).ToArray();
        });
    }

    public static DesktopColorResult GetDesktopBackgroundColor() {
        return ExecuteCore(() => MapDesktopColor(new DesktopAutomationService().GetDesktopBackgroundColor()));
    }

    public static DesktopColorResult SetDesktopBackgroundColor(uint color) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            automation.SetDesktopBackgroundColor(color);
            return MapDesktopColor(automation.GetDesktopBackgroundColor());
        });
    }

    public static DesktopWallpaperPositionResult GetDesktopWallpaperPosition() {
        return ExecuteCore(() => MapDesktopWallpaperPosition(new DesktopAutomationService().GetDesktopWallpaperPosition()));
    }

    public static DesktopWallpaperPositionResult SetDesktopWallpaperPosition(DesktopWallpaperPosition position) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            automation.SetDesktopWallpaperPosition(position);
            return MapDesktopWallpaperPosition(automation.GetDesktopWallpaperPosition());
        });
    }

    public static DesktopSlideshowResult StartDesktopSlideshow(IReadOnlyList<string> imagePaths) {
        if (imagePaths == null || imagePaths.Count == 0) {
            throw new CommandLineException("At least one slideshow image path is required.");
        }

        return ExecuteCore(() => {
            new DesktopAutomationService().StartDesktopSlideshow(imagePaths);
            return new DesktopSlideshowResult {
                Action = "start-desktop-slideshow",
                IsRunning = true,
                ImageCount = imagePaths.Count
            };
        });
    }

    public static DesktopSlideshowResult StopDesktopSlideshow() {
        return ExecuteCore(() => {
            new DesktopAutomationService().StopDesktopSlideshow();
            return new DesktopSlideshowResult {
                Action = "stop-desktop-slideshow",
                IsRunning = false
            };
        });
    }

    public static DesktopSlideshowResult AdvanceDesktopSlideshow(DesktopSlideshowDirection direction) {
        return ExecuteCore(() => {
            new DesktopAutomationService().AdvanceDesktopSlideshow(direction);
            return new DesktopSlideshowResult {
                Action = "advance-desktop-slideshow",
                IsRunning = true,
                Direction = direction.ToString()
            };
        });
    }

    public static IReadOnlyList<MonitorResult> SetMonitorPosition(int left, int top, int right, int bottom, bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            IReadOnlyList<Monitor> monitors = automation.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName, refresh: true);
            MonitorPosition position = new(left, top, right, bottom);
            foreach (Monitor monitor in monitors) {
                automation.SetMonitorPosition(monitor.DeviceId, position);
            }

            return ListMonitors(connectedOnly, primaryOnly, index, deviceId, deviceName);
        });
    }

    public static IReadOnlyList<MonitorResult> SetMonitorResolution(int width, int height, DisplayOrientation? orientation = null, bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            IReadOnlyList<Monitor> monitors = automation.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName, refresh: true);
            foreach (Monitor monitor in monitors) {
                automation.SetMonitorResolution(monitor.DeviceId, width, height);
                if (orientation.HasValue) {
                    automation.SetMonitorOrientation(monitor.DeviceId, orientation.Value);
                }
            }

            return ListMonitors(connectedOnly, primaryOnly, index, deviceId, deviceName);
        });
    }

    public static IReadOnlyList<MonitorResult> SetMonitorDpiScaling(int scalingPercent, bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            IReadOnlyList<Monitor> monitors = automation.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName, refresh: true);
            foreach (Monitor monitor in monitors) {
                automation.SetMonitorDpiScaling(monitor.DeviceId, scalingPercent);
            }

            return ListMonitors(connectedOnly, primaryOnly, index, deviceId, deviceName);
        });
    }

    public static IReadOnlyList<MonitorResult> SetTaskbarPosition(TaskbarPosition? position, bool? visible = null, bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        if (!position.HasValue && !visible.HasValue) {
            throw new CommandLineException("At least one of position or visible must be provided.");
        }

        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            IReadOnlyList<Monitor> monitors = automation.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName, refresh: true);
            foreach (Monitor monitor in monitors) {
                if (position.HasValue) {
                    automation.SetTaskbarPosition(monitor.Index, position.Value);
                }

                if (visible.HasValue) {
                    automation.SetTaskbarVisibility(monitor.Index, visible.Value);
                }
            }

            return ListMonitors(connectedOnly, primaryOnly, index, deviceId, deviceName);
        });
    }

    public static IReadOnlyList<ControlResult> ListControls(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, allControls: true)
            .Select(MapControl)
            .ToArray());
    }

    public static IReadOnlyList<ControlResult> ListControlTargets(WindowSelectionCriteria windowCriteria, string targetName, bool allWindows, bool allControls) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls)
            .Select(MapControl)
            .ToArray());
    }

    public static ControlAssertionResult ControlExists(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows) {
        return ExecuteCore(() => {
            IReadOnlyList<ControlResult> controls = new DesktopAutomationService()
                .GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, allControls: true)
                .Select(MapControl)
                .ToArray();
            return new ControlAssertionResult {
                Assertion = "control-exists",
                Matched = controls.Count > 0,
                Count = controls.Count,
                Controls = controls
            };
        });
    }

    public static ControlAssertionResult ControlTargetExists(WindowSelectionCriteria windowCriteria, string targetName, bool allWindows, bool allControls) {
        return ExecuteCore(() => {
            IReadOnlyList<ControlResult> controls = new DesktopAutomationService()
                .GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls)
                .Select(MapControl)
                .ToArray();
            return new ControlAssertionResult {
                Assertion = "control-exists",
                Matched = controls.Count > 0,
                Count = controls.Count,
                Controls = controls
            };
        });
    }

    public static IReadOnlyList<ControlDiagnosticResult> DiagnoseControls(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows, int sampleLimit, bool includeActionProbe) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetControlDiagnostics(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, sampleLimit, includeActionProbe)
            .Select(MapControlDiagnostics)
            .ToArray());
    }

    public static IReadOnlyList<ControlDiagnosticResult> DiagnoseControlTargets(WindowSelectionCriteria windowCriteria, string targetName, bool allWindows, int sampleLimit, bool includeActionProbe) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetControlTargetDiagnostics(CreateWindowQuery(windowCriteria), targetName, allWindows, sampleLimit, includeActionProbe)
            .Select(MapControlDiagnostics)
            .ToArray());
    }

    public static ControlActionResult ClickControl(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, string button, bool allWindows, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteControlMutation(
            "click-control",
            windowCriteria,
            allWindows,
            controlCriteria.All,
            artifactOptions,
            automation => automation.GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, controlCriteria.All),
            resolvedControls => DetermineControlClickSafetyMode(resolvedControls),
            automation => automation.ClickControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), mouseButton, allWindows, controlCriteria.All));
    }

    public static ControlActionResult ClickControlTarget(WindowSelectionCriteria windowCriteria, string targetName, string button, bool allWindows, bool allControls, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteControlMutation(
            "click-control",
            windowCriteria,
            allWindows,
            allControls,
            artifactOptions,
            automation => automation.GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls),
            resolvedControls => DetermineControlClickSafetyMode(resolvedControls),
            automation => automation.ClickControlTarget(CreateWindowQuery(windowCriteria), targetName, mouseButton, allWindows, allControls),
            targetName,
            "control-target");
    }

    public static ControlActionResult SetControlText(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, string text, bool allWindows, MutationArtifactOptions? artifactOptions = null) {
        if (text == null) {
            throw new CommandLineException("Text is required.");
        }

        return ExecuteControlMutation(
            "set-control-text",
            windowCriteria,
            allWindows,
            controlCriteria.All,
            artifactOptions,
            automation => automation.GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, controlCriteria.All),
            resolvedControls => DetermineControlTextSafetyMode(resolvedControls, controlCriteria.AllowForegroundInputFallback),
            automation => automation.SetControlText(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), text, allWindows, controlCriteria.All));
    }

    public static ControlActionResult SetControlTargetText(WindowSelectionCriteria windowCriteria, string targetName, string text, bool ensureForegroundWindow, bool allowForegroundInputFallback, bool allWindows, bool allControls, MutationArtifactOptions? artifactOptions = null) {
        if (text == null) {
            throw new CommandLineException("Text is required.");
        }

        return ExecuteControlMutation(
            "set-control-text",
            windowCriteria,
            allWindows,
            allControls,
            artifactOptions,
            automation => automation.GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls),
            resolvedControls => DetermineControlTextSafetyMode(resolvedControls, allowForegroundInputFallback),
            automation => automation.SetControlTargetText(CreateWindowQuery(windowCriteria), targetName, text, ensureForegroundWindow, allowForegroundInputFallback, allWindows, allControls),
            targetName,
            "control-target");
    }

    public static ControlActionResult SendControlKeys(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, IReadOnlyList<string> keys, bool allWindows, MutationArtifactOptions? artifactOptions = null) {
        VirtualKey[] virtualKeys = ParseVirtualKeys(keys);
        return ExecuteControlMutation(
            "send-control-keys",
            windowCriteria,
            allWindows,
            controlCriteria.All,
            artifactOptions,
            automation => automation.GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, controlCriteria.All),
            resolvedControls => DetermineControlKeySafetyMode(resolvedControls, controlCriteria.AllowForegroundInputFallback),
            automation => automation.SendControlKeys(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), virtualKeys, allWindows, controlCriteria.All));
    }

    public static ControlActionResult SendControlTargetKeys(WindowSelectionCriteria windowCriteria, string targetName, IReadOnlyList<string> keys, bool ensureForegroundWindow, bool allowForegroundInputFallback, bool allWindows, bool allControls, MutationArtifactOptions? artifactOptions = null) {
        VirtualKey[] virtualKeys = ParseVirtualKeys(keys);
        return ExecuteControlMutation(
            "send-control-keys",
            windowCriteria,
            allWindows,
            allControls,
            artifactOptions,
            automation => automation.GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls),
            resolvedControls => DetermineControlKeySafetyMode(resolvedControls, allowForegroundInputFallback),
            automation => automation.SendControlTargetKeys(CreateWindowQuery(windowCriteria), targetName, virtualKeys, ensureForegroundWindow, allowForegroundInputFallback, allWindows, allControls),
            targetName,
            "control-target");
    }

    public static WaitForControlResult WaitForControl(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, int timeoutMilliseconds, int intervalMilliseconds, bool allWindows) {
        return ExecuteCore(() => {
            DesktopControlWaitResult result = new DesktopAutomationService().WaitForControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), timeoutMilliseconds, intervalMilliseconds, allWindows, controlCriteria.All);
            return new WaitForControlResult {
                ElapsedMilliseconds = result.ElapsedMilliseconds,
                Count = result.Controls.Count,
                Controls = result.Controls.Select(MapControl).ToArray()
            };
        });
    }

    public static WaitForControlResult WaitForControlTarget(WindowSelectionCriteria windowCriteria, string targetName, int timeoutMilliseconds, int intervalMilliseconds, bool allWindows, bool allControls) {
        return ExecuteCore(() => {
            DesktopControlWaitResult result = new DesktopAutomationService().WaitForControlTarget(CreateWindowQuery(windowCriteria), targetName, timeoutMilliseconds, intervalMilliseconds, allWindows, allControls);
            return new WaitForControlResult {
                ElapsedMilliseconds = result.ElapsedMilliseconds,
                Count = result.Controls.Count,
                Controls = result.Controls.Select(MapControl).ToArray()
            };
        });
    }

    public static NamedStateResult SaveLayout(string name) {
        return ExecuteCore(() => {
            string path = DesktopStateStore.GetLayoutPath(name);
            new DesktopAutomationService().SaveLayout(path);
            return new NamedStateResult {
                Action = "save",
                Name = name,
                Path = path
            };
        });
    }

    public static NamedStateResult ApplyLayout(string name, bool validate) {
        return ExecuteCore(() => {
            string path = DesktopStateStore.GetLayoutPath(name);
            if (!File.Exists(path)) {
                throw new InvalidOperationException($"Named layout '{name}' was not found.");
            }

            new DesktopAutomationService().LoadLayout(path, validate);
            return new NamedStateResult {
                Action = "apply",
                Name = name,
                Path = path
            };
        });
    }

    public static IReadOnlyList<string> ListLayouts() {
        return ExecuteCore(() => DesktopStateStore.ListNames("layouts"));
    }

    public static NamedStateResult SaveSnapshot(string name) {
        return ExecuteCore(() => {
            string path = DesktopStateStore.GetSnapshotPath(name);
            new DesktopAutomationService().SaveLayout(path);
            return new NamedStateResult {
                Action = "save",
                Name = name,
                Path = path,
                Scope = "windows-only"
            };
        });
    }

    public static NamedStateResult RestoreSnapshot(string name, bool validate) {
        return ExecuteCore(() => {
            string path = DesktopStateStore.GetSnapshotPath(name);
            if (!File.Exists(path)) {
                throw new InvalidOperationException($"Named snapshot '{name}' was not found.");
            }

            new DesktopAutomationService().LoadLayout(path, validate);
            return new NamedStateResult {
                Action = "restore",
                Name = name,
                Path = path,
                Scope = "windows-only"
            };
        });
    }

    public static IReadOnlyList<string> ListSnapshots() {
        return ExecuteCore(() => DesktopStateStore.ListNames("snapshots"));
    }

    public static WindowTargetResult SaveWindowTarget(string name, string? description, int? x, int? y, double? xRatio, double? yRatio, int? width, int? height, double? widthRatio, double? heightRatio, bool clientArea) {
        return ExecuteCore(() => {
            DesktopWindowTargetDefinition definition = new DesktopWindowTargetDefinition {
                Description = description,
                X = x,
                Y = y,
                XRatio = xRatio,
                YRatio = yRatio,
                Width = width,
                Height = height,
                WidthRatio = widthRatio,
                HeightRatio = heightRatio,
                ClientArea = clientArea
            };

            DesktopWindowTargetDefinition saved = new DesktopAutomationService().SaveWindowTarget(name, definition);
            return BuildWindowTargetResult(name, DesktopStateStore.GetTargetPath(name), saved);
        });
    }

    public static IReadOnlyList<string> ListWindowTargets() {
        return ExecuteCore(() => new DesktopAutomationService().ListWindowTargets());
    }

    public static WindowTargetResult GetWindowTarget(string name) {
        return ExecuteCore(() => BuildWindowTargetResult(
            name,
            DesktopStateStore.GetTargetPath(name),
            new DesktopAutomationService().GetWindowTarget(name)));
    }

    public static IReadOnlyList<ResolvedWindowTargetResult> ResolveWindowTargets(WindowSelectionCriteria criteria, string name) {
        return ExecuteCore(() => new DesktopAutomationService()
            .ResolveWindowTargets(CreateWindowQuery(criteria), name, criteria.All)
            .Select(MapResolvedWindowTarget)
            .ToArray());
    }

    public static ControlTargetResult SaveControlTarget(string name, ControlSelectionCriteria criteria, string? description) {
        return ExecuteCore(() => {
            DesktopControlTargetDefinition definition = new DesktopControlTargetDefinition {
                Description = description,
                ClassNamePattern = criteria.ClassNamePattern,
                TextPattern = criteria.TextPattern,
                ValuePattern = criteria.ValuePattern,
                Id = criteria.Id,
                Handle = criteria.Handle,
                AutomationIdPattern = criteria.AutomationIdPattern,
                ControlTypePattern = criteria.ControlTypePattern,
                FrameworkIdPattern = criteria.FrameworkIdPattern,
                IsEnabled = criteria.IsEnabled,
                IsKeyboardFocusable = criteria.IsKeyboardFocusable,
                SupportsBackgroundClick = criteria.SupportsBackgroundClick,
                SupportsBackgroundText = criteria.SupportsBackgroundText,
                SupportsBackgroundKeys = criteria.SupportsBackgroundKeys,
                SupportsForegroundInputFallback = criteria.SupportsForegroundInputFallback,
                UseUiAutomation = criteria.UiAutomation,
                IncludeUiAutomation = criteria.IncludeUiAutomation,
                EnsureForegroundWindow = criteria.EnsureForegroundWindow
            };

            DesktopControlTargetDefinition saved = new DesktopAutomationService().SaveControlTarget(name, definition);
            return BuildControlTargetResult(name, DesktopStateStore.GetControlTargetPath(name), saved);
        });
    }

    public static IReadOnlyList<string> ListControlTargets() {
        return ExecuteCore(() => new DesktopAutomationService().ListControlTargets());
    }

    public static ControlTargetResult GetControlTarget(string name) {
        return ExecuteCore(() => BuildControlTargetResult(
            name,
            DesktopStateStore.GetControlTargetPath(name),
            new DesktopAutomationService().GetControlTarget(name)));
    }

    public static IReadOnlyList<ResolvedControlTargetResult> ResolveControlTargets(WindowSelectionCriteria windowCriteria, string name, bool allControls = false) {
        return ExecuteCore(() => new DesktopAutomationService()
            .ResolveControlTargets(CreateWindowQuery(windowCriteria), name, windowCriteria.All, allControls)
            .Select(MapResolvedControlTarget)
            .ToArray());
    }

    public static object GetCurrentSnapshotSummary() {
        return new {
            ActiveWindow = SafeGetActiveWindow(),
            Monitors = ListMonitors(connectedOnly: true),
            Windows = ListWindows(new WindowSelectionCriteria())
        };
    }

    public static ScreenshotResult CaptureDesktopScreenshot(int? monitorIndex, string? deviceId, string? deviceName, int? left, int? top, int? width, int? height, string? outputPath) {
        return ExecuteCore(() => {
            bool hasRegionSelector = left.HasValue || top.HasValue || width.HasValue || height.HasValue;
            if (hasRegionSelector && (!left.HasValue || !top.HasValue || !width.HasValue || !height.HasValue)) {
                throw new ArgumentException("Region capture requires --left, --top, --width, and --height.");
            }

            using DesktopCapture capture = hasRegionSelector
                ? new DesktopAutomationService().CaptureRegion(left!.Value, top!.Value, width!.Value, height!.Value)
                : monitorIndex.HasValue || !string.IsNullOrWhiteSpace(deviceId) || !string.IsNullOrWhiteSpace(deviceName)
                    ? new DesktopAutomationService().CaptureMonitor(monitorIndex, deviceId, deviceName)
                    : new DesktopAutomationService().CaptureDesktop();

            string prefix = capture.Kind == "monitor" && capture.MonitorIndex.HasValue ? $"monitor-{capture.MonitorIndex.Value}" : capture.Kind;
            return SaveScreenshot(capture, prefix, outputPath);
        });
    }

    public static ScreenshotResult CaptureWindowScreenshot(WindowSelectionCriteria criteria, string? outputPath) {
        return ExecuteCore(() => {
            using DesktopCapture capture = new DesktopAutomationService().CaptureWindow(CreateWindowQuery(criteria));
            string prefix = capture.Window == null ? "window" : $"window-{capture.Window.ProcessId}";
            return SaveScreenshot(capture, prefix, outputPath);
        });
    }

    public static ScreenshotResult CaptureWindowTargetScreenshot(WindowSelectionCriteria criteria, string targetName, string? outputPath) {
        return ExecuteCore(() => {
            using DesktopCapture capture = new DesktopAutomationService().CaptureWindowTarget(CreateWindowQuery(criteria), targetName);
            string prefix = capture.Window == null ? $"target-{targetName}" : $"target-{targetName}-{capture.Window.ProcessId}";
            return SaveScreenshot(capture, prefix, outputPath);
        });
    }

    public static ProcessLaunchResult LaunchProcess(string filePath, string? arguments, string? workingDirectory, int? waitForInputIdleMilliseconds, int? waitForWindowMilliseconds, int? waitForWindowIntervalMilliseconds, string? windowTitlePattern, string? windowClassNamePattern, bool requireWindow) {
        return ExecuteCore(() => {
            DesktopProcessLaunchInfo result = new DesktopAutomationService().LaunchProcess(new DesktopProcessStartOptions {
                FilePath = filePath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                WaitForInputIdleMilliseconds = waitForInputIdleMilliseconds,
                WaitForWindowMilliseconds = waitForWindowMilliseconds,
                WaitForWindowIntervalMilliseconds = waitForWindowIntervalMilliseconds,
                WindowTitlePattern = windowTitlePattern,
                WindowClassNamePattern = windowClassNamePattern,
                RequireWindow = requireWindow
            });

            return BuildProcessLaunchResult(result);
        });
    }

    public static WaitForWindowResult WaitForWindow(WindowSelectionCriteria criteria, int timeoutMilliseconds, int intervalMilliseconds) {
        return ExecuteCore(() => {
            DesktopWindowWaitResult result = new DesktopAutomationService().WaitForWindows(CreateWindowQuery(criteria), timeoutMilliseconds, intervalMilliseconds, criteria.All);
            return new WaitForWindowResult {
                ElapsedMilliseconds = result.ElapsedMilliseconds,
                Count = result.Windows.Count,
                Windows = result.Windows.Select(MapWindow).ToArray()
            };
        });
    }

    public static WindowAssertionResult WindowExists(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => {
            WindowQueryOptions query = CreateWindowQuery(criteria);
            DesktopAutomationService automation = new DesktopAutomationService();
            IReadOnlyList<WindowResult> windows = automation.GetWindows(query).Select(MapWindow).ToArray();
            return new WindowAssertionResult {
                Assertion = "window-exists",
                Matched = automation.WindowExists(query),
                Count = windows.Count,
                Windows = windows
            };
        });
    }

    public static WindowAssertionResult ActiveWindowMatches(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => {
            WindowQueryOptions query = CreateWindowQuery(criteria);
            DesktopAutomationService automation = new DesktopAutomationService();
            WindowInfo? activeWindow = automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
            bool matched = automation.ActiveWindowMatches(query);
            IReadOnlyList<WindowResult> windows = matched
                ? automation.GetWindows(new WindowQueryOptions {
                    TitlePattern = query.TitlePattern,
                    ProcessNamePattern = query.ProcessNamePattern,
                    ClassNamePattern = query.ClassNamePattern,
                    TitleRegex = query.TitleRegex,
                    ProcessId = query.ProcessId,
                    Handle = query.Handle,
                    ActiveWindow = true,
                    IncludeEmptyTitles = query.IncludeEmptyTitles ?? true,
                    IncludeHidden = true,
                    IncludeCloaked = true,
                    IncludeOwned = true,
                    IsVisible = query.IsVisible,
                    State = query.State,
                    IsTopMost = query.IsTopMost,
                    ZOrderMin = query.ZOrderMin,
                    ZOrderMax = query.ZOrderMax
                }).Select(MapWindow).ToArray()
                : Array.Empty<WindowResult>();

            return new WindowAssertionResult {
                Assertion = "active-window-matches",
                Matched = matched,
                Count = windows.Count,
                Windows = windows,
                ActiveWindow = activeWindow == null ? null : MapWindow(activeWindow)
            };
        });
    }

    private static WindowResult? SafeGetActiveWindow() {
        try {
            return GetActiveWindow();
        } catch {
            return null;
        }
    }

    private static WindowInfo? SafeGetActiveWindowInfo(DesktopAutomationService automation) {
        try {
            return automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        } catch {
            return null;
        }
    }

    private static WindowChangeResult BuildWindowChangeResult(string action, IReadOnlyList<WindowInfo> windows, int elapsedMilliseconds, string safetyMode, string? targetName, string? targetKind, IReadOnlyList<ScreenshotResult> beforeScreenshots, IReadOnlyList<ScreenshotResult> afterScreenshots, IReadOnlyList<string> artifactWarnings, WindowMutationVerificationResult? verification) {
        return new WindowChangeResult {
            Action = action,
            Success = true,
            Count = windows.Count,
            ElapsedMilliseconds = elapsedMilliseconds,
            SafetyMode = safetyMode,
            TargetName = targetName,
            TargetKind = targetKind,
            BeforeScreenshots = beforeScreenshots,
            AfterScreenshots = afterScreenshots,
            ArtifactWarnings = artifactWarnings,
            Windows = windows.Select(MapWindow).ToArray(),
            Verification = verification
        };
    }

    internal static WindowMutationVerificationResult BuildWindowPresenceVerificationResult(string action, IReadOnlyList<WindowInfo> expectedWindows, IReadOnlyList<WindowInfo> observedWindows, int tolerancePixels) {
        WindowResult[] expected = expectedWindows.Select(MapWindow).ToArray();
        WindowResult[] observed = observedWindows.Select(MapWindow).ToArray();
        int observedMatches = CountObservedHandles(expected, observed);
        bool verified = expected.Length == observedMatches;

        return new WindowMutationVerificationResult {
            Verified = verified,
            Mode = "presence",
            Summary = expected.Length == 0
                ? $"The '{action}' mutation did not report any windows, so there was nothing to verify."
                : verified
                    ? $"Observed all {observedMatches} mutated window(s) after '{action}'."
                    : $"Observed {observedMatches} of {expected.Length} mutated window(s) after '{action}'.",
            ExpectedCount = expected.Length,
            ObservedCount = observed.Length,
            MatchedCount = observedMatches,
            MismatchCount = Math.Max(0, expected.Length - observedMatches),
            TolerancePixels = tolerancePixels,
            Notes = expected.Length > observedMatches
                ? BuildMissingHandleNotes(expected, observed)
                : Array.Empty<string>(),
            ObservedWindows = observed
        };
    }

    private static WindowMutationVerificationResult BuildWindowClosedVerificationResult(string action, IReadOnlyList<WindowResult> expected, IReadOnlyList<WindowResult> observed, int tolerancePixels) {
        int observedMatches = CountObservedHandles(expected, observed);
        bool verified = observedMatches == 0;

        return new WindowMutationVerificationResult {
            Verified = verified,
            Mode = "closed",
            Summary = verified
                ? $"Observed all requested window(s) closed after '{action}'."
                : $"Observed {observedMatches} requested window(s) still present after '{action}'.",
            ExpectedCount = expected.Count,
            ObservedCount = observed.Count,
            MatchedCount = Math.Max(0, expected.Count - observedMatches),
            MismatchCount = observedMatches,
            TolerancePixels = tolerancePixels,
            Notes = verified
                ? Array.Empty<string>()
                : observed
                    .Where(window => expected.Any(expectedWindow => string.Equals(expectedWindow.Handle, window.Handle, StringComparison.OrdinalIgnoreCase)))
                    .Select(window => $"Window {window.Handle} remained observable after '{action}'.")
                    .ToArray(),
            ObservedWindows = observed
        };
    }

    internal static WindowMutationVerificationResult BuildWindowPostconditionVerificationResult(string action, IReadOnlyList<WindowInfo> expectedWindows, IReadOnlyList<WindowInfo> observedWindows, WindowInfo? activeWindow, int tolerancePixels, int? monitorIndex = null, int? x = null, int? y = null, int? width = null, int? height = null, bool requireForegroundMatch = false) {
        WindowResult[] expected = expectedWindows.Select(MapWindow).ToArray();
        WindowResult[] observed = observedWindows.Select(MapWindow).ToArray();
        WindowResult? active = activeWindow == null ? null : MapWindow(activeWindow);

        if (action.Equals("focus", StringComparison.OrdinalIgnoreCase)) {
            return BuildWindowFocusVerificationResult(expected, observed, active, tolerancePixels);
        }

        if (action.Equals("minimize", StringComparison.OrdinalIgnoreCase)) {
            return BuildWindowStateVerificationResult(action, expected, observed, active, tolerancePixels, "Minimize");
        }

        bool hasGeometryExpectation = monitorIndex.HasValue || x.HasValue || y.HasValue || width.HasValue || height.HasValue;
        if (!hasGeometryExpectation && !requireForegroundMatch) {
            return BuildWindowPresenceVerificationResult(action, expectedWindows, observedWindows, tolerancePixels);
        }

        var observedByHandle = observed.ToDictionary(window => window.Handle, StringComparer.OrdinalIgnoreCase);
        var notes = new List<string>();
        int matchedCount = 0;
        foreach (WindowResult expectedWindow in expected) {
            if (!observedByHandle.TryGetValue(expectedWindow.Handle, out WindowResult? observedWindow)) {
                notes.Add($"Window {expectedWindow.Handle} is no longer observable after '{action}'.");
                continue;
            }

            bool windowMatched = true;
            if (monitorIndex.HasValue && observedWindow.MonitorIndex != monitorIndex.Value) {
                notes.Add($"Window {observedWindow.Handle} ended on monitor {observedWindow.MonitorIndex} instead of {monitorIndex.Value}.");
                windowMatched = false;
            }
            if (x.HasValue && Math.Abs(observedWindow.Left - x.Value) > tolerancePixels) {
                notes.Add($"Window {observedWindow.Handle} left={observedWindow.Left} did not reach requested x={x.Value} within {tolerancePixels}px.");
                windowMatched = false;
            }
            if (y.HasValue && Math.Abs(observedWindow.Top - y.Value) > tolerancePixels) {
                notes.Add($"Window {observedWindow.Handle} top={observedWindow.Top} did not reach requested y={y.Value} within {tolerancePixels}px.");
                windowMatched = false;
            }
            if (width.HasValue && Math.Abs(observedWindow.Width - width.Value) > tolerancePixels) {
                notes.Add($"Window {observedWindow.Handle} width={observedWindow.Width} did not reach requested width={width.Value} within {tolerancePixels}px.");
                windowMatched = false;
            }
            if (height.HasValue && Math.Abs(observedWindow.Height - height.Value) > tolerancePixels) {
                notes.Add($"Window {observedWindow.Handle} height={observedWindow.Height} did not reach requested height={height.Value} within {tolerancePixels}px.");
                windowMatched = false;
            }

            if (windowMatched) {
                matchedCount++;
            }
        }

        bool foregroundMatched = !requireForegroundMatch || active != null && expected.Any(window => string.Equals(window.Handle, active.Handle, StringComparison.OrdinalIgnoreCase));
        if (requireForegroundMatch && !foregroundMatched) {
            notes.Add(active == null
                ? "Windows did not report an active foreground window after the mutation."
                : $"Foreground window was {active.Title} [{active.Handle}] instead of one of the mutated windows.");
        }

        bool verified = matchedCount == expected.Length && foregroundMatched;
        return new WindowMutationVerificationResult {
            Verified = verified,
            Mode = requireForegroundMatch ? "geometry-and-foreground" : "geometry",
            Summary = verified
                ? $"Observed {matchedCount} of {expected.Length} mutated window(s) at the requested post-mutation geometry."
                : $"Only {matchedCount} of {expected.Length} mutated window(s) matched the requested post-mutation geometry.",
            ExpectedCount = expected.Length,
            ObservedCount = observed.Length,
            MatchedCount = matchedCount,
            MismatchCount = Math.Max(0, expected.Length - matchedCount) + (foregroundMatched ? 0 : 1),
            TolerancePixels = tolerancePixels,
            ActiveWindow = active,
            Notes = notes,
            ObservedWindows = observed
        };
    }

    private static WindowMutationVerificationResult BuildWindowFocusVerificationResult(IReadOnlyList<WindowResult> expected, IReadOnlyList<WindowResult> observed, WindowResult? activeWindow, int tolerancePixels) {
        int observedMatches = CountObservedHandles(expected, observed);
        bool foregroundMatched = activeWindow != null && expected.Any(window => string.Equals(window.Handle, activeWindow.Handle, StringComparison.OrdinalIgnoreCase));
        var notes = new List<string>();
        if (observedMatches != expected.Count) {
            notes.AddRange(BuildMissingHandleNotes(expected, observed));
        }
        if (!foregroundMatched) {
            notes.Add(activeWindow == null
                ? "Windows did not report an active foreground window after the focus request."
                : $"Foreground window was {activeWindow.Title} [{activeWindow.Handle}] instead of one of the requested windows.");
        }

        bool verified = expected.Count == 0 || observedMatches == expected.Count && foregroundMatched;
        return new WindowMutationVerificationResult {
            Verified = verified,
            Mode = "foreground",
            Summary = verified
                ? $"Observed the requested window in the foreground after 'focus'."
                : $"DesktopManager requested focus, but the foreground window did not match the requested target.",
            ExpectedCount = expected.Count,
            ObservedCount = observed.Count,
            MatchedCount = foregroundMatched ? 1 : 0,
            MismatchCount = verified ? 0 : 1,
            TolerancePixels = tolerancePixels,
            ActiveWindow = activeWindow,
            Notes = notes,
            ObservedWindows = observed
        };
    }

    private static WindowMutationVerificationResult BuildWindowStateVerificationResult(string action, IReadOnlyList<WindowResult> expected, IReadOnlyList<WindowResult> observed, WindowResult? activeWindow, int tolerancePixels, string expectedState) {
        var observedByHandle = observed.ToDictionary(window => window.Handle, StringComparer.OrdinalIgnoreCase);
        var notes = new List<string>();
        int matchedCount = 0;
        foreach (WindowResult expectedWindow in expected) {
            if (!observedByHandle.TryGetValue(expectedWindow.Handle, out WindowResult? observedWindow)) {
                notes.Add($"Window {expectedWindow.Handle} is no longer observable after '{action}'.");
                continue;
            }

            if (string.Equals(observedWindow.State, expectedState, StringComparison.OrdinalIgnoreCase)) {
                matchedCount++;
            } else {
                notes.Add($"Window {observedWindow.Handle} reported state '{observedWindow.State ?? "<unknown>"}' instead of '{expectedState}'.");
            }
        }

        bool verified = matchedCount == expected.Count;
        return new WindowMutationVerificationResult {
            Verified = verified,
            Mode = "window-state",
            Summary = verified
                ? $"Observed {matchedCount} of {expected.Count} mutated window(s) in state '{expectedState}'."
                : $"Only {matchedCount} of {expected.Count} mutated window(s) reported state '{expectedState}'.",
            ExpectedCount = expected.Count,
            ObservedCount = observed.Count,
            MatchedCount = matchedCount,
            MismatchCount = Math.Max(0, expected.Count - matchedCount),
            TolerancePixels = tolerancePixels,
            ActiveWindow = activeWindow,
            Notes = notes,
            ObservedWindows = observed
        };
    }

    private static WindowMutationVerificationResult BuildWindowBooleanVerificationResult(string action, IReadOnlyList<WindowResult> expected, IReadOnlyList<WindowResult> observed, int tolerancePixels, string propertyName, Func<WindowResult, bool> selector, bool expectedValue) {
        var observedByHandle = observed.ToDictionary(window => window.Handle, StringComparer.OrdinalIgnoreCase);
        var notes = new List<string>();
        int matchedCount = 0;
        foreach (WindowResult expectedWindow in expected) {
            if (!observedByHandle.TryGetValue(expectedWindow.Handle, out WindowResult? observedWindow)) {
                notes.Add($"Window {expectedWindow.Handle} is no longer observable after '{action}'.");
                continue;
            }

            if (selector(observedWindow) == expectedValue) {
                matchedCount++;
            } else {
                notes.Add($"Window {observedWindow.Handle} reported {propertyName}={selector(observedWindow)} instead of {expectedValue}.");
            }
        }

        bool verified = matchedCount == expected.Count;
        return new WindowMutationVerificationResult {
            Verified = verified,
            Mode = propertyName,
            Summary = verified
                ? $"Observed {matchedCount} of {expected.Count} mutated window(s) with {propertyName}={expectedValue}."
                : $"Only {matchedCount} of {expected.Count} mutated window(s) reported {propertyName}={expectedValue}.",
            ExpectedCount = expected.Count,
            ObservedCount = observed.Count,
            MatchedCount = matchedCount,
            MismatchCount = Math.Max(0, expected.Count - matchedCount),
            TolerancePixels = tolerancePixels,
            Notes = notes,
            ObservedWindows = observed
        };
    }

    private static int CountObservedHandles(IReadOnlyList<WindowResult> expected, IReadOnlyList<WindowResult> observed) {
        var observedHandles = new HashSet<string>(observed.Select(window => window.Handle), StringComparer.OrdinalIgnoreCase);
        return expected.Count(window => observedHandles.Contains(window.Handle));
    }

    private static IReadOnlyList<string> BuildMissingHandleNotes(IReadOnlyList<WindowResult> expected, IReadOnlyList<WindowResult> observed) {
        var observedHandles = new HashSet<string>(observed.Select(window => window.Handle), StringComparer.OrdinalIgnoreCase);
        return expected
            .Where(window => !observedHandles.Contains(window.Handle))
            .Select(window => $"Window {window.Handle} is no longer observable after the mutation.")
            .ToArray();
    }

    internal static ProcessLaunchResult BuildProcessLaunchResult(DesktopProcessLaunchInfo result) {
        return new ProcessLaunchResult {
            FilePath = result.FilePath,
            Arguments = result.Arguments,
            WorkingDirectory = result.WorkingDirectory,
            ProcessId = result.ProcessId,
            ResolvedProcessId = result.ResolvedProcessId,
            HasExited = result.HasExited,
            MainWindow = result.MainWindow == null ? null : MapWindow(result.MainWindow)
        };
    }

    private static WindowResult MapWindow(WindowInfo window) {
        return new WindowResult {
            Title = window.Title,
            Handle = $"0x{window.Handle.ToInt64():X}",
            ProcessId = window.ProcessId,
            ThreadId = window.ThreadId,
            IsVisible = window.IsVisible,
            IsTopMost = window.IsTopMost,
            State = window.State?.ToString(),
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height,
            MonitorIndex = window.MonitorIndex,
            MonitorDeviceName = window.MonitorDeviceName
        };
    }

    internal static WindowProcessInfoResult MapWindowProcessInfo(WindowInfo window, WindowProcessInfo info, bool owner) {
        return new WindowProcessInfoResult {
            Window = MapWindow(window),
            IsOwnerProcess = owner,
            ProcessId = info.ProcessId,
            ThreadId = info.ThreadId,
            ProcessName = info.ProcessName,
            ProcessPath = info.ProcessPath,
            IsElevated = info.IsElevated,
            IsWow64 = info.IsWow64
        };
    }

    private static MouseStateResult MapMouseState(DesktopMouseState state) {
        return new MouseStateResult {
            X = state.X,
            Y = state.Y,
            IsLeftButtonDown = state.IsLeftButtonDown,
            IsRightButtonDown = state.IsRightButtonDown,
            IsCursorVisible = state.IsCursorVisible,
            CursorHandle = state.CursorHandle == IntPtr.Zero ? string.Empty : $"0x{state.CursorHandle.ToInt64():X}"
        };
    }

    private static FocusedControlObservationResult MapFocusedControlObservation(DesktopFocusedControlObservation observation) {
        return new FocusedControlObservationResult {
            WindowHandle = $"0x{observation.WindowHandle.ToInt64():X}",
            WindowTitle = observation.WindowTitle,
            FocusedHandle = $"0x{observation.FocusedHandle.ToInt64():X}",
            ClassName = observation.ClassName,
            AutomationId = observation.AutomationId,
            ControlType = observation.ControlType,
            Text = observation.Text,
            Value = observation.Value,
            IsKeyboardFocusable = observation.IsKeyboardFocusable,
            IsEnabled = observation.IsEnabled
        };
    }

    private static WindowTextObservationResult MapWindowTextObservation(DesktopWindowTextObservation observation) {
        return new WindowTextObservationResult {
            WindowHandle = $"0x{observation.WindowHandle.ToInt64():X}",
            WindowTitle = observation.WindowTitle,
            ControlHandle = observation.ControlHandle == IntPtr.Zero ? string.Empty : $"0x{observation.ControlHandle.ToInt64():X}",
            ControlClassName = observation.ControlClassName,
            ControlAutomationId = observation.ControlAutomationId,
            ControlType = observation.ControlType,
            Value = observation.Value,
            Source = observation.Source,
            ContainsExpected = observation.ContainsExpected,
            IsTruncated = observation.IsTruncated
        };
    }

    private static WaitCompletionResult MapWaitCompletion(DesktopWaitResult result) {
        return new WaitCompletionResult {
            ElapsedMilliseconds = result.ElapsedMilliseconds
        };
    }

    private static ControlStateResult MapControlState(DesktopControlState state) {
        return new ControlStateResult {
            WindowHandle = state.WindowHandle == IntPtr.Zero ? string.Empty : $"0x{state.WindowHandle.ToInt64():X}",
            ControlHandle = state.ControlHandle == IntPtr.Zero ? string.Empty : $"0x{state.ControlHandle.ToInt64():X}",
            ClassName = state.ClassName,
            AutomationId = state.AutomationId,
            ControlType = state.ControlType,
            Text = state.Text,
            Value = state.Value,
            IsEnabled = state.IsEnabled,
            IsVisible = state.IsVisible,
            IsFocused = state.IsFocused,
            IsKeyboardFocusable = state.IsKeyboardFocusable,
            IsOffscreen = state.IsOffscreen,
            SupportsBackgroundClick = state.SupportsBackgroundClick,
            SupportsBackgroundText = state.SupportsBackgroundText,
            SupportsBackgroundKeys = state.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = state.SupportsForegroundInputFallback,
            Left = state.Left,
            Top = state.Top,
            Width = state.Width,
            Height = state.Height
        };
    }

    private static DesktopTextObservationOptions? CreateTextObservationOptions(int? maxLength, int? retryCount, int? retryDelayMilliseconds) {
        if (!maxLength.HasValue && !retryCount.HasValue && !retryDelayMilliseconds.HasValue) {
            return null;
        }

        return new DesktopTextObservationOptions {
            MaxObservedTextLength = maxLength ?? 2048,
            RetryCount = retryCount ?? 5,
            RetryDelayMilliseconds = retryDelayMilliseconds ?? 50
        };
    }

    internal static WindowGeometryResult MapWindowGeometry(DesktopWindowGeometry geometry) {
        return new WindowGeometryResult {
            Window = MapWindow(geometry.Window),
            WindowLeft = geometry.WindowLeft,
            WindowTop = geometry.WindowTop,
            WindowWidth = geometry.WindowWidth,
            WindowHeight = geometry.WindowHeight,
            ClientLeft = geometry.ClientLeft,
            ClientTop = geometry.ClientTop,
            ClientWidth = geometry.ClientWidth,
            ClientHeight = geometry.ClientHeight,
            ClientOffsetLeft = geometry.ClientOffsetLeft,
            ClientOffsetTop = geometry.ClientOffsetTop
        };
    }

    private static WindowTargetResult BuildWindowTargetResult(string name, string path, DesktopWindowTargetDefinition target) {
        return new WindowTargetResult {
            Name = name,
            Path = path,
            Target = MapWindowTargetDefinition(target)
        };
    }

    private static ControlTargetResult BuildControlTargetResult(string name, string path, DesktopControlTargetDefinition target) {
        return new ControlTargetResult {
            Name = name,
            Path = path,
            Target = MapControlTargetDefinition(target)
        };
    }

    private static WindowTargetDefinitionResult MapWindowTargetDefinition(DesktopWindowTargetDefinition target) {
        return new WindowTargetDefinitionResult {
            Description = target.Description,
            X = target.X,
            Y = target.Y,
            XRatio = target.XRatio,
            YRatio = target.YRatio,
            Width = target.Width,
            Height = target.Height,
            WidthRatio = target.WidthRatio,
            HeightRatio = target.HeightRatio,
            ClientArea = target.ClientArea
        };
    }

    private static ControlTargetDefinitionResult MapControlTargetDefinition(DesktopControlTargetDefinition target) {
        return new ControlTargetDefinitionResult {
            Description = target.Description,
            ClassNamePattern = target.ClassNamePattern,
            TextPattern = target.TextPattern,
            ValuePattern = target.ValuePattern,
            Id = target.Id,
            Handle = target.Handle,
            AutomationIdPattern = target.AutomationIdPattern,
            ControlTypePattern = target.ControlTypePattern,
            FrameworkIdPattern = target.FrameworkIdPattern,
            IsEnabled = target.IsEnabled,
            IsKeyboardFocusable = target.IsKeyboardFocusable,
            SupportsBackgroundClick = target.SupportsBackgroundClick,
            SupportsBackgroundText = target.SupportsBackgroundText,
            SupportsBackgroundKeys = target.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = target.SupportsForegroundInputFallback,
            UseUiAutomation = target.UseUiAutomation,
            IncludeUiAutomation = target.IncludeUiAutomation,
            EnsureForegroundWindow = target.EnsureForegroundWindow
        };
    }

    internal static ResolvedWindowTargetResult MapResolvedWindowTarget(DesktopResolvedWindowTarget target) {
        return new ResolvedWindowTargetResult {
            Name = target.Name,
            Target = MapWindowTargetDefinition(target.Definition),
            Window = MapWindow(target.Geometry.Window),
            Geometry = MapWindowGeometry(target.Geometry),
            RelativeX = target.RelativeX,
            RelativeY = target.RelativeY,
            RelativeWidth = target.RelativeWidth,
            RelativeHeight = target.RelativeHeight,
            ScreenX = target.ScreenX,
            ScreenY = target.ScreenY,
            ScreenWidth = target.ScreenWidth,
            ScreenHeight = target.ScreenHeight
        };
    }

    internal static ResolvedControlTargetResult MapResolvedControlTarget(DesktopResolvedControlTarget target) {
        return new ResolvedControlTargetResult {
            Name = target.Name,
            Target = MapControlTargetDefinition(target.Definition),
            Window = MapWindow(target.Window),
            Control = MapControl(target.Window, target.Control)
        };
    }

    private static ControlResult MapControl(WindowControlTargetInfo target) {
        return MapControl(target.Window, target.Control);
    }

    private static ControlResult MapControl(WindowInfo window, WindowControlInfo control) {
        return new ControlResult {
            Handle = $"0x{control.Handle.ToInt64():X}",
            ClassName = control.ClassName,
            Id = control.Id,
            Text = !string.IsNullOrWhiteSpace(control.Text) ? control.Text : control.Handle != IntPtr.Zero ? WindowTextHelper.GetWindowText(control.Handle) : string.Empty,
            Value = !string.IsNullOrWhiteSpace(control.Value) ? control.Value : !string.IsNullOrWhiteSpace(control.Text) ? control.Text : control.Handle != IntPtr.Zero ? WindowTextHelper.GetWindowText(control.Handle) : string.Empty,
            Source = control.Source.ToString(),
            AutomationId = control.AutomationId,
            ControlType = control.ControlType,
            FrameworkId = control.FrameworkId,
            IsKeyboardFocusable = control.IsKeyboardFocusable,
            IsEnabled = control.IsEnabled,
            IsOffscreen = control.IsOffscreen,
            SupportsBackgroundClick = control.SupportsBackgroundClick,
            SupportsBackgroundText = control.SupportsBackgroundText,
            SupportsBackgroundKeys = control.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = control.SupportsForegroundInputFallback,
            Left = control.Left,
            Top = control.Top,
            Width = control.Width,
            Height = control.Height,
            ParentWindow = MapWindow(window)
        };
    }

    internal static ControlDiagnosticResult MapControlDiagnostics(DesktopControlDiscoveryDiagnostics diagnostics) {
        return new ControlDiagnosticResult {
            Window = MapWindow(diagnostics.Window),
            RequiresUiAutomation = diagnostics.RequiresUiAutomation,
            UseUiAutomation = diagnostics.UseUiAutomation,
            IncludeUiAutomation = diagnostics.IncludeUiAutomation,
            EnsureForegroundWindow = diagnostics.EnsureForegroundWindow,
            UiAutomationAvailable = diagnostics.UiAutomationAvailable,
            ElapsedMilliseconds = diagnostics.ElapsedMilliseconds,
            PreparationAttempted = diagnostics.PreparationAttempted,
            PreparationSucceeded = diagnostics.PreparationSucceeded,
            UiAutomationFallbackRootCount = diagnostics.UiAutomationFallbackRootCount,
            UsedUiAutomationFallbackRoots = diagnostics.UsedUiAutomationFallbackRoots,
            UsedCachedUiAutomationControls = diagnostics.UsedCachedUiAutomationControls,
            UsedPreferredUiAutomationRoot = diagnostics.UsedPreferredUiAutomationRoot,
            PreferredUiAutomationRootHandle = diagnostics.PreferredUiAutomationRootHandle == IntPtr.Zero ? string.Empty : $"0x{diagnostics.PreferredUiAutomationRootHandle.ToInt64():X}",
            EffectiveSource = diagnostics.EffectiveSource,
            Win32ControlCount = diagnostics.Win32ControlCount,
            UiAutomationControlCount = diagnostics.UiAutomationControlCount,
            EffectiveControlCount = diagnostics.EffectiveControlCount,
            MatchedControlCount = diagnostics.MatchedControlCount,
            SampleControls = diagnostics.SampleControls.Select(control => MapControl(diagnostics.Window, control)).ToArray(),
            UiAutomationRoots = diagnostics.UiAutomationRoots.Select(root => new UiAutomationRootDiagnosticResult {
                Order = root.Order,
                Handle = $"0x{root.Handle.ToInt64():X}",
                ClassName = root.ClassName,
                IsPrimaryRoot = root.IsPrimaryRoot,
                IsPreferredRoot = root.IsPreferredRoot,
                UsedCachedControls = root.UsedCachedControls,
                IncludeRoot = root.IncludeRoot,
                ElementResolved = root.ElementResolved,
                ControlCount = root.ControlCount,
                Error = root.Error,
                SampleControls = root.SampleControls.Select(control => MapControl(diagnostics.Window, control)).ToArray()
            }).ToArray(),
            UiAutomationActionProbe = diagnostics.UiAutomationActionProbe == null ? null : new UiAutomationActionDiagnosticResult {
                Attempted = diagnostics.UiAutomationActionProbe.Attempted,
                Resolved = diagnostics.UiAutomationActionProbe.Resolved,
                UsedCachedActionMatch = diagnostics.UiAutomationActionProbe.UsedCachedActionMatch,
                UsedPreferredRoot = diagnostics.UiAutomationActionProbe.UsedPreferredRoot,
                RootHandle = diagnostics.UiAutomationActionProbe.RootHandle == IntPtr.Zero ? string.Empty : $"0x{diagnostics.UiAutomationActionProbe.RootHandle.ToInt64():X}",
                Score = diagnostics.UiAutomationActionProbe.Score,
                SearchMode = diagnostics.UiAutomationActionProbe.SearchMode,
                ElapsedMilliseconds = diagnostics.UiAutomationActionProbe.ElapsedMilliseconds
            }
        };
    }

    private static ControlActionResult BuildControlActionResult(string action, IReadOnlyList<WindowControlTargetInfo> controls, int elapsedMilliseconds, string safetyMode, string? targetName, string? targetKind, IReadOnlyList<ScreenshotResult> beforeScreenshots, IReadOnlyList<ScreenshotResult> afterScreenshots, IReadOnlyList<string> artifactWarnings) {
        return new ControlActionResult {
            Action = action,
            Success = true,
            Count = controls.Count,
            ElapsedMilliseconds = elapsedMilliseconds,
            SafetyMode = safetyMode,
            TargetName = targetName,
            TargetKind = targetKind,
            BeforeScreenshots = beforeScreenshots,
            AfterScreenshots = afterScreenshots,
            ArtifactWarnings = artifactWarnings,
            Controls = controls.Select(MapControl).ToArray()
        };
    }

    private static MouseButton ParseMouseButton(string? value) {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("left", StringComparison.OrdinalIgnoreCase)) {
            return MouseButton.Left;
        }

        if (value.Equals("right", StringComparison.OrdinalIgnoreCase)) {
            return MouseButton.Right;
        }

        throw new CommandLineException($"Unsupported mouse button '{value}'.");
    }

    private static VirtualKey[] ParseVirtualKeys(IReadOnlyList<string> values) {
        if (values == null || values.Count == 0) {
            throw new CommandLineException("At least one key is required.");
        }

        var keys = new List<VirtualKey>();
        foreach (string raw in values) {
            foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                keys.Add(ParseVirtualKey(part));
            }
        }

        if (keys.Count == 0) {
            throw new CommandLineException("At least one key is required.");
        }

        return keys.ToArray();
    }

    private static VirtualKey ParseVirtualKey(string value) {
        if (Enum.TryParse<VirtualKey>(value, ignoreCase: true, out VirtualKey parsed)) {
            return parsed;
        }

        if (value.Length == 1) {
            char character = char.ToUpperInvariant(value[0]);
            string enumName = character switch {
                >= 'A' and <= 'Z' => $"VK_{character}",
                >= '0' and <= '9' => $"VK_{character}",
                ' ' => "VK_SPACE",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(enumName) && Enum.TryParse<VirtualKey>(enumName, ignoreCase: true, out parsed)) {
                return parsed;
            }
        }

        throw new CommandLineException($"Unsupported key '{value}'.");
    }

    private static ScreenshotResult SaveScreenshot(DesktopCapture capture, string prefix, string? outputPath) {
        string path = DesktopStateStore.ResolveCapturePath(prefix, outputPath);
        capture.Save(path);
        return new ScreenshotResult {
            Kind = capture.Kind,
            Path = path,
            Width = capture.Width,
            Height = capture.Height,
            MonitorIndex = capture.MonitorIndex,
            MonitorDeviceName = capture.MonitorDeviceName,
            Window = capture.Window == null ? null : MapWindow(capture.Window),
            Geometry = capture.Geometry == null ? null : MapWindowGeometry(capture.Geometry)
        };
    }

    private static bool HasExplicitSelector(WindowSelectionCriteria criteria) {
        return !string.IsNullOrWhiteSpace(criteria.TitlePattern) && criteria.TitlePattern != "*" ||
            !string.IsNullOrWhiteSpace(criteria.ProcessNamePattern) && criteria.ProcessNamePattern != "*" ||
            !string.IsNullOrWhiteSpace(criteria.ClassNamePattern) && criteria.ClassNamePattern != "*" ||
            criteria.ProcessId.HasValue ||
            criteria.Active ||
            !string.IsNullOrWhiteSpace(criteria.Handle);
    }

    internal static WindowQueryOptions CreateWindowQuery(WindowSelectionCriteria criteria) {
        return new WindowQueryOptions {
            TitlePattern = criteria.TitlePattern,
            ProcessNamePattern = criteria.ProcessNamePattern,
            ClassNamePattern = criteria.ClassNamePattern,
            Handle = string.IsNullOrWhiteSpace(criteria.Handle) ? null : DesktopHandleParser.Parse(criteria.Handle),
            ActiveWindow = criteria.Active,
            IncludeHidden = criteria.IncludeHidden,
            IncludeCloaked = criteria.IncludeCloaked,
            IncludeOwned = criteria.IncludeOwned,
            IncludeEmptyTitles = criteria.IncludeEmptyTitles ? true : HasExplicitSelector(criteria) ? null : false,
            ProcessId = criteria.ProcessId ?? 0
        };
    }

    internal static WindowControlQueryOptions CreateControlQuery(ControlSelectionCriteria criteria) {
        return new WindowControlQueryOptions {
            ClassNamePattern = criteria.ClassNamePattern,
            TextPattern = criteria.TextPattern,
            Id = criteria.Id,
            Handle = string.IsNullOrWhiteSpace(criteria.Handle) ? null : DesktopHandleParser.Parse(criteria.Handle),
            AutomationIdPattern = criteria.AutomationIdPattern,
            ControlTypePattern = criteria.ControlTypePattern,
            FrameworkIdPattern = criteria.FrameworkIdPattern,
            ValuePattern = criteria.ValuePattern,
            IsEnabled = criteria.IsEnabled,
            IsKeyboardFocusable = criteria.IsKeyboardFocusable,
            SupportsBackgroundClick = criteria.SupportsBackgroundClick,
            SupportsBackgroundText = criteria.SupportsBackgroundText,
            SupportsBackgroundKeys = criteria.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = criteria.SupportsForegroundInputFallback,
            EnsureForegroundWindow = criteria.EnsureForegroundWindow,
            AllowForegroundInputFallback = criteria.AllowForegroundInputFallback,
            UseUiAutomation = criteria.UiAutomation,
            IncludeUiAutomation = criteria.IncludeUiAutomation
        };
    }

    private static DesktopColorResult MapDesktopColor(uint color) {
        return new DesktopColorResult {
            Value = color,
            HexValue = "0x" + color.ToString("X6")
        };
    }

    private static DesktopWallpaperPositionResult MapDesktopWallpaperPosition(DesktopWallpaperPosition position) {
        return new DesktopWallpaperPositionResult {
            Position = position.ToString()
        };
    }

    private static T ExecuteCore<T>(Func<T> operation) {
        try {
            return operation();
        } catch (ArgumentOutOfRangeException ex) {
            throw new CommandLineException(ex.Message);
        } catch (ArgumentException ex) {
            throw new CommandLineException(ex.Message);
        } catch (DirectoryNotFoundException ex) {
            throw new CommandLineException(ex.Message);
        } catch (InvalidDataException ex) {
            throw new CommandLineException(ex.Message);
        } catch (FileNotFoundException ex) {
            throw new CommandLineException(ex.Message);
        } catch (Win32Exception ex) {
            throw new CommandLineException(ex.Message);
        } catch (InvalidOperationException ex) {
            throw new CommandLineException(ex.Message);
        } catch (TimeoutException ex) {
            throw new CommandLineException(ex.Message);
        }
    }

    private static bool TryParseSnapPosition(string value, out SnapPosition position) {
        switch (value.ToLowerInvariant()) {
            case "left":
                position = SnapPosition.Left;
                return true;
            case "right":
                position = SnapPosition.Right;
                return true;
            case "top-left":
                position = SnapPosition.TopLeft;
                return true;
            case "top-right":
                position = SnapPosition.TopRight;
                return true;
            case "bottom-left":
                position = SnapPosition.BottomLeft;
                return true;
            case "bottom-right":
                position = SnapPosition.BottomRight;
                return true;
            default:
                position = default;
                return false;
        }
    }

}
