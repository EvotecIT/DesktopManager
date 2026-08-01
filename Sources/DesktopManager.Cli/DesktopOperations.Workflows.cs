using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DesktopManager.Cli;

internal static partial class DesktopOperations {
    private const int WorkflowResolutionTimeoutMilliseconds = 2000;
    private const int WorkflowResolutionIntervalMilliseconds = 100;

    private static readonly string[] CodingProcessPatterns = {
        "code",
        "devenv",
        "rider*",
        "webstorm*",
        "pycharm*",
        "idea*",
        "powershell",
        "pwsh",
        "wt",
        "windowsterminal",
        "cmd"
    };

    private static readonly string[] SharingProcessPatterns = {
        "code",
        "devenv",
        "rider*",
        "webstorm*",
        "pycharm*",
        "idea*",
        "chrome",
        "msedge",
        "firefox",
        "brave",
        "powerpnt"
    };

    private static readonly string[] DistractingProcessPatterns = {
        "teams*",
        "ms-teams*",
        "slack",
        "discord",
        "olk",
        "outlook",
        "thunderbird",
        "whatsapp",
        "telegram",
        "signal",
        "skype"
    };

    private static readonly string[] DistractingTitlePatterns = {
        "*Inbox*",
        "*Mail*",
        "*Messenger*",
        "*Chat*"
    };

