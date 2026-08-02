using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DesktopManager.Cli;

internal static class ControlCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "observe" => Observe(arguments),
            "wait-observation" => WaitObservation(arguments),
            "diagnose" => Diagnose(arguments),
            "exists" => Exists(arguments),
            "assert-value" => AssertValue(arguments),
            "wait" => Wait(arguments),
            "click" => Click(arguments),
            "set-text" => SetText(arguments),
            "edit-text" => EditText(arguments),
            "send-keys" => SendKeys(arguments),
            _ => throw new CommandLineException($"Unknown control command '{action}'.")
        };
    }

    private static int Observe(CommandLineArguments arguments) {
        IReadOnlyList<DesktopControlObservation> observations = DesktopOperations.ObserveControls(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            arguments.GetIntOption("max-text-length") ?? 4096,
            arguments.GetOption("expected-text"),
            arguments.GetBoolFlag("ignore-case"),
            arguments.GetBoolFlag("include-text-ranges"),
            arguments.GetBoolFlag("realize-virtualized-item"),
            arguments.GetBoolFlag("all-windows"));
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(observations);
            return 0;
        }

        foreach (DesktopControlObservation observation in observations) {
            Console.Out.WriteLine($"{observation.Identity.ControlHandleHex} {observation.Identity.ControlType} status={observation.Status} source={observation.Source} patterns={string.Join(',', observation.Capabilities.Patterns)}");
            Console.Out.WriteLine($"text-source={observation.Text.Source} complete={observation.Text.IsComplete} fingerprint={observation.Text.ContentFingerprint} text={observation.Text.EscapedValue}");
        }

        return 0;
    }

    private static int WaitObservation(CommandLineArguments arguments) {
        DesktopControlObservationCondition condition = CreateObservationCondition(arguments);
        DesktopControlObservation observation = DesktopOperations.WaitForControlObservation(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            condition,
            arguments.GetIntOption("max-text-length") ?? 4096,
            !arguments.HasFlag("include-text-ranges") || arguments.GetBoolFlag("include-text-ranges"),
            arguments.GetBoolFlag("realize-virtualized-item"),
            arguments.GetIntOption("timeout-ms") ?? 10000,
            arguments.GetIntOption("interval-ms") ?? 200);
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(observation);
        } else {
            Console.Out.WriteLine($"matched {observation.Identity.SessionKey} strategy={observation.WaitStrategy} status={observation.Status}");
        }

        return 0;
    }

    internal static DesktopControlObservationCondition CreateObservationCondition(CommandLineArguments arguments) {
        return new DesktopControlObservationCondition {
            ExpectedText = arguments.GetOption("expected-text"),
            IgnoreCase = arguments.GetBoolFlag("ignore-case"),
            IsTextComplete = arguments.GetBoolFlag("complete-text") ? true : null,
            IsTextTruncated = arguments.GetBoolFlag("truncated-text") ? true : null,
            IsEnabled = arguments.GetBoolFlag("enabled") ? true : arguments.GetBoolFlag("disabled") ? false : null,
            IsFocused = arguments.GetBoolFlag("focused") ? true : arguments.GetBoolFlag("not-focused") ? false : null,
            IsChecked = arguments.GetBoolFlag("checked") ? true : arguments.GetBoolFlag("unchecked") ? false : null,
            IsSelected = arguments.GetBoolFlag("selected") ? true : arguments.GetBoolFlag("not-selected") ? false : null,
            ExpandCollapseState = arguments.GetOption("expand-collapse-state"),
            MinimumRangeValue = arguments.GetDoubleOption("minimum-range-value"),
            MaximumRangeValue = arguments.GetDoubleOption("maximum-range-value")
        };
    }

    private static int List(CommandLineArguments arguments) {
        string? targetName = arguments.GetOption("target");
        IReadOnlyList<ControlResult> controls = !string.IsNullOrWhiteSpace(targetName)
            ? DesktopOperations.ListControlTargets(
                CreateWindowCriteria(arguments),
                targetName,
                arguments.GetBoolFlag("all-windows"),
                arguments.GetBoolFlag("all"))
            : DesktopOperations.ListControls(
                CreateWindowCriteria(arguments),
                CreateControlCriteria(arguments),
                arguments.GetBoolFlag("all-windows"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(controls);
            return 0;
        }

        if (controls.Count == 0) {
            return WriteNoMatchingControls(Console.Out);
        }

        return WriteControlListResults(controls, Console.Out);
    }

    internal static int WriteControlListResults(IReadOnlyList<ControlResult> controls, TextWriter writer) {
        IReadOnlyList<IReadOnlyList<string>> rows = controls
            .Select(control => (IReadOnlyList<string>)new[] {
                control.ParentWindow.ProcessId.ToString(),
                control.Id.ToString(),
                control.Handle.Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase),
                control.Source,
                control.ControlType,
                control.Left.ToString(),
                control.Top.ToString(),
                control.Width.ToString(),
                control.Height.ToString(),
                control.IsEnabled?.ToString() ?? string.Empty,
                control.IsKeyboardFocusable?.ToString() ?? string.Empty,
                control.IsOffscreen?.ToString() ?? string.Empty,
                control.SupportsBackgroundClick.ToString(),
                control.SupportsBackgroundText.ToString(),
                control.SupportsBackgroundKeys.ToString(),
                control.SupportsForegroundInputFallback.ToString(),
                control.AutomationId,
                control.ClassName,
                control.Text,
                control.Value,
                control.ParentWindow.Title
            })
            .ToArray();

        OutputFormatter.WriteTable(writer, new[] { "PID", "Id", "Handle", "Source", "Type", "X", "Y", "Width", "Height", "Enabled", "Focusable", "Offscreen", "BgClick", "BgText", "BgKeys", "FgFallback", "AutomationId", "Class", "Text", "Value", "Window" }, rows);
        return 0;
    }

    internal static int WriteNoMatchingControls(TextWriter writer) {
        writer.WriteLine("No matching controls found.");
        return 0;
    }

    internal static int WriteNoMatchingDiagnosticWindows(TextWriter writer) {
        writer.WriteLine("No matching windows found for control diagnostics.");
        return 0;
    }

    private static int Exists(CommandLineArguments arguments) {
        string? targetName = arguments.GetOption("target");
        ControlAssertionResult result = !string.IsNullOrWhiteSpace(targetName)
            ? DesktopOperations.ControlTargetExists(
                CreateWindowCriteria(arguments),
                targetName,
                arguments.GetBoolFlag("all-windows"),
                arguments.GetBoolFlag("all"))
            : DesktopOperations.ControlExists(
                CreateWindowCriteria(arguments),
                CreateControlCriteria(arguments),
                arguments.GetBoolFlag("all-windows"));
        return WriteAssertion(arguments, result, "Matching control found.", "No matching controls found.");
    }

    private static int Diagnose(CommandLineArguments arguments) {
        string? targetName = arguments.GetOption("target");
        IReadOnlyList<ControlDiagnosticResult> diagnostics = !string.IsNullOrWhiteSpace(targetName)
            ? DesktopOperations.DiagnoseControlTargets(
                CreateWindowCriteria(arguments),
                targetName,
                arguments.GetBoolFlag("all-windows"),
                arguments.GetIntOption("sample-limit") ?? 10,
                arguments.GetBoolFlag("action-probe"))
            : DesktopOperations.DiagnoseControls(
                CreateWindowCriteria(arguments),
                CreateControlCriteria(arguments),
                arguments.GetBoolFlag("all-windows"),
                arguments.GetIntOption("sample-limit") ?? 10,
                arguments.GetBoolFlag("action-probe"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(diagnostics);
            return 0;
        }

        if (diagnostics.Count == 0) {
            return WriteNoMatchingDiagnosticWindows(Console.Out);
        }

        WriteDiagnosticResults(diagnostics, Console.Out);
        return 0;
    }

    private static int Wait(CommandLineArguments arguments) {
        string? targetName = arguments.GetOption("target");
        WaitForControlResult result = !string.IsNullOrWhiteSpace(targetName)
            ? DesktopOperations.WaitForControlTarget(
                CreateWindowCriteria(arguments),
                targetName,
                arguments.GetIntOption("timeout-ms") ?? 10000,
                arguments.GetIntOption("interval-ms") ?? 200,
                arguments.GetBoolFlag("all-windows"),
                arguments.GetBoolFlag("all"))
            : DesktopOperations.WaitForControl(
                CreateWindowCriteria(arguments),
                CreateControlCriteria(arguments),
                arguments.GetIntOption("timeout-ms") ?? 10000,
                arguments.GetIntOption("interval-ms") ?? 200,
                arguments.GetBoolFlag("all-windows"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteWaitResult(result, Console.Out);
    }

    private static int AssertValue(CommandLineArguments arguments) {
        bool contains = arguments.GetBoolFlag("contains");
        string? expected = arguments.GetOption("expected-value") ?? arguments.GetOption("expected");
        if (string.IsNullOrWhiteSpace(expected)) {
            throw new CommandLineException("Missing required option '--expected-value'.");
        }

        string? targetName = arguments.GetOption("target");
        ControlValueAssertionResult result = !string.IsNullOrWhiteSpace(targetName)
            ? DesktopOperations.AssertControlTargetValue(
                CreateWindowCriteria(arguments),
                targetName,
                expected,
                contains,
                arguments.GetBoolFlag("all-windows"),
                arguments.GetBoolFlag("all"))
            : DesktopOperations.AssertControlValue(
                CreateWindowCriteria(arguments),
                CreateControlCriteria(arguments),
                expected,
                contains,
                arguments.GetBoolFlag("all-windows"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
        } else {
            return WriteValueAssertionResult(result, Console.Out);
        }

        return result.Matched ? 0 : 2;
    }

    private static int Click(CommandLineArguments arguments) {
        string? targetName = arguments.GetOption("target");
        ControlActionResult result;
        if (!string.IsNullOrWhiteSpace(targetName)) {
            result = DesktopOperations.ClickControlTarget(
                CreateWindowCriteria(arguments),
                targetName,
                arguments.GetOption("button") ?? "left",
                arguments.GetBoolFlag("all-windows"),
                arguments.GetBoolFlag("all"),
                CreateArtifactOptions(arguments));
            return WriteAction(arguments, result);
        }

        result = DesktopOperations.ClickControl(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            arguments.GetOption("button") ?? "left",
            arguments.GetBoolFlag("all-windows"),
            CreateArtifactOptions(arguments));
        return WriteAction(arguments, result);
    }

    private static int SetText(CommandLineArguments arguments) {
        string? targetName = arguments.GetOption("target");
        ControlActionResult result = !string.IsNullOrWhiteSpace(targetName)
            ? DesktopOperations.SetControlTargetText(
                CreateWindowCriteria(arguments),
                targetName,
                arguments.GetRequiredOption("text"),
                arguments.GetBoolFlag("ensure-foreground"),
                arguments.GetBoolFlag("allow-foreground-input"),
                arguments.GetBoolFlag("all-windows"),
                arguments.GetBoolFlag("all"),
                CreateArtifactOptions(arguments))
            : DesktopOperations.SetControlText(
                CreateWindowCriteria(arguments),
                CreateControlCriteria(arguments),
                arguments.GetRequiredOption("text"),
                arguments.GetBoolFlag("all-windows"),
                CreateArtifactOptions(arguments));
        return WriteAction(arguments, result);
    }

    private static int EditText(CommandLineArguments arguments) {
        DesktopTextEditResult result = DesktopOperations.EditControlText(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            arguments.GetRequiredOption("text"),
            arguments.GetOption("mode") ?? nameof(DesktopTextEditMode.ReplaceDocument),
            arguments.GetOption("expected-fingerprint"),
            arguments.GetOption("expected-edit-context-fingerprint"),
            arguments.GetBoolFlag("ensure-foreground"),
            arguments.GetBoolFlag("allow-foreground-input"),
            !arguments.GetBoolFlag("no-verify"),
            arguments.GetIntOption("max-text-length") ?? DesktopTextObservationOptions.MaximumTextLength);
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
        } else {
            Console.Out.WriteLine($"edit-text success={result.Success} applied={result.Applied} method={result.Method} failure={result.FailureCode}");
            if (!string.IsNullOrWhiteSpace(result.FailureReason)) {
                Console.Out.WriteLine(result.FailureReason);
            }
        }

        return result.Success ? 0 : 2;
    }

    private static int SendKeys(CommandLineArguments arguments) {
        IReadOnlyList<string> keys = arguments.GetOptions("keys");
        if (keys.Count == 0) {
            string single = arguments.GetRequiredOption("keys");
            keys = new[] { single };
        }

        string? targetName = arguments.GetOption("target");
        ControlActionResult result = !string.IsNullOrWhiteSpace(targetName)
            ? DesktopOperations.SendControlTargetKeys(
                CreateWindowCriteria(arguments),
                targetName,
                keys,
                arguments.GetBoolFlag("ensure-foreground"),
                arguments.GetBoolFlag("allow-foreground-input"),
                arguments.GetBoolFlag("all-windows"),
                arguments.GetBoolFlag("all"),
                CreateArtifactOptions(arguments))
            : DesktopOperations.SendControlKeys(
                CreateWindowCriteria(arguments),
                CreateControlCriteria(arguments),
                keys,
                arguments.GetBoolFlag("all-windows"),
                CreateArtifactOptions(arguments));
        return WriteAction(arguments, result);
    }

    private static int WriteAction(CommandLineArguments arguments, ControlActionResult result) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteActionResult(result, Console.Out);
    }

    internal static int WriteActionResult(ControlActionResult result, TextWriter writer) {
        writer.WriteLine($"{result.Action}: {result.Count} control(s) success={result.Success} safety={result.SafetyMode} elapsed-ms={result.ElapsedMilliseconds}");
        if (!string.IsNullOrWhiteSpace(result.TargetName)) {
            writer.WriteLine($"target: {result.TargetKind ?? "selector"} {result.TargetName}");
        }

        if (result.VisualChange != null) {
            writer.WriteLine($"visual-change: changed after {result.VisualChange.ElapsedMilliseconds}ms");
            writer.WriteLine($"visual-metrics: changed-samples={result.VisualChange.ChangedSampleCount}/{result.VisualChange.SampleCount} ratio={result.VisualChange.ChangedSampleRatio:F4} avg-diff={result.VisualChange.AverageDifference:F1} threshold={result.VisualChange.DifferenceThreshold} min-ratio={result.VisualChange.MinimumChangedRatio:F4} size-changed={result.VisualChange.SizeChanged}");
        }

        if (result.BeforeScreenshots.Count > 0 || result.AfterScreenshots.Count > 0) {
            writer.WriteLine($"artifacts: before={result.BeforeScreenshots.Count} after={result.AfterScreenshots.Count}");
        }

        foreach (string warning in result.ArtifactWarnings) {
            writer.WriteLine($"warning: {warning}");
        }

        foreach (ControlResult control in result.Controls) {
            writer.WriteLine($"- {control.ClassName} [{control.Id}] in {control.ParentWindow.Title}");
        }
        return 0;
    }

    internal static int WriteWaitResult(WaitForControlResult result, TextWriter writer) {
        writer.WriteLine($"wait: {result.Count} control(s) after {result.ElapsedMilliseconds}ms");
        foreach (ControlResult control in result.Controls) {
            writer.WriteLine($"- {control.ControlType} {control.Text} in {control.ParentWindow.Title}");
        }

        return 0;
    }

    internal static int WriteValueAssertionResult(ControlValueAssertionResult result, TextWriter writer) {
        writer.WriteLine(result.Matched ? "Control value assertion passed." : "Control value assertion failed.");
        writer.WriteLine($"assertion: {result.PropertyName} {result.MatchMode} \"{result.Expected}\"");
        writer.WriteLine($"matched: {result.MatchedCount}/{result.Count}");
        foreach (ControlResult control in result.Controls) {
            writer.WriteLine($"- {control.ControlType} value=\"{control.Value}\" text=\"{control.Text}\" in {control.ParentWindow.Title}");
        }

        return result.Matched ? 0 : 2;
    }

    internal static int WriteAssertionResult(ControlAssertionResult result, string successText, string failureText, TextWriter writer) {
        writer.WriteLine(result.Matched ? successText : failureText);
        foreach (ControlResult control in result.Controls) {
            writer.WriteLine($"- {control.ControlType} {control.Text} in {control.ParentWindow.Title}");
        }

        return result.Matched ? 0 : 2;
    }

    internal static void WriteDiagnosticResults(IReadOnlyList<ControlDiagnosticResult> diagnostics, TextWriter writer) {
        foreach (ControlDiagnosticResult diagnostic in diagnostics) {
            writer.WriteLine($"window: {diagnostic.Window.Title} ({diagnostic.Window.Handle})");
            writer.WriteLine($"effective-source: {diagnostic.EffectiveSource}");
            writer.WriteLine($"elapsed-ms: {diagnostic.ElapsedMilliseconds}");
            writer.WriteLine($"uia: required={diagnostic.RequiresUiAutomation} available={diagnostic.UiAutomationAvailable} requested={diagnostic.UseUiAutomation} include={diagnostic.IncludeUiAutomation}");
            writer.WriteLine($"preparation: attempted={diagnostic.PreparationAttempted} succeeded={diagnostic.PreparationSucceeded} ensureForeground={diagnostic.EnsureForegroundWindow}");
            writer.WriteLine($"fallback-roots: count={diagnostic.UiAutomationFallbackRootCount} used={diagnostic.UsedUiAutomationFallbackRoots} cached={diagnostic.UsedCachedUiAutomationControls} preferred={diagnostic.PreferredUiAutomationRootHandle} reused={diagnostic.UsedPreferredUiAutomationRoot}");
            writer.WriteLine($"counts: win32={diagnostic.Win32ControlCount} uia={diagnostic.UiAutomationControlCount} effective={diagnostic.EffectiveControlCount} matched={diagnostic.MatchedControlCount}");
            if (diagnostic.UiAutomationActionProbe != null) {
                writer.WriteLine($"action-probe: attempted={diagnostic.UiAutomationActionProbe.Attempted} resolved={diagnostic.UiAutomationActionProbe.Resolved} cached={diagnostic.UiAutomationActionProbe.UsedCachedActionMatch} preferred={diagnostic.UiAutomationActionProbe.UsedPreferredRoot} root={diagnostic.UiAutomationActionProbe.RootHandle} score={diagnostic.UiAutomationActionProbe.Score} mode={diagnostic.UiAutomationActionProbe.SearchMode} elapsed-ms={diagnostic.UiAutomationActionProbe.ElapsedMilliseconds}");
            }

            foreach (UiAutomationRootDiagnosticResult root in diagnostic.UiAutomationRoots) {
                writer.WriteLine($"root[{root.Order}]: handle={root.Handle} class={root.ClassName} primary={root.IsPrimaryRoot} preferred={root.IsPreferredRoot} cached={root.UsedCachedControls} includeRoot={root.IncludeRoot} elementResolved={root.ElementResolved} count={root.ControlCount} error={root.Error ?? string.Empty}");
                foreach (ControlResult sample in root.SampleControls) {
                    writer.WriteLine($"  * {sample.Source} {sample.ControlType} {sample.AutomationId} {sample.Text}");
                }
            }

            foreach (ControlResult sample in diagnostic.SampleControls) {
                writer.WriteLine($"- {sample.Source} {sample.ControlType} {sample.AutomationId} {sample.Text}");
            }

            writer.WriteLine();
        }
    }

    private static MutationArtifactOptions? CreateArtifactOptions(CommandLineArguments arguments) {
        bool captureBefore = arguments.GetBoolFlag("capture-before");
        bool captureAfter = arguments.GetBoolFlag("capture-after");
        string? artifactDirectory = arguments.GetOption("artifact-directory");
        bool waitForVisualChange = arguments.GetBoolFlag("wait-visual-change");
        string? visualTargetName = arguments.GetOption("visual-target");
        bool visualClientArea = arguments.GetBoolFlag("visual-client-area");
        int visualTimeoutMilliseconds = arguments.GetIntOption("visual-timeout-ms") ?? 5000;
        int visualIntervalMilliseconds = arguments.GetIntOption("visual-interval-ms") ?? 100;
        double visualMinimumChangedRatio = arguments.GetDoubleOption("minimum-changed-ratio") ?? 0.01;
        int visualDifferenceThreshold = arguments.GetIntOption("difference-threshold") ?? 24;
        if (!captureBefore && !captureAfter && string.IsNullOrWhiteSpace(artifactDirectory) && !waitForVisualChange) {
            return null;
        }

        return new MutationArtifactOptions {
            CaptureBefore = captureBefore,
            CaptureAfter = captureAfter,
            ArtifactDirectory = artifactDirectory,
            WaitForVisualChange = waitForVisualChange,
            VisualTargetName = visualTargetName,
            VisualClientArea = visualClientArea,
            VisualTimeoutMilliseconds = visualTimeoutMilliseconds,
            VisualIntervalMilliseconds = visualIntervalMilliseconds,
            VisualMinimumChangedRatio = visualMinimumChangedRatio,
            VisualDifferenceThreshold = visualDifferenceThreshold
        };
    }

    private static int WriteAssertion(CommandLineArguments arguments, ControlAssertionResult result, string successText, string failureText) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
        } else {
            return WriteAssertionResult(result, successText, failureText, Console.Out);
        }

        return result.Matched ? 0 : 2;
    }

    internal static WindowSelectionCriteria CreateWindowCriteria(CommandLineArguments arguments) {
        return new WindowSelectionCriteria {
            TitlePattern = arguments.GetOption("window-title") ?? arguments.GetOption("title") ?? "*",
            ProcessNamePattern = arguments.GetOption("window-process") ?? arguments.GetOption("process") ?? "*",
            ClassNamePattern = arguments.GetOption("window-class") ?? "*",
            ProcessId = arguments.GetIntOption("window-pid") ?? arguments.GetIntOption("pid"),
            Handle = arguments.GetOption("window-handle"),
            Active = arguments.GetBoolFlag("window-active") || arguments.GetBoolFlag("active"),
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }

    internal static ControlSelectionCriteria CreateControlCriteria(CommandLineArguments arguments) {
        return new ControlSelectionCriteria {
            ClassNamePattern = arguments.GetOption("class") ?? "*",
            TextPattern = arguments.GetOption("text-pattern") ?? "*",
            ValuePattern = arguments.GetOption("value-pattern") ?? "*",
            Id = arguments.GetIntOption("id"),
            Handle = arguments.GetOption("handle"),
            AutomationIdPattern = arguments.GetOption("automation-id") ?? "*",
            ControlTypePattern = arguments.GetOption("control-type") ?? "*",
            FrameworkIdPattern = arguments.GetOption("framework-id") ?? "*",
            IsEnabled = arguments.GetBoolFlag("enabled") ? true : arguments.GetBoolFlag("disabled") ? false : null,
            IsKeyboardFocusable = arguments.GetBoolFlag("focusable") ? true : arguments.GetBoolFlag("not-focusable") ? false : null,
            SupportsBackgroundClick = arguments.GetBoolFlag("background-click") ? true : null,
            SupportsBackgroundText = arguments.GetBoolFlag("background-text") ? true : null,
            SupportsBackgroundKeys = arguments.GetBoolFlag("background-keys") ? true : null,
            SupportsForegroundInputFallback = arguments.GetBoolFlag("foreground-fallback") ? true : null,
            EnsureForegroundWindow = arguments.GetBoolFlag("ensure-foreground"),
            AllowForegroundInputFallback = arguments.GetBoolFlag("allow-foreground-input"),
            UiAutomation = arguments.GetBoolFlag("uia"),
            IncludeUiAutomation = arguments.GetBoolFlag("include-uia"),
            All = arguments.GetBoolFlag("all")
        };
    }
}
