using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DesktopManager.Cli;

internal static class WindowCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "geometry" => Geometry(arguments),
            "process-info" => ProcessInfo(arguments, owner: false),
            "owner-process-info" => ProcessInfo(arguments, owner: true),
            "exists" => Exists(arguments),
            "active-matches" => ActiveMatches(arguments),
            "move" => Move(arguments),
            "click" => Click(arguments),
            "drag" => Drag(arguments),
            "scroll" => Scroll(arguments),
            "focus" => Focus(arguments),
            "keep-alive-list" => KeepAliveList(arguments),
            "keep-alive-start" => KeepAliveStart(arguments),
            "keep-alive-stop" => KeepAliveStop(arguments),
            "minimize" => Minimize(arguments),
            "maximize" => Maximize(arguments),
            "restore" => Restore(arguments),
            "close" => Close(arguments),
            "topmost" => TopMost(arguments),
            "visibility" => Visibility(arguments),
            "transparency" => Transparency(arguments),
            "snap" => Snap(arguments),
            "type" => Type(arguments),
            "keys" => Keys(arguments),
            "wait" => Wait(arguments),
            _ => throw new CommandLineException($"Unknown window command '{action}'.")
        };
    }

    private static int List(CommandLineArguments arguments) {
        IReadOnlyList<WindowResult> windows = DesktopOperations.ListWindows(CreateCriteria(arguments, includeEmptyDefault: false));
        if (windows.Count == 0) {
            if (arguments.GetBoolFlag("json")) {
                OutputFormatter.WriteJson(Array.Empty<object>());
            } else {
                WriteNoMatchingWindows(Console.Out);
            }
            return 0;
        }

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(windows);
            return 0;
        }

        return WriteWindowListResults(windows, Console.Out);
    }

    private static int ProcessInfo(CommandLineArguments arguments, bool owner) {
        IReadOnlyList<WindowProcessInfoResult> results = DesktopOperations.GetWindowProcessInfo(
            CreateCriteria(arguments, includeEmptyDefault: true),
            owner);
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        if (results.Count == 0) {
            return WriteNoMatchingWindows(Console.Out);
        }

        return WriteWindowProcessInfoResults(results, Console.Out);
    }

    internal static int WriteWindowListResults(IReadOnlyList<WindowResult> windows, TextWriter writer) {
        IReadOnlyList<IReadOnlyList<string>> rows = windows
            .Select(window => (IReadOnlyList<string>)new[] {
                window.ProcessId.ToString(),
                window.Handle.Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase),
                window.MonitorIndex.ToString(),
                window.IsVisible ? "Yes" : "No",
                window.State ?? string.Empty,
                window.Title
            })
            .ToArray();

        OutputFormatter.WriteTable(writer, new[] { "PID", "Handle", "Mon", "Visible", "State", "Title" }, rows);
        return 0;
    }

    internal static int WriteWindowProcessInfoResults(IReadOnlyList<WindowProcessInfoResult> results, TextWriter writer) {
        for (int index = 0; index < results.Count; index++) {
            WindowProcessInfoResult result = results[index];
            if (index > 0) {
                writer.WriteLine();
            }

            writer.WriteLine($"window: {result.Window.Title} ({result.Window.Handle})");
            writer.WriteLine($"- Scope: {(result.IsOwnerProcess ? "Owner" : "Window")}");
            writer.WriteLine($"- ProcessId: {result.ProcessId}");
            writer.WriteLine($"- ThreadId: {result.ThreadId}");
            writer.WriteLine($"- ProcessName: {result.ProcessName}");
            writer.WriteLine($"- IsElevated: {FormatNullableBoolean(result.IsElevated)}");
            writer.WriteLine($"- IsWow64: {FormatNullableBoolean(result.IsWow64)}");
            if (!string.IsNullOrWhiteSpace(result.ProcessPath)) {
                writer.WriteLine($"- ProcessPath: {result.ProcessPath}");
            }
        }

        return 0;
    }

    internal static int WriteWindowKeepAliveResults(IReadOnlyList<WindowResult> windows, TextWriter writer) {
        writer.WriteLine($"keep-alive: {windows.Count} window(s)");
        if (windows.Count == 0) {
            return 0;
        }

        return WriteWindowListResults(windows, writer);
    }

    private static int Exists(CommandLineArguments arguments) {
        WindowAssertionResult result = DesktopOperations.WindowExists(CreateCriteria(arguments, includeEmptyDefault: true));
        return WriteAssertionResult(arguments, result, "Matching window found.", "No matching windows found.");
    }

    private static int Geometry(CommandLineArguments arguments) {
        IReadOnlyList<WindowGeometryResult> geometries = DesktopOperations.GetWindowGeometry(CreateCriteria(arguments, includeEmptyDefault: true));
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(geometries);
            return 0;
        }

        if (geometries.Count == 0) {
            return WriteNoMatchingWindows(Console.Out);
        }

        return WriteWindowGeometryResults(geometries, Console.Out);
    }

    private static int ActiveMatches(CommandLineArguments arguments) {
        WindowAssertionResult result = DesktopOperations.ActiveWindowMatches(CreateCriteria(arguments, includeEmptyDefault: true));
        return WriteAssertionResult(arguments, result, "Active window matches.", "Active window does not match.");
    }

    private static int KeepAliveList(CommandLineArguments arguments) {
        IReadOnlyList<WindowResult> windows = DesktopOperations.ListWindowKeepAlive();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(windows);
            return 0;
        }

        return WriteWindowKeepAliveResults(windows, Console.Out);
    }

    private static int KeepAliveStart(CommandLineArguments arguments) {
        int intervalMilliseconds = arguments.GetIntOption("interval-ms") ?? 60000;
        if (intervalMilliseconds <= 0) {
            throw new CommandLineException("Option '--interval-ms' expects a value greater than 0.");
        }

        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.StartWindowKeepAlive(CreateCriteria(arguments, includeEmptyDefault: true), intervalMilliseconds));
    }

    private static int KeepAliveStop(CommandLineArguments arguments) {
        bool allSessions = arguments.GetBoolFlag("all-sessions");
        if (allSessions) {
            if (HasWindowSelector(arguments) || arguments.GetBoolFlag("all")) {
                throw new CommandLineException("Cannot combine '--all-sessions' with window selectors or '--all'.");
            }

            return WriteWindowMutationResult(arguments, DesktopOperations.StopAllWindowKeepAlive());
        }

        return WriteWindowMutationResult(arguments, DesktopOperations.StopWindowKeepAlive(CreateCriteria(arguments, includeEmptyDefault: true)));
    }

    private static int Move(CommandLineArguments arguments) {
        WindowChangeResult result = DesktopOperations.MoveWindow(
            CreateCriteria(arguments, includeEmptyDefault: true),
            arguments.GetIntOption("monitor"),
            arguments.GetIntOption("x"),
            arguments.GetIntOption("y"),
            arguments.GetIntOption("width"),
            arguments.GetIntOption("height"),
            arguments.GetBoolFlag("activate"),
            CreateArtifactOptions(arguments));
        return WriteWindowMutationResult(arguments, result);
    }

    private static int Focus(CommandLineArguments arguments) {
        return WriteWindowMutationResult(arguments, DesktopOperations.FocusWindow(CreateCriteria(arguments, includeEmptyDefault: true), CreateArtifactOptions(arguments)));
    }

    private static int Click(CommandLineArguments arguments) {
        string? targetName = arguments.GetOption("target");
        if (!string.IsNullOrWhiteSpace(targetName)) {
            return WriteWindowMutationResult(
                arguments,
                DesktopOperations.ClickWindowTarget(
                    CreateCriteria(arguments, includeEmptyDefault: true),
                    targetName,
                    arguments.GetOption("button") ?? "left",
                    arguments.GetBoolFlag("activate"),
                    CreateArtifactOptions(arguments)));
        }

        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.ClickWindowPoint(
                CreateCriteria(arguments, includeEmptyDefault: true),
                arguments.GetIntOption("x"),
                arguments.GetIntOption("y"),
                arguments.GetDoubleOption("x-ratio"),
                arguments.GetDoubleOption("y-ratio"),
                arguments.GetOption("button") ?? "left",
                arguments.GetBoolFlag("activate"),
                arguments.GetBoolFlag("client-area"),
                CreateArtifactOptions(arguments)));
    }

    private static int Drag(CommandLineArguments arguments) {
        string? startTargetName = arguments.GetOption("start-target");
        string? endTargetName = arguments.GetOption("end-target");
        if (!string.IsNullOrWhiteSpace(startTargetName) || !string.IsNullOrWhiteSpace(endTargetName)) {
            if (string.IsNullOrWhiteSpace(startTargetName) || string.IsNullOrWhiteSpace(endTargetName)) {
                throw new CommandLineException("Both '--start-target' and '--end-target' are required when using named targets.");
            }

            return WriteWindowMutationResult(
                arguments,
                DesktopOperations.DragWindowTargets(
                    CreateCriteria(arguments, includeEmptyDefault: true),
                    startTargetName,
                    endTargetName,
                    arguments.GetOption("button") ?? "left",
                    arguments.GetIntOption("step-delay-ms") ?? 0,
                    arguments.GetBoolFlag("activate"),
                    CreateArtifactOptions(arguments)));
        }

        return WriteWindowMutationResult(
            arguments,
                DesktopOperations.DragWindowPoints(
                CreateCriteria(arguments, includeEmptyDefault: true),
                arguments.GetIntOption("start-x"),
                arguments.GetIntOption("start-y"),
                arguments.GetDoubleOption("start-x-ratio"),
                arguments.GetDoubleOption("start-y-ratio"),
                arguments.GetIntOption("end-x"),
                arguments.GetIntOption("end-y"),
                arguments.GetDoubleOption("end-x-ratio"),
                arguments.GetDoubleOption("end-y-ratio"),
                arguments.GetOption("button") ?? "left",
                arguments.GetIntOption("step-delay-ms") ?? 0,
                arguments.GetBoolFlag("activate"),
                arguments.GetBoolFlag("client-area"),
                CreateArtifactOptions(arguments)));
    }

    private static int Scroll(CommandLineArguments arguments) {
        string? targetName = arguments.GetOption("target");
        if (!string.IsNullOrWhiteSpace(targetName)) {
            return WriteWindowMutationResult(
                arguments,
                DesktopOperations.ScrollWindowTarget(
                    CreateCriteria(arguments, includeEmptyDefault: true),
                    targetName,
                    arguments.GetRequiredIntOption("delta"),
                    arguments.GetBoolFlag("activate"),
                    CreateArtifactOptions(arguments)));
        }

        return WriteWindowMutationResult(
            arguments,
                DesktopOperations.ScrollWindowPoint(
                CreateCriteria(arguments, includeEmptyDefault: true),
                arguments.GetIntOption("x"),
                arguments.GetIntOption("y"),
                arguments.GetDoubleOption("x-ratio"),
                arguments.GetDoubleOption("y-ratio"),
                arguments.GetRequiredIntOption("delta"),
                arguments.GetBoolFlag("activate"),
                arguments.GetBoolFlag("client-area"),
                CreateArtifactOptions(arguments)));
    }

    private static int Minimize(CommandLineArguments arguments) {
        return WriteWindowMutationResult(arguments, DesktopOperations.MinimizeWindows(CreateCriteria(arguments, includeEmptyDefault: true), CreateArtifactOptions(arguments)));
    }

    private static int Maximize(CommandLineArguments arguments) {
        return WriteWindowMutationResult(arguments, DesktopOperations.MaximizeWindows(CreateCriteria(arguments, includeEmptyDefault: true), CreateArtifactOptions(arguments)));
    }

    private static int Restore(CommandLineArguments arguments) {
        return WriteWindowMutationResult(arguments, DesktopOperations.RestoreWindows(CreateCriteria(arguments, includeEmptyDefault: true), CreateArtifactOptions(arguments)));
    }

    private static int Close(CommandLineArguments arguments) {
        return WriteWindowMutationResult(arguments, DesktopOperations.CloseWindows(CreateCriteria(arguments, includeEmptyDefault: true), CreateArtifactOptions(arguments)));
    }

    private static int TopMost(CommandLineArguments arguments) {
        bool on = arguments.GetBoolFlag("on");
        bool off = arguments.GetBoolFlag("off");
        if (on == off) {
            throw new CommandLineException("Specify exactly one of '--on' or '--off'.");
        }

        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.SetWindowTopMost(CreateCriteria(arguments, includeEmptyDefault: true), on, CreateArtifactOptions(arguments)));
    }

    private static int Visibility(CommandLineArguments arguments) {
        bool show = arguments.GetBoolFlag("show");
        bool hide = arguments.GetBoolFlag("hide");
        if (show == hide) {
            throw new CommandLineException("Specify exactly one of '--show' or '--hide'.");
        }

        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.SetWindowVisibility(CreateCriteria(arguments, includeEmptyDefault: true), show, CreateArtifactOptions(arguments)));
    }

    private static int Transparency(CommandLineArguments arguments) {
        int alpha = arguments.GetRequiredIntOption("alpha");
        if (alpha < 0 || alpha > 255) {
            throw new CommandLineException("Option '--alpha' expects a value from 0 to 255.");
        }

        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.SetWindowTransparency(CreateCriteria(arguments, includeEmptyDefault: true), (byte)alpha, CreateArtifactOptions(arguments)));
    }

    private static int Snap(CommandLineArguments arguments) {
        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.SnapWindow(CreateCriteria(arguments, includeEmptyDefault: true), arguments.GetRequiredOption("position"), CreateArtifactOptions(arguments)));
    }

    private static int Type(CommandLineArguments arguments) {
        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.TypeWindowText(
                CreateCriteria(arguments, includeEmptyDefault: true),
                CreateTypeOptions(arguments),
                CreateArtifactOptions(arguments)));
    }

    private static int Keys(CommandLineArguments arguments) {
        IReadOnlyList<string> keys = arguments.GetOptions("keys");
        if (keys.Count == 0) {
            string single = arguments.GetRequiredOption("keys");
            keys = new[] { single };
        }

        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.SendWindowKeys(
                CreateCriteria(arguments, includeEmptyDefault: true),
                keys,
                !arguments.GetBoolFlag("no-activate"),
                CreateArtifactOptions(arguments)));
    }

    private static int Wait(CommandLineArguments arguments) {
        WaitForWindowResult result = DesktopOperations.WaitForWindow(
            CreateCriteria(arguments, includeEmptyDefault: true),
            arguments.GetIntOption("timeout-ms") ?? 10000,
            arguments.GetIntOption("interval-ms") ?? 200);

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteWaitResult(result, Console.Out);
    }

    private static int WriteWindowMutationResult(CommandLineArguments arguments, WindowChangeResult payload) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(payload);
            return 0;
        }

        return WriteWindowMutationResult(payload, Console.Out);
    }

    internal static int WriteWindowMutationResult(WindowChangeResult payload, TextWriter writer) {
        writer.WriteLine($"{payload.Action}: {payload.Count} window(s) success={payload.Success} safety={payload.SafetyMode} elapsed-ms={payload.ElapsedMilliseconds}");
        if (!string.IsNullOrWhiteSpace(payload.TargetName)) {
            writer.WriteLine($"target: {payload.TargetKind ?? "selector"} {payload.TargetName}");
        }

        if (payload.Verification != null) {
            writer.WriteLine($"verification: verified={payload.Verification.Verified} mode={payload.Verification.Mode} observed={payload.Verification.ObservedCount}/{payload.Verification.ExpectedCount} matched={payload.Verification.MatchedCount} mismatches={payload.Verification.MismatchCount} tolerance-px={payload.Verification.TolerancePixels}");
            writer.WriteLine($"verification-summary: {payload.Verification.Summary}");
            if (payload.Verification.ActiveWindow != null) {
                writer.WriteLine($"verification-active: {payload.Verification.ActiveWindow.Title} [PID {payload.Verification.ActiveWindow.ProcessId}]");
            }

            foreach (string note in payload.Verification.Notes) {
                writer.WriteLine($"verification-note: {note}");
            }
        }

        if (payload.BeforeScreenshots.Count > 0 || payload.AfterScreenshots.Count > 0) {
            writer.WriteLine($"artifacts: before={payload.BeforeScreenshots.Count} after={payload.AfterScreenshots.Count}");
        }

        foreach (string warning in payload.ArtifactWarnings) {
            writer.WriteLine($"warning: {warning}");
        }

        foreach (WindowResult window in payload.Windows) {
            writer.WriteLine($"- {window.Title} [PID {window.ProcessId}]");
        }

        return 0;
    }

    internal static int WriteWindowGeometryResults(IReadOnlyList<WindowGeometryResult> geometries, TextWriter writer) {
        foreach (WindowGeometryResult geometry in geometries) {
            writer.WriteLine($"window: {geometry.Window.Title} ({geometry.Window.Handle})");
            writer.WriteLine($"- Window: {geometry.WindowLeft},{geometry.WindowTop} {geometry.WindowWidth}x{geometry.WindowHeight}");
            writer.WriteLine($"- Client: {geometry.ClientLeft},{geometry.ClientTop} {geometry.ClientWidth}x{geometry.ClientHeight}");
            writer.WriteLine($"- ClientOffset: {geometry.ClientOffsetLeft},{geometry.ClientOffsetTop}");
        }
        return 0;
    }

    internal static int WriteWaitResult(WaitForWindowResult result, TextWriter writer) {
        writer.WriteLine($"wait: {result.Count} window(s) after {result.ElapsedMilliseconds}ms");
        foreach (WindowResult window in result.Windows) {
            writer.WriteLine($"- {window.Title} [PID {window.ProcessId}]");
        }

        return 0;
    }

    internal static MutationArtifactOptions? CreateArtifactOptions(CommandLineArguments arguments) {
        bool captureBefore = arguments.GetBoolFlag("capture-before");
        bool captureAfter = arguments.GetBoolFlag("capture-after");
        string? artifactDirectory = arguments.GetOption("artifact-directory");
        bool verifyAfter = arguments.GetBoolFlag("verify") || arguments.GetIntOption("verify-tolerance-px").HasValue;
        int verificationTolerancePixels = arguments.GetIntOption("verify-tolerance-px") ?? 10;
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

    private static int WriteAssertionResult(CommandLineArguments arguments, WindowAssertionResult payload, string successText, string failureText) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(payload);
        } else {
            return WriteAssertionResult(payload, successText, failureText, Console.Out);
        }

        return payload.Matched ? 0 : 2;
    }

    internal static int WriteAssertionResult(WindowAssertionResult payload, string successText, string failureText, TextWriter writer) {
        writer.WriteLine(payload.Matched ? successText : failureText);
        if (payload.ActiveWindow != null) {
            writer.WriteLine($"Active: {payload.ActiveWindow.Title} [PID {payload.ActiveWindow.ProcessId}]");
        }

        foreach (WindowResult window in payload.Windows) {
            writer.WriteLine($"- {window.Title} [PID {window.ProcessId}]");
        }

        return payload.Matched ? 0 : 2;
    }

    internal static int WriteNoMatchingWindows(TextWriter writer) {
        writer.WriteLine("No matching windows found.");
        return 0;
    }

    internal static WindowSelectionCriteria CreateCriteria(CommandLineArguments arguments, bool includeEmptyDefault) {
        return new WindowSelectionCriteria {
            TitlePattern = arguments.GetOption("title") ?? "*",
            ProcessNamePattern = arguments.GetOption("process") ?? "*",
            ClassNamePattern = arguments.GetOption("class") ?? "*",
            ProcessId = arguments.GetIntOption("pid"),
            Handle = arguments.GetOption("handle"),
            Active = arguments.GetBoolFlag("active"),
            IncludeHidden = arguments.GetBoolFlag("include-hidden"),
            IncludeCloaked = !arguments.GetBoolFlag("exclude-cloaked"),
            IncludeOwned = !arguments.GetBoolFlag("exclude-owned"),
            IncludeEmptyTitles = arguments.GetBoolFlag("include-empty") || includeEmptyDefault,
            All = arguments.GetBoolFlag("all")
        };
    }

    internal static WindowTextCommandOptions CreateTypeOptions(CommandLineArguments arguments) {
        bool hostedSession = arguments.GetBoolFlag("hosted-session");
        bool physicalKeys = arguments.GetBoolFlag("physical-keys");
        bool scriptMode = arguments.GetBoolFlag("script");
        int? delayMilliseconds = arguments.GetIntOption("delay-ms");
        int? scriptLineDelayMilliseconds = arguments.GetIntOption("line-delay-ms");
        return new WindowTextCommandOptions {
            Text = arguments.GetRequiredOption("text"),
            Paste = arguments.GetBoolFlag("paste"),
            DelayMilliseconds = delayMilliseconds ?? (hostedSession ? 35 : 0),
            ForegroundInput = arguments.GetBoolFlag("foreground-input") || physicalKeys || hostedSession,
            PhysicalKeys = physicalKeys,
            HostedSession = hostedSession,
            ScriptMode = scriptMode,
            ScriptChunkLength = arguments.GetIntOption("chunk-size") ?? 120,
            ScriptLineDelayMilliseconds = scriptLineDelayMilliseconds ?? (hostedSession && scriptMode ? 120 : 0)
        };
    }

    private static bool HasWindowSelector(CommandLineArguments arguments) {
        return !string.IsNullOrWhiteSpace(arguments.GetOption("title")) ||
               !string.IsNullOrWhiteSpace(arguments.GetOption("process")) ||
               !string.IsNullOrWhiteSpace(arguments.GetOption("class")) ||
               arguments.GetIntOption("pid").HasValue ||
               !string.IsNullOrWhiteSpace(arguments.GetOption("handle")) ||
               arguments.GetBoolFlag("active") ||
               arguments.GetBoolFlag("include-empty") ||
               arguments.GetBoolFlag("include-hidden") ||
               arguments.GetBoolFlag("exclude-cloaked") ||
               arguments.GetBoolFlag("exclude-owned");
    }

    private static string FormatNullableBoolean(bool? value) {
        if (!value.HasValue) {
            return "Unknown";
        }

        return value.Value ? "True" : "False";
    }
}
