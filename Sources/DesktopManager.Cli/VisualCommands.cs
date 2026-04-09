using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace DesktopManager.Cli;

internal static class VisualCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "save" => Save(arguments),
            "get" => Get(arguments),
            "list" => List(arguments),
            "assert" => Assert(arguments),
            "resolve" => Resolve(arguments),
            "read-text" => ReadText(arguments),
            "resolve-text" => ResolveText(arguments),
            _ => throw new CommandLineException($"Unknown visual command '{action}'.")
        };
    }

    private static int Save(CommandLineArguments arguments) {
        VisualBaselineResult result = DesktopOperations.SaveVisualBaseline(
            arguments.GetRequiredCommandPart(2, "visual baseline name"),
            CreateCriteria(arguments),
            arguments.GetOption("target"),
            arguments.GetBoolFlag("client-area"),
            arguments.GetOption("description"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteBaselineResult(result, Console.Out, "save");
    }

    private static int Get(CommandLineArguments arguments) {
        VisualBaselineResult result = DesktopOperations.GetVisualBaseline(arguments.GetRequiredCommandPart(2, "visual baseline name"));
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteBaselineResult(result, Console.Out, "visual");
    }

    private static int List(CommandLineArguments arguments) {
        IReadOnlyList<string> names = DesktopOperations.ListVisualBaselines();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(names);
            return 0;
        }

        if (names.Count == 0) {
            Console.Out.WriteLine("No named visual baselines found.");
            return 0;
        }

        foreach (string name in names.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)) {
            Console.Out.WriteLine(name);
        }

        return 0;
    }

    private static int Assert(CommandLineArguments arguments) {
        VisualBaselineAssertionResult result = DesktopOperations.AssertVisualBaseline(
            arguments.GetRequiredCommandPart(2, "visual baseline name"),
            CreateCriteria(arguments),
            arguments.GetOption("target"),
            arguments.HasFlag("client-area") ? arguments.GetBoolFlag("client-area") : null,
            arguments.GetDoubleOption("max-changed-ratio") ?? 0.01,
            arguments.GetIntOption("difference-threshold") ?? 24);

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteAssertionResult(result, Console.Out);
    }

    private static int Resolve(CommandLineArguments arguments) {
        VisualBaselineResolveResult result = DesktopOperations.ResolveVisualBaseline(
            arguments.GetRequiredCommandPart(2, "visual baseline name"),
            CreateCriteria(arguments),
            arguments.GetBoolFlag("client-area"),
            arguments.GetDoubleOption("max-average-difference") ?? 12.0,
            arguments.GetIntOption("difference-threshold") ?? 24,
            arguments.GetIntOption("scan-step") ?? 8);

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteResolveResult(result, Console.Out);
    }

    private static int ReadText(CommandLineArguments arguments) {
        WindowTextReadResult result = DesktopOperations.ReadWindowText(
            CreateCriteria(arguments),
            arguments.GetOption("target"),
            arguments.GetBoolFlag("client-area"),
            arguments.GetOption("language"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteTextReadResult(result, Console.Out);
    }

    private static int ResolveText(CommandLineArguments arguments) {
        WindowTextResolveResult result = DesktopOperations.ResolveWindowText(
            CreateCriteria(arguments),
            arguments.GetRequiredCommandPart(2, "OCR query text"),
            arguments.GetBoolFlag("contains"),
            arguments.GetOption("target"),
            arguments.GetBoolFlag("client-area"),
            arguments.GetOption("language"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteTextResolveResult(result, Console.Out);
    }

    internal static int WriteBaselineResult(VisualBaselineResult result, TextWriter writer, string prefix) {
        writer.WriteLine($"{prefix}: {result.Name}");
        writer.WriteLine($"- Path: {result.Path}");
        writer.WriteLine($"- ImagePath: {result.ImagePath}");
        writer.WriteLine($"- Size: {result.Baseline.Width}x{result.Baseline.Height}");
        writer.WriteLine($"- ClientArea: {(result.Baseline.ClientArea ? "Yes" : "No")}");
        if (!string.IsNullOrWhiteSpace(result.Baseline.TargetName)) {
            writer.WriteLine($"- Target: {result.Baseline.TargetName}");
        }
        if (!string.IsNullOrWhiteSpace(result.Baseline.Description)) {
            writer.WriteLine($"- Description: {result.Baseline.Description}");
        }
        if (!string.IsNullOrWhiteSpace(result.Baseline.CreatedUtc)) {
            writer.WriteLine($"- CreatedUtc: {result.Baseline.CreatedUtc}");
        }
        return 0;
    }

    internal static int WriteAssertionResult(VisualBaselineAssertionResult result, TextWriter writer) {
        writer.WriteLine(result.Matched ? "Visual baseline assertion passed." : "Visual baseline assertion failed.");
        writer.WriteLine($"baseline: {result.Name}");
        writer.WriteLine($"window: {result.Window.Title} ({result.Window.Handle})");
        writer.WriteLine($"metrics: changed-samples={result.ChangedSampleCount}/{result.SampleCount} ratio={result.ChangedSampleRatio:F4} avg-diff={result.AverageDifference:F1} threshold={result.DifferenceThreshold} max-ratio={result.MaxChangedRatio:F4} size-changed={result.SizeChanged}");
        if (!string.IsNullOrWhiteSpace(result.TargetName)) {
            writer.WriteLine($"target: {result.TargetName}");
        } else if (result.ClientArea) {
            writer.WriteLine("target: client-area");
        } else {
            writer.WriteLine("target: window");
        }
        return result.Matched ? 0 : 2;
    }

    internal static int WriteResolveResult(VisualBaselineResolveResult result, TextWriter writer) {
        writer.WriteLine(result.Matched ? "Visual baseline resolved." : "Visual baseline best match exceeded tolerance.");
        writer.WriteLine($"baseline: {result.Name}");
        writer.WriteLine($"window: {result.Window.Title} ({result.Window.Handle})");
        writer.WriteLine($"match: relative=({result.RelativeX},{result.RelativeY}) size={result.Width}x{result.Height} screen=({result.ScreenX},{result.ScreenY})");
        writer.WriteLine($"metrics: changed-samples={result.ChangedSampleCount}/{result.SampleCount} ratio={result.ChangedSampleRatio:F4} avg-diff={result.AverageDifference:F1} threshold={result.DifferenceThreshold} max-avg-diff={result.MaxAverageDifference:F1} size-changed={result.SizeChanged} evaluated={result.EvaluatedPositionCount} scan-step={result.ScanStep}");
        writer.WriteLine(result.ClientArea ? "search-region: client-area" : "search-region: window");
        return result.Matched ? 0 : 2;
    }

    internal static int WriteTextReadResult(WindowTextReadResult result, TextWriter writer) {
        writer.WriteLine("Window OCR text extracted.");
        writer.WriteLine($"window: {result.Window.Title} ({result.Window.Handle})");
        writer.WriteLine(result.TargetName != null
            ? $"capture: target '{result.TargetName}'"
            : result.ClientArea
                ? "capture: client-area"
                : "capture: window");
        writer.WriteLine($"language: {result.LanguageTag}");
        writer.WriteLine($"origin: ({result.CaptureScreenX},{result.CaptureScreenY})");
        writer.WriteLine($"lines: {result.Lines.Count}");
        foreach (OcrLineResult line in result.Lines) {
            writer.WriteLine($"line: ({line.X},{line.Y}) {line.Width}x{line.Height} \"{line.Text}\"");
        }

        if (!string.IsNullOrWhiteSpace(result.Text)) {
            writer.WriteLine("text:");
            writer.WriteLine(result.Text);
        }

        return 0;
    }

    internal static int WriteTextResolveResult(WindowTextResolveResult result, TextWriter writer) {
        writer.WriteLine(result.Matched ? "Window OCR text resolved." : "Window OCR text was not found.");
        writer.WriteLine($"query: {result.QueryText}");
        writer.WriteLine($"window: {result.Window.Title} ({result.Window.Handle})");
        writer.WriteLine(result.TargetName != null
            ? $"capture: target '{result.TargetName}'"
            : result.ClientArea
                ? "capture: client-area"
                : "capture: window");
        writer.WriteLine($"language: {result.LanguageTag}");
        writer.WriteLine($"contains: {result.ContainsMatch}");
        writer.WriteLine($"candidates: {result.CandidateCount}");
        if (!result.Matched) {
            return 2;
        }

        writer.WriteLine($"match: kind={result.MatchKind} relative=({result.RelativeX},{result.RelativeY}) size={result.Width}x{result.Height} screen=({result.ScreenX},{result.ScreenY}) action=({result.ActionX},{result.ActionY})");
        writer.WriteLine($"text: {result.MatchedText}");
        if (result.Words.Count > 0) {
            writer.WriteLine("words:");
            foreach (OcrWordResult word in result.Words) {
                writer.WriteLine($"- ({word.X},{word.Y}) {word.Width}x{word.Height} \"{word.Text}\"");
            }
        }

        return 0;
    }

    internal static WindowSelectionCriteria CreateCriteria(CommandLineArguments arguments) {
        return new WindowSelectionCriteria {
            TitlePattern = arguments.GetOption("title") ?? "*",
            ProcessNamePattern = arguments.GetOption("process") ?? "*",
            ClassNamePattern = arguments.GetOption("class") ?? "*",
            ProcessId = arguments.GetIntOption("pid"),
            Handle = arguments.GetOption("handle"),
            Active = arguments.GetBoolFlag("active"),
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true,
            All = false
        };
    }
}
