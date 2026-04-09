using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace DesktopManager.Cli;

internal static partial class DesktopOperations {
    private static WindowChangeResult ExecuteWindowMutation(string action, WindowSelectionCriteria criteria, string safetyMode, MutationArtifactOptions? artifactOptions, Func<DesktopAutomationService, IReadOnlyList<WindowInfo>> mutation, string? targetName = null, string? targetKind = null, Func<DesktopAutomationService, IReadOnlyList<ResolvedPointerTargetResult>>? resolveTargets = null, Func<DesktopAutomationService, IReadOnlyList<WindowInfo>, MutationArtifactOptions, WindowMutationVerificationResult?>? verify = null) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            var warnings = new List<string>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            MutationArtifactOptions options = artifactOptions ?? new MutationArtifactOptions();
            IReadOnlyList<ResolvedPointerTargetResult> resolvedTargets = resolveTargets?.Invoke(automation) ?? Array.Empty<ResolvedPointerTargetResult>();
            VisualObservationBaseline? visualBaseline = PrepareWindowVisualBaseline(automation, ResolveWindowsForMutation(automation, criteria), options, warnings);

            IReadOnlyList<ScreenshotResult> beforeScreenshots = options.CaptureBefore
                ? CaptureWindowArtifacts(automation, ResolveWindowsForMutation(automation, criteria), action, "before", options, warnings)
                : Array.Empty<ScreenshotResult>();

            IReadOnlyList<WindowInfo> windows = mutation(automation);
            WindowMutationVerificationResult? verification = VerifyWindowMutation(automation, action, windows, options, verify, warnings);
            WindowVisualChangeResult? visualChange = ObserveVisualChange(automation, windows, visualBaseline, options, warnings);

            IReadOnlyList<ScreenshotResult> afterScreenshots = options.CaptureAfter
                ? CaptureWindowArtifacts(automation, windows, action, "after", options, warnings)
                : Array.Empty<ScreenshotResult>();

