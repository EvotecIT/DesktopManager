using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static partial class DesktopOperations {
    public static LaunchAndWaitResult LaunchAndWaitForWindow(string filePath, string? arguments, string? workingDirectory, int? waitForInputIdleMilliseconds, int? launchWaitForWindowMilliseconds, int? launchWaitForWindowIntervalMilliseconds, string? launchWindowTitlePattern, string? launchWindowClassNamePattern, string? windowTitlePattern, string? windowClassNamePattern, bool includeHidden, bool includeEmpty, bool all, bool followProcessFamily, int timeoutMilliseconds, int intervalMilliseconds, MutationArtifactOptions? artifactOptions = null) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            throw new CommandLineException("Process path is required.");
        }

        if (timeoutMilliseconds <= 0) {
            throw new CommandLineException("Wait timeout must be greater than 0.");
        }

        if (intervalMilliseconds <= 0) {
            throw new CommandLineException("Wait interval must be greater than 0.");
        }

        return ExecuteCore(() => {
            var warnings = new List<string>();
            var notes = new List<string>();
            MutationArtifactOptions options = artifactOptions ?? new MutationArtifactOptions();

            IReadOnlyList<ScreenshotResult> beforeScreenshots = options.CaptureBefore
                ? CaptureDesktopArtifacts("launch-and-wait-for-window", "before", options, warnings)
                : Array.Empty<ScreenshotResult>();

            DesktopProcessLaunchAndWaitResult workflow = new DesktopAutomationService().LaunchAndWaitForWindow(new DesktopProcessLaunchAndWaitOptions {
                FilePath = filePath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                WaitForInputIdleMilliseconds = waitForInputIdleMilliseconds,
                LaunchWaitForWindowMilliseconds = launchWaitForWindowMilliseconds,
                LaunchWaitForWindowIntervalMilliseconds = launchWaitForWindowIntervalMilliseconds,
                LaunchWindowTitlePattern = launchWindowTitlePattern,
                LaunchWindowClassNamePattern = launchWindowClassNamePattern,
                WindowTitlePattern = windowTitlePattern,
                WindowClassNamePattern = windowClassNamePattern,
                IncludeHidden = includeHidden,
                IncludeEmptyTitles = includeEmpty,
                All = all,
                FollowProcessFamily = followProcessFamily,
                TimeoutMilliseconds = timeoutMilliseconds,
                IntervalMilliseconds = intervalMilliseconds
            });
            ProcessLaunchResult launch = BuildProcessLaunchResult(workflow.Launch);
            LaunchWaitBindingPlan waitPlan = MapLaunchWaitBindingPlan(workflow.WaitPlan, all);
            WaitForWindowResult waitResult = BuildWaitForWindowResult(workflow.WindowWait);

            notes.Add($"Launched process {launch.ProcessId} from '{launch.FilePath}'.");
            if (launch.ResolvedProcessId.HasValue && launch.ResolvedProcessId.Value != launch.ProcessId) {
                notes.Add($"Launch-time window correlation resolved process {launch.ResolvedProcessId.Value}.");
            }

            if (waitPlan.BoundProcessId.HasValue) {
                notes.Add($"Wait selector bound to processId={waitPlan.BoundProcessId.Value}, title='{waitPlan.Criteria.TitlePattern}', class='{waitPlan.Criteria.ClassNamePattern}'.");
            } else {
                notes.Add($"Wait selector bound to processName='{waitPlan.BoundProcessName}', title='{waitPlan.Criteria.TitlePattern}', class='{waitPlan.Criteria.ClassNamePattern}'.");
            }

            notes.Add(waitPlan.BoundProcessId.HasValue
                ? $"Resolved {waitResult.Count} window(s) for process {waitPlan.BoundProcessId.Value}."
                : $"Resolved {waitResult.Count} window(s) for process family '{waitPlan.BoundProcessName}'.");

            IReadOnlyList<ScreenshotResult> afterScreenshots;
            if (options.CaptureAfter) {
                var automation = new DesktopAutomationService();
                afterScreenshots = CaptureWindowArtifacts(
                    automation,
                    workflow.WindowWait.Windows,
                    "launch-and-wait-for-window",
                    "after",
                    options,
                    warnings);
            } else {
                afterScreenshots = Array.Empty<ScreenshotResult>();
            }

            return new LaunchAndWaitResult {
                Action = "launch-and-wait-for-window",
                Success = workflow.Success,
                ElapsedMilliseconds = workflow.ElapsedMilliseconds,
                WaitTimeoutMilliseconds = timeoutMilliseconds,
                WaitIntervalMilliseconds = intervalMilliseconds,
                WaitBinding = waitPlan.WaitBinding,
                BoundProcessId = waitPlan.BoundProcessId,
                BoundProcessName = waitPlan.BoundProcessName,
                Launch = launch,
                WindowWait = waitResult,
                Notes = notes,
                BeforeScreenshots = beforeScreenshots,
                AfterScreenshots = afterScreenshots,
                ArtifactWarnings = warnings
            };
        });
    }

    internal static LaunchWaitBindingPlan CreateLaunchWaitBindingPlan(ProcessLaunchResult launch, string? launchWindowTitlePattern, string? launchWindowClassNamePattern, string? windowTitlePattern, string? windowClassNamePattern, bool includeHidden, bool includeEmpty, bool all, bool followProcessFamily) {
        DesktopLaunchWaitBindingPlan corePlan = DesktopAutomationService.CreateLaunchWaitBindingPlan(new DesktopProcessLaunchInfo {
            FilePath = launch.FilePath,
            Arguments = launch.Arguments,
            WorkingDirectory = launch.WorkingDirectory,
            ProcessId = launch.ProcessId,
            ResolvedProcessId = launch.ResolvedProcessId
        }, launchWindowTitlePattern, launchWindowClassNamePattern, windowTitlePattern, windowClassNamePattern, includeHidden, includeEmpty, all, followProcessFamily);
        return MapLaunchWaitBindingPlan(corePlan, all);
    }

    private static LaunchWaitBindingPlan MapLaunchWaitBindingPlan(DesktopLaunchWaitBindingPlan corePlan, bool all) {
        return new LaunchWaitBindingPlan {
            Criteria = new WindowSelectionCriteria {
                TitlePattern = corePlan.Criteria.TitlePattern,
                ProcessNamePattern = corePlan.Criteria.ProcessNamePattern,
                ClassNamePattern = corePlan.Criteria.ClassNamePattern,
                ProcessId = corePlan.Criteria.ProcessId == 0 ? null : corePlan.Criteria.ProcessId,
                Active = corePlan.Criteria.ActiveWindow,
                IncludeHidden = corePlan.Criteria.IncludeHidden,
                IncludeCloaked = corePlan.Criteria.IncludeCloaked,
                IncludeOwned = corePlan.Criteria.IncludeOwned,
                IncludeEmptyTitles = corePlan.Criteria.IncludeEmptyTitles ?? false,
                All = all
            },
            WaitBinding = corePlan.WaitBinding,
            BoundProcessId = corePlan.BoundProcessId,
            BoundProcessName = corePlan.BoundProcessName
        };
    }

    private static WaitForWindowResult BuildWaitForWindowResult(DesktopWindowWaitResult result) {
        return new WaitForWindowResult {
            ElapsedMilliseconds = result.ElapsedMilliseconds,
            Count = result.Windows.Count,
            Windows = result.Windows.Select(MapWindow).ToArray()
        };
    }
}