    public static ControlValueAssertionResult AssertControlValue(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, string expected, bool contains, bool allWindows) {
        return ExecuteControlValueAssertion(windowCriteria, allWindows, null, controlCriteria.All, expected, contains, automation => automation.GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, controlCriteria.All));
    }

    public static ControlValueAssertionResult AssertControlTargetValue(WindowSelectionCriteria windowCriteria, string targetName, string expected, bool contains, bool allWindows, bool allControls) {
        return ExecuteControlValueAssertion(windowCriteria, allWindows, targetName, allControls, expected, contains, automation => automation.GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls));
    }

    public static WorkflowResult PrepareForCoding(string? layoutName, WindowSelectionCriteria focusCriteria, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWorkflow("prepare-for-coding", layoutName, artifactOptions, notes => {
            bool layoutApplied = TryApplyNamedLayout(layoutName, notes);
            WindowResult? resolvedWindow;
            WindowResult? focusedWindow;
            if (HasExplicitSelector(focusCriteria)) {
                WorkflowFocusResult? focusAttempt = TryFocusWorkflowWindow(focusCriteria, notes, "Focused requested coding window.");
                resolvedWindow = focusAttempt?.ResolvedWindow;
                focusedWindow = focusAttempt?.FocusedWindow;
            } else {
                focusedWindow = TryFocusPreferredProcesses(CodingProcessPatterns, notes, "Focused coding window.");
                resolvedWindow = focusedWindow;
            }

            if (!layoutApplied && !string.IsNullOrWhiteSpace(layoutName)) {
                notes.Add($"Named layout '{layoutName}' was not found.");
            }

            if (focusedWindow == null) {
                notes.Add("No editor or terminal window could be focused for coding.");
            }

            return new WorkflowOutcome {
                Success = true,
                LayoutApplied = layoutApplied,
                ResolvedWindow = resolvedWindow,
                FocusedWindow = focusedWindow
            };
        });
    }

    public static WorkflowResult PrepareForScreenSharing(string? layoutName, WindowSelectionCriteria focusCriteria, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWorkflow("prepare-for-screen-sharing", layoutName, artifactOptions, notes => {
            bool layoutApplied = TryApplyNamedLayout(layoutName, notes);
            IReadOnlyList<WindowResult> minimizedWindows = MinimizeDistractingWindows(focusCriteria, notes);
            WindowResult? resolvedWindow;
            WindowResult? focusedWindow;
            if (HasExplicitSelector(focusCriteria)) {
                WorkflowFocusResult? focusAttempt = TryFocusWorkflowWindow(focusCriteria, notes, "Focused requested sharing window.");
                resolvedWindow = focusAttempt?.ResolvedWindow;
                focusedWindow = focusAttempt?.FocusedWindow;
            } else {
                focusedWindow = TryFocusPreferredProcesses(SharingProcessPatterns, notes, "Focused sharing window.");
                resolvedWindow = focusedWindow;
            }

            if (focusedWindow == null) {
                focusedWindow = TryFocusFirstVisibleNonDistractingWindow(notes);
                if (resolvedWindow == null) {
                    resolvedWindow = focusedWindow;
                }
            }

            if (!layoutApplied && !string.IsNullOrWhiteSpace(layoutName)) {
                notes.Add($"Named layout '{layoutName}' was not found.");
            }

            if (focusedWindow == null) {
                notes.Add("No sharing window could be focused automatically.");
            }

            return new WorkflowOutcome {
                Success = true,
                LayoutApplied = layoutApplied,
                ResolvedWindow = resolvedWindow,
                FocusedWindow = focusedWindow,
                MinimizedWindows = minimizedWindows
            };
        });
    }

    public static WorkflowResult CleanUpDistractions(MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWorkflow("clean-up-distractions", null, artifactOptions, notes => {
            IReadOnlyList<WindowResult> minimizedWindows = MinimizeDistractingWindows(new WindowSelectionCriteria(), notes);
            if (minimizedWindows.Count == 0) {
                notes.Add("No distracting windows were identified.");
            }

            return new WorkflowOutcome {
                Success = true,
                MinimizedWindows = minimizedWindows
            };
        });
    }

    private static ControlValueAssertionResult ExecuteControlValueAssertion(WindowSelectionCriteria windowCriteria, bool allWindows, string? targetName, bool allControls, string expected, bool contains, Func<DesktopAutomationService, IReadOnlyList<WindowControlTargetInfo>> resolveControls) {
        if (string.IsNullOrWhiteSpace(expected)) {
            throw new CommandLineException("An expected value is required.");
        }

        return ExecuteCore(() => {
            var automation = new DesktopAutomationService();
            IReadOnlyList<WindowControlTargetInfo> controls = resolveControls(automation);
            StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            int matchedCount = controls.Count(control => MatchesControlValue(control.Control, expected, contains, comparison));
            return new ControlValueAssertionResult {
                Assertion = "control-value",
                Expected = expected,
                MatchMode = contains ? "contains-ignore-case" : "equals-ignore-case",
                PropertyName = "value-or-text",
                Count = controls.Count,
                MatchedCount = matchedCount,
                Matched = controls.Count > 0 && matchedCount == controls.Count,
                TargetName = targetName,
                Controls = controls.Select(MapControl).ToArray()
            };
        });
    }

    private static bool MatchesControlValue(WindowControlInfo control, string expected, bool contains, StringComparison comparison) {
        string value = string.IsNullOrEmpty(control.Value) ? control.Text : control.Value;
        if (contains) {
            return value.IndexOf(expected, comparison) >= 0;
        }

        return string.Equals(value, expected, comparison);
    }

    private static WorkflowResult ExecuteWorkflow(string action, string? layoutName, MutationArtifactOptions? artifactOptions, Func<List<string>, WorkflowOutcome> body) {
        return ExecuteCore(() => {
            Stopwatch stopwatch = Stopwatch.StartNew();
            MutationArtifactOptions options = artifactOptions ?? new MutationArtifactOptions();
            var warnings = new List<string>();
            var notes = new List<string>();

            IReadOnlyList<ScreenshotResult> beforeScreenshots = options.CaptureBefore
                ? CaptureDesktopArtifacts(action, "before", options, warnings)
                : Array.Empty<ScreenshotResult>();

            WorkflowOutcome outcome = body(notes);

            IReadOnlyList<ScreenshotResult> afterScreenshots = options.CaptureAfter
                ? CaptureDesktopArtifacts(action, "after", options, warnings)
                : Array.Empty<ScreenshotResult>();

            stopwatch.Stop();
            return BuildWorkflowResult(
                action,
                outcome.Success,
                (int)stopwatch.ElapsedMilliseconds,
                layoutName,
                outcome.LayoutApplied,
                outcome.MinimizedWindows,
                outcome.ResolvedWindow,
                outcome.FocusedWindow,
                notes,
                beforeScreenshots,
                afterScreenshots,
                warnings);
        });
    }

    internal static WorkflowResult BuildWorkflowResult(string action, bool success, int elapsedMilliseconds, string? layoutName, bool layoutApplied, IReadOnlyList<WindowResult> minimizedWindows, WindowResult? resolvedWindow, WindowResult? focusedWindow, IReadOnlyList<string> notes, IReadOnlyList<ScreenshotResult> beforeScreenshots, IReadOnlyList<ScreenshotResult> afterScreenshots, IReadOnlyList<string> artifactWarnings) {
        return new WorkflowResult {
            Action = action,
            Success = success,
            ElapsedMilliseconds = elapsedMilliseconds,
            LayoutName = layoutName,
            LayoutApplied = layoutApplied,
            MinimizedCount = minimizedWindows.Count,
            MinimizedWindows = minimizedWindows,
            ResolvedWindow = resolvedWindow,
            FocusedWindow = focusedWindow,
            Notes = notes,
            BeforeScreenshots = beforeScreenshots,
            AfterScreenshots = afterScreenshots,
            ArtifactWarnings = artifactWarnings
        };
    }

    private static IReadOnlyList<ScreenshotResult> CaptureDesktopArtifacts(string action, string phase, MutationArtifactOptions options, List<string> warnings) {
        try {
            using DesktopCapture capture = new DesktopAutomationService().CaptureDesktop();
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
            string prefix = $"{action}-{phase}-{stamp}";
            return new[] { SaveScreenshot(capture, prefix, ResolveArtifactOutputPath(options.ArtifactDirectory, prefix)) };
        } catch (Exception ex) when (IsArtifactWarning(ex)) {
            warnings.Add($"Failed to capture {phase} workflow screenshot: {ex.Message}");
            return Array.Empty<ScreenshotResult>();
        }
    }

    private static bool TryApplyNamedLayout(string? layoutName, List<string> notes) {
        if (string.IsNullOrWhiteSpace(layoutName)) {
            return false;
        }

        if (!DesktopStateStore.ListNames("layouts").Contains(layoutName, StringComparer.OrdinalIgnoreCase)) {
            return false;
        }

        ApplyLayout(layoutName, validate: false);
        notes.Add($"Applied named layout '{layoutName}'.");
        return true;
    }

    private static IReadOnlyList<WindowResult> MinimizeDistractingWindows(WindowSelectionCriteria focusCriteria, List<string> notes) {
        var windows = new List<WindowInfo>();
        DesktopAutomationService automation = new DesktopAutomationService();
        foreach (string processPattern in DistractingProcessPatterns) {
            windows.AddRange(ResolveWindowsForMutation(automation, new WindowSelectionCriteria {
                ProcessNamePattern = processPattern,
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = true,
                All = true
            }));
        }

        foreach (string titlePattern in DistractingTitlePatterns) {
            windows.AddRange(ResolveWindowsForMutation(automation, new WindowSelectionCriteria {
                TitlePattern = titlePattern,
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = true,
                All = true
            }));
        }

        IntPtr protectedHandle = ResolveProtectedFocusHandle(focusCriteria);
        WindowInfo[] distinctWindows = windows
            .Where(window => window.Handle != IntPtr.Zero && window.Handle != protectedHandle)
            .GroupBy(window => window.Handle)
            .Select(group => group.First())
            .ToArray();

        var minimizedWindows = new List<WindowResult>(distinctWindows.Length);
        foreach (WindowInfo window in distinctWindows) {
            try {
                IReadOnlyList<WindowInfo> minimized = automation.MinimizeWindows(new WindowQueryOptions {
                    Handle = window.Handle,
                    IncludeHidden = true,
                    IncludeCloaked = true,
                    IncludeOwned = true,
                    IncludeEmptyTitles = true
                });
                minimizedWindows.AddRange(minimized.Select(MapWindow));
            } catch (InvalidOperationException) {
                // Ignore windows that disappeared between discovery and minimize.
            }
        }

        if (minimizedWindows.Count > 0) {
            notes.Add($"Minimized {minimizedWindows.Count} distracting window(s).");
        }

        return minimizedWindows;
    }

    private static IntPtr ResolveProtectedFocusHandle(WindowSelectionCriteria focusCriteria) {
        if (!HasExplicitSelector(focusCriteria)) {
            return IntPtr.Zero;
        }

        try {
            DesktopAutomationService automation = new DesktopAutomationService();
            IReadOnlyList<WindowInfo> windows = ResolveWindowsForMutation(automation, focusCriteria);
            return windows.FirstOrDefault()?.Handle ?? IntPtr.Zero;
        } catch {
            return IntPtr.Zero;
        }
    }

    private static WorkflowFocusResult? TryFocusWorkflowWindow(WindowSelectionCriteria criteria, List<string> notes, string successNote) {
        WindowResult? resolvedWindow = null;
        try {
            DesktopAutomationService automation = new DesktopAutomationService();
            WindowQueryOptions query = CreateWindowQuery(criteria);
            IReadOnlyList<WindowInfo> windows = WaitForWorkflowWindows(automation, query);
            if (windows.Count == 0) {
                return null;
            }

            resolvedWindow = MapWindow(windows[0]);
            IReadOnlyList<WindowInfo> focused = automation.FocusWindows(query, all: false);
            if (focused.Count == 0) {
                notes.Add("Resolved the requested workflow window, but Windows did not allow it to take foreground focus.");
                return new WorkflowFocusResult {
                    ResolvedWindow = resolvedWindow
                };
            }

            WindowResult result = MapWindow(focused[0]);
            notes.Add(successNote);
            return new WorkflowFocusResult {
                ResolvedWindow = resolvedWindow,
                FocusedWindow = result
            };
        } catch (InvalidOperationException) {
            if (resolvedWindow != null) {
                notes.Add("Resolved the requested workflow window, but Windows rejected the foreground focus attempt.");
                return new WorkflowFocusResult {
                    ResolvedWindow = resolvedWindow
                };
            }

            return null;
        }
    }

    private static IReadOnlyList<WindowInfo> WaitForWorkflowWindows(DesktopAutomationService automation, WindowQueryOptions query) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        IReadOnlyList<WindowInfo> windows = Array.Empty<WindowInfo>();
        do {
            windows = automation.GetWindows(query);
            if (windows.Count > 0) {
                return windows;
            }

            System.Threading.Thread.Sleep(WorkflowResolutionIntervalMilliseconds);
        } while (stopwatch.ElapsedMilliseconds < WorkflowResolutionTimeoutMilliseconds);

        return windows;
    }

    private static WindowResult? TryFocusFirstVisibleNonDistractingWindow(List<string> notes) {
        try {
            DesktopAutomationService automation = new DesktopAutomationService();
            IReadOnlyList<WindowInfo> windows = automation.GetWindows(new WindowQueryOptions {
                TitlePattern = "*",
                ProcessNamePattern = "*",
                ClassNamePattern = "*",
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = false
            });

            WindowInfo? candidate = windows.FirstOrDefault(window => !IsDistractingWindow(window));
            if (candidate == null) {
                return null;
            }

            IReadOnlyList<WindowInfo> focused = automation.FocusWindows(new WindowQueryOptions {
                Handle = candidate.Handle,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            }, all: false);
            if (focused.Count == 0) {
                return null;
            }

            notes.Add("Focused the first non-distracting visible window.");
            return MapWindow(focused[0]);
        } catch (InvalidOperationException) {
            return null;
        }
    }

    private static bool IsDistractingWindow(WindowInfo window) {
        string title = window.Title ?? string.Empty;
        return DistractingTitlePatterns.Any(pattern => MatchesPattern(title, pattern)) ||
            TryGetProcessName(window.ProcessId, out string? processName) && DistractingProcessPatterns.Any(pattern => MatchesPattern(processName!, pattern));
    }

    private static bool TryGetProcessName(uint processId, out string? processName) {
        processName = null;
        try {
            using Process process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
            return !string.IsNullOrWhiteSpace(processName);
        } catch {
            return false;
        }
    }

    private static WindowResult? TryFocusPreferredProcesses(IEnumerable<string> processPatterns, List<string> notes, string successNote) {
        foreach (string processPattern in processPatterns) {
            WorkflowFocusResult? focusAttempt = TryFocusWorkflowWindow(new WindowSelectionCriteria {
                ProcessNamePattern = processPattern,
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = true,
                IncludeEmptyTitles = false
            }, notes, successNote);
            if (focusAttempt?.FocusedWindow != null) {
                return focusAttempt.FocusedWindow;
            }
        }

        return null;
    }

    private static bool MatchesPattern(string text, string pattern) {
        if (string.IsNullOrWhiteSpace(pattern) || pattern == "*") {
            return true;
        }

        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(text ?? string.Empty, regexPattern, RegexOptions.IgnoreCase);
    }

    private sealed class WorkflowOutcome {
        public bool Success { get; set; }
        public bool LayoutApplied { get; set; }
        public IReadOnlyList<WindowResult> MinimizedWindows { get; set; } = Array.Empty<WindowResult>();
        public WindowResult? ResolvedWindow { get; set; }
        public WindowResult? FocusedWindow { get; set; }
    }

    private sealed class WorkflowFocusResult {
        public WindowResult? ResolvedWindow { get; set; }
        public WindowResult? FocusedWindow { get; set; }
    }
}