            stopwatch.Stop();
            visualBaseline?.Dispose();
            return BuildWindowChangeResult(action, windows, (int)stopwatch.ElapsedMilliseconds, safetyMode, targetName, targetKind, resolvedTargets, beforeScreenshots, afterScreenshots, warnings, verification, visualChange);
        });
    }

    private static ControlActionResult ExecuteControlMutation(string action, WindowSelectionCriteria windowCriteria, bool allWindows, bool allControls, MutationArtifactOptions? artifactOptions, Func<DesktopAutomationService, IReadOnlyList<WindowControlTargetInfo>> resolveControls, Func<IReadOnlyList<WindowControlTargetInfo>, string> determineSafetyMode, Func<DesktopAutomationService, IReadOnlyList<WindowControlTargetInfo>> mutation, string? targetName = null, string? targetKind = null) {
        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            var warnings = new List<string>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            MutationArtifactOptions options = artifactOptions ?? new MutationArtifactOptions();

            IReadOnlyList<WindowControlTargetInfo> resolvedControls = options.CaptureBefore || options.WaitForVisualChange
                ? resolveControls(automation)
                : Array.Empty<WindowControlTargetInfo>();
            VisualObservationBaseline? visualBaseline = PrepareControlVisualBaseline(automation, LimitControlsForMutation(resolvedControls, allWindows, allControls), options, warnings);
            IReadOnlyList<ScreenshotResult> beforeScreenshots = options.CaptureBefore
                ? CaptureControlArtifacts(automation, LimitControlsForMutation(resolvedControls, allWindows, allControls), action, "before", options, warnings)
                : Array.Empty<ScreenshotResult>();

            IReadOnlyList<WindowControlTargetInfo> controls = mutation(automation);
            IReadOnlyList<WindowControlTargetInfo> safetyControls = resolvedControls.Count > 0 ? resolvedControls : controls;
            string safetyMode = determineSafetyMode(safetyControls);
            WindowVisualChangeResult? visualChange = ObserveVisualChange(
                automation,
                controls.Select(static control => control.Window).ToArray(),
                visualBaseline,
                options,
                warnings);

            IReadOnlyList<ScreenshotResult> afterScreenshots = options.CaptureAfter
                ? CaptureControlArtifacts(automation, controls, action, "after", options, warnings)
                : Array.Empty<ScreenshotResult>();

            stopwatch.Stop();
            visualBaseline?.Dispose();
            return BuildControlActionResult(action, controls, (int)stopwatch.ElapsedMilliseconds, safetyMode, targetName, targetKind, beforeScreenshots, afterScreenshots, warnings, visualChange);
        });
    }

    private static VisualObservationBaseline? PrepareWindowVisualBaseline(DesktopAutomationService automation, IReadOnlyList<WindowInfo> windows, MutationArtifactOptions options, List<string> warnings) {
        if (!options.WaitForVisualChange) {
            return null;
        }

        return PrepareVisualBaseline(automation, windows, options, warnings);
    }

    private static VisualObservationBaseline? PrepareControlVisualBaseline(DesktopAutomationService automation, IReadOnlyList<WindowControlTargetInfo> controls, MutationArtifactOptions options, List<string> warnings) {
        if (!options.WaitForVisualChange) {
            return null;
        }

        if (controls.Count == 0) {
            warnings.Add("Visual change verification was requested, but no controls could be resolved before the mutation.");
            return null;
        }

        WindowInfo[] windows = controls
            .Select(static control => control.Window)
            .GroupBy(static window => window.Handle)
            .Select(static group => group.First())
            .ToArray();
        return PrepareVisualBaseline(automation, windows, options, warnings);
    }

    private static VisualObservationBaseline? PrepareVisualBaseline(DesktopAutomationService automation, IReadOnlyList<WindowInfo> windows, MutationArtifactOptions options, List<string> warnings) {
        if (windows.Count == 0) {
            warnings.Add("Visual change verification was requested, but no matching window could be resolved before the mutation.");
            return null;
        }

        WindowInfo window = windows[0];
        if (windows.Count > 1) {
            warnings.Add($"Visual change verification currently observes only the first mutated window. Using {FormatWindowLabel(window)}.");
        }

        try {
            DesktopCapture capture = !string.IsNullOrWhiteSpace(options.VisualTargetName)
                ? automation.CaptureWindowTarget(new WindowQueryOptions {
                    Handle = window.Handle,
                    IncludeHidden = true,
                    IncludeCloaked = true,
                    IncludeOwned = true,
                    IncludeEmptyTitles = true
                }, options.VisualTargetName)
                : options.VisualClientArea
                    ? automation.CaptureWindowClientArea(window.Handle)
                    : automation.CaptureWindow(window.Handle);
            return new VisualObservationBaseline(window, capture);
        } catch (Exception ex) when (IsArtifactWarning(ex) || ex is TimeoutException || ex is Win32Exception) {
            warnings.Add($"Failed to capture the visual baseline for {FormatWindowLabel(window)}: {ex.Message}");
            return null;
        }
    }

    private static WindowVisualChangeResult? ObserveVisualChange(DesktopAutomationService automation, IReadOnlyList<WindowInfo> windows, VisualObservationBaseline? baseline, MutationArtifactOptions options, List<string> warnings) {
        if (!options.WaitForVisualChange || baseline == null) {
            return null;
        }

        try {
            DesktopWindowVisualChangeObservation observation = automation.WaitForWindowVisualChange(
                baseline.Window,
                baseline.Capture,
                options.VisualTargetName,
                options.VisualClientArea,
                options.VisualTimeoutMilliseconds,
                options.VisualIntervalMilliseconds,
                options.VisualMinimumChangedRatio,
                options.VisualDifferenceThreshold);
            return MapWindowVisualChangeObservation(observation);
        } catch (Exception ex) when (IsArtifactWarning(ex) || ex is TimeoutException || ex is Win32Exception) {
            if (ex is TimeoutException) {
                warnings.Add($"Timed out after {options.VisualTimeoutMilliseconds}ms waiting for a visible change in {FormatWindowLabel(baseline.Window)}.");
                return null;
            }

            warnings.Add($"Visual change verification failed for {FormatWindowLabel(baseline.Window)}: {ex.Message}");
            return null;
        }
    }

    private static IReadOnlyList<WindowInfo> ResolveWindowsForMutation(DesktopAutomationService automation, WindowSelectionCriteria criteria) {
        IReadOnlyList<WindowInfo> windows = automation.GetWindows(CreateWindowQuery(criteria));
        if (criteria.All || windows.Count <= 1) {
            return windows;
        }

        return new[] { windows[0] };
    }

    private static IReadOnlyList<WindowControlTargetInfo> LimitControlsForMutation(IReadOnlyList<WindowControlTargetInfo> controls, bool allWindows, bool allControls) {
        if (allControls || controls.Count <= 1) {
            return controls;
        }

        return new[] { controls[0] };
    }

    private static IReadOnlyList<ScreenshotResult> CaptureWindowArtifacts(DesktopAutomationService automation, IReadOnlyList<WindowInfo> windows, string action, string phase, MutationArtifactOptions options, List<string> warnings) {
        if (windows.Count == 0) {
            return Array.Empty<ScreenshotResult>();
        }

        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        var screenshots = new List<ScreenshotResult>(windows.Count);
        for (int index = 0; index < windows.Count; index++) {
            WindowInfo window = windows[index];
            try {
                using DesktopCapture capture = automation.CaptureWindow(new WindowQueryOptions {
                    Handle = window.Handle,
                    IncludeHidden = true,
                    IncludeCloaked = true,
                    IncludeOwned = true,
                    IncludeEmptyTitles = true
                });
                string prefix = BuildArtifactPrefix(action, phase, window.Handle, stamp, index);
                screenshots.Add(SaveScreenshot(capture, prefix, ResolveArtifactOutputPath(options.ArtifactDirectory, prefix)));
            } catch (Exception ex) when (IsArtifactWarning(ex)) {
                warnings.Add($"Failed to capture {phase} window screenshot for {FormatWindowLabel(window)}: {ex.Message}");
            }
        }

        return screenshots;
    }

    private static IReadOnlyList<ScreenshotResult> CaptureControlArtifacts(DesktopAutomationService automation, IReadOnlyList<WindowControlTargetInfo> controls, string action, string phase, MutationArtifactOptions options, List<string> warnings) {
        if (controls.Count == 0) {
            return Array.Empty<ScreenshotResult>();
        }

        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        WindowControlTargetInfo[] uniqueControls = controls
            .GroupBy(control => control.Window.Handle)
            .Select(group => group.First())
            .ToArray();

        var screenshots = new List<ScreenshotResult>(uniqueControls.Length);
        for (int index = 0; index < uniqueControls.Length; index++) {
            WindowControlTargetInfo target = uniqueControls[index];
            try {
                using DesktopCapture capture = automation.CaptureWindow(new WindowQueryOptions {
                    Handle = target.Window.Handle,
                    IncludeHidden = true,
                    IncludeCloaked = true,
                    IncludeOwned = true,
                    IncludeEmptyTitles = true
                });
                string prefix = BuildArtifactPrefix(action, phase, target.Window.Handle, stamp, index);
                screenshots.Add(SaveScreenshot(capture, prefix, ResolveArtifactOutputPath(options.ArtifactDirectory, prefix)));
            } catch (Exception ex) when (IsArtifactWarning(ex)) {
                warnings.Add($"Failed to capture {phase} control screenshot for {FormatWindowLabel(target.Window)}: {ex.Message}");
            }
        }

        return screenshots;
    }

    private static string DetermineControlClickSafetyMode(IReadOnlyList<WindowControlTargetInfo> controls) {
        return HasZeroHandleUiAutomationControl(controls) ? "uia-direct-invoke" : "background-control-click";
    }

    private static string DetermineControlCheckStateSafetyMode(IReadOnlyList<WindowControlTargetInfo> controls) {
        return HasZeroHandleUiAutomationControl(controls) ? "uia-toggle" : "background-control-check-state";
    }

    private static string DetermineControlSelectedValueSafetyMode(IReadOnlyList<WindowControlTargetInfo> controls) {
        return HasZeroHandleUiAutomationControl(controls) ? "uia-selection-item" : "background-control-selected-value";
    }

    private static string DetermineControlTextSafetyMode(IReadOnlyList<WindowControlTargetInfo> controls, bool allowForegroundInputFallback) {
        if (controls.Any(control => control.Control.Source == WindowControlSource.UiAutomation && control.Control.Handle == IntPtr.Zero && !control.Control.SupportsBackgroundText)) {
            return allowForegroundInputFallback ? "foreground-input-fallback" : "uia-direct-value";
        }

        return HasZeroHandleUiAutomationControl(controls) ? "uia-direct-value" : "background-control-text";
    }

    private static string DetermineControlKeySafetyMode(IReadOnlyList<WindowControlTargetInfo> controls, bool allowForegroundInputFallback) {
        if (HasZeroHandleUiAutomationControl(controls)) {
            return allowForegroundInputFallback ? "foreground-input-fallback" : "background-control-keys";
        }

        return "background-control-keys";
    }

    private static bool HasZeroHandleUiAutomationControl(IReadOnlyList<WindowControlTargetInfo> controls) {
        return controls.Any(control => control.Control.Source == WindowControlSource.UiAutomation && control.Control.Handle == IntPtr.Zero);
    }

    private static string BuildArtifactPrefix(string action, string phase, IntPtr handle, string stamp, int index) {
        return $"{action}-{phase}-{stamp}-{handle.ToInt64():X}-{index + 1}";
    }

    private static string? ResolveArtifactOutputPath(string? artifactDirectory, string prefix) {
        if (string.IsNullOrWhiteSpace(artifactDirectory)) {
            return null;
        }

        return Path.Combine(artifactDirectory, prefix + ".png");
    }

    private static string FormatWindowLabel(WindowInfo window) {
        string title = string.IsNullOrWhiteSpace(window.Title) ? "<untitled>" : window.Title;
        return $"{title} ({window.Handle.ToInt64():X})";
    }

    private static WindowMutationVerificationResult? VerifyWindowMutation(DesktopAutomationService automation, string action, IReadOnlyList<WindowInfo> windows, MutationArtifactOptions options, Func<DesktopAutomationService, IReadOnlyList<WindowInfo>, MutationArtifactOptions, WindowMutationVerificationResult?>? verify, List<string> warnings) {
        if (!options.VerifyAfter) {
            return null;
        }

        try {
            Thread.Sleep(75);
            if (verify != null) {
                return verify(automation, windows, options);
            }

            return BuildWindowPresenceVerificationResult(action, windows, ObserveWindowsByHandle(automation, windows), tolerancePixels: options.VerificationTolerancePixels);
        } catch (Exception ex) when (IsArtifactWarning(ex) || ex is Win32Exception || ex is TimeoutException) {
            warnings.Add($"Post-mutation verification failed for '{action}': {ex.Message}");
            return new WindowMutationVerificationResult {
                Verified = false,
                Mode = "verification-error",
                Summary = $"The '{action}' mutation completed, but DesktopManager could not verify the final state.",
                ExpectedCount = windows.Count,
                ObservedCount = 0,
                MatchedCount = 0,
                MismatchCount = windows.Count,
                TolerancePixels = options.VerificationTolerancePixels,
                Notes = new[] { ex.Message }
            };
        }
    }

    private static IReadOnlyList<WindowInfo> ObserveWindowsByHandle(DesktopAutomationService automation, IReadOnlyList<WindowInfo> windows) {
        if (windows.Count == 0) {
            return Array.Empty<WindowInfo>();
        }

        var observedWindows = new List<WindowInfo>(windows.Count);
        foreach (WindowInfo window in windows) {
            if (window.Handle == IntPtr.Zero) {
                continue;
            }

            IReadOnlyList<WindowInfo> matches = automation.GetWindows(new WindowQueryOptions {
                Handle = window.Handle,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            });
            if (matches.Count > 0) {
                observedWindows.Add(matches[0]);
            }
        }

        return observedWindows;
    }

    private static bool IsArtifactWarning(Exception exception) {
        return exception is ArgumentException ||
            exception is ArgumentOutOfRangeException ||
            exception is DirectoryNotFoundException ||
            exception is FileNotFoundException ||
            exception is InvalidOperationException;
    }

    private sealed class VisualObservationBaseline : IDisposable {
        public VisualObservationBaseline(WindowInfo window, DesktopCapture capture) {
            Window = window;
            Capture = capture;
        }

        public WindowInfo Window { get; }

        public DesktopCapture Capture { get; }

        public void Dispose() {
            Capture.Dispose();
        }
    }
}
