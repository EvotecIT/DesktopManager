using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DesktopManager.Cli;

internal static class TargetCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "save" => Save(arguments),
            "get" => Get(arguments),
            "list" => List(arguments),
            "resolve" => Resolve(arguments),
            _ => throw new CommandLineException($"Unknown target command '{action}'.")
        };
    }

    private static int Save(CommandLineArguments arguments) {
        WindowTargetResult result = DesktopOperations.SaveWindowTarget(
            arguments.GetRequiredCommandPart(2, "target name"),
            arguments.GetOption("description"),
            arguments.GetIntOption("x"),
            arguments.GetIntOption("y"),
            arguments.GetDoubleOption("x-ratio"),
            arguments.GetDoubleOption("y-ratio"),
            arguments.GetIntOption("width"),
            arguments.GetIntOption("height"),
            arguments.GetDoubleOption("width-ratio"),
            arguments.GetDoubleOption("height-ratio"),
            arguments.GetBoolFlag("client-area"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteSavedTargetResult(result, Console.Out);
    }

    private static int Get(CommandLineArguments arguments) {
        WindowTargetResult result = DesktopOperations.GetWindowTarget(arguments.GetRequiredCommandPart(2, "target name"));
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteTargetResult(result, Console.Out);
    }

    private static int List(CommandLineArguments arguments) {
        var names = DesktopOperations.ListWindowTargets();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(names);
            return 0;
        }

        return WriteTargetNames(names, Console.Out);
    }

    private static int Resolve(CommandLineArguments arguments) {
        IReadOnlyList<ResolvedWindowTargetResult> results = DesktopOperations.ResolveWindowTargets(
            CreateCriteria(arguments, includeEmptyDefault: true),
            arguments.GetRequiredCommandPart(2, "target name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteResolvedTargets(results, Console.Out);
    }

    internal static int WriteTargetResult(WindowTargetResult result, TextWriter writer) {
        writer.WriteLine(result.Name);
        writer.WriteLine($"- Path: {result.Path}");
        writer.WriteLine($"- ClientArea: {(result.Target.ClientArea ? "Yes" : "No")}");
        writer.WriteLine($"- X: {FormatNumber(result.Target.X)}");
        writer.WriteLine($"- Y: {FormatNumber(result.Target.Y)}");
        writer.WriteLine($"- XRatio: {FormatNumber(result.Target.XRatio)}");
        writer.WriteLine($"- YRatio: {FormatNumber(result.Target.YRatio)}");
        writer.WriteLine($"- Width: {FormatNumber(result.Target.Width)}");
        writer.WriteLine($"- Height: {FormatNumber(result.Target.Height)}");
        writer.WriteLine($"- WidthRatio: {FormatNumber(result.Target.WidthRatio)}");
        writer.WriteLine($"- HeightRatio: {FormatNumber(result.Target.HeightRatio)}");
        if (!string.IsNullOrWhiteSpace(result.Target.Description)) {
            writer.WriteLine($"- Description: {result.Target.Description}");
        }
        return 0;
    }

    private static string FormatNumber(int? value) {
        return value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : "-";
    }

    private static string FormatNumber(double? value) {
        return value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : "-";
    }

    internal static int WriteSavedTargetResult(WindowTargetResult result, TextWriter writer) {
        writer.WriteLine($"save: {result.Name}");
        writer.WriteLine(result.Path);
        return 0;
    }

    internal static int WriteTargetNames(IReadOnlyList<string> names, TextWriter writer) {
        if (names.Count == 0) {
            writer.WriteLine("No named targets found.");
            return 0;
        }

        foreach (string name in names.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)) {
            writer.WriteLine(name);
        }
        return 0;
    }

    internal static int WriteResolvedTargets(IReadOnlyList<ResolvedWindowTargetResult> results, TextWriter writer) {
        foreach (ResolvedWindowTargetResult result in results) {
            writer.WriteLine($"{result.Name}: {result.Window.Title} ({result.Window.Handle})");
            writer.WriteLine($"- Relative: {result.RelativeX},{result.RelativeY}");
            writer.WriteLine($"- Screen: {result.ScreenX},{result.ScreenY}");
            if (result.ScreenWidth.HasValue && result.ScreenHeight.HasValue) {
                writer.WriteLine($"- Area: {result.ScreenWidth}x{result.ScreenHeight}");
            }
            writer.WriteLine($"- ClientArea: {(result.Target.ClientArea ? "Yes" : "No")}");
        }
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
}
