using System;
using System.IO;

namespace DesktopManager.Cli;

internal static class CliApplication {
    public static int Run(string[] args) {
        return Run(args, Console.Out, Console.Error);
    }

    internal static int Run(string[] args, TextWriter output, TextWriter error) {
        TextWriter originalOutput = Console.Out;
        TextWriter originalError = Console.Error;
        try {
            if (!ReferenceEquals(output, originalOutput)) {
                Console.SetOut(output);
            }
            if (!ReferenceEquals(error, originalError)) {
                Console.SetError(error);
            }

            var parsed = CommandLineArguments.Parse(args);
            if (parsed.IsEmpty || parsed.HasFlag("help")) {
                output.WriteLine(HelpText.GetGeneralHelp());
                return 0;
            }

            string group = parsed.GetCommandPart(0)?.ToLowerInvariant() ?? string.Empty;
            string action = parsed.GetCommandPart(1)?.ToLowerInvariant() ?? string.Empty;
            if (RequiresAction(group) && string.IsNullOrWhiteSpace(action)) {
                throw new CommandLineException($"Missing required {group} command.");
            }

            return group switch {
                "desktop" => DesktopCommands.Run(action, parsed),
                "window" => WindowCommands.Run(action, parsed),
                "control" => ControlCommands.Run(action, parsed),
                "monitor" => MonitorCommands.Run(action, parsed),
                "process" => ProcessCommands.Run(action, parsed),
                "screenshot" => ScreenshotCommands.Run(action, parsed),
                "target" => TargetCommands.Run(action, parsed),
                "control-target" => ControlTargetCommands.Run(action, parsed),
                "layout" => LayoutCommands.Run(action, parsed),
                "snapshot" => SnapshotCommands.Run(action, parsed),
                "diagnostic" => DiagnosticCommands.Run(action, parsed),
                "workflow" => WorkflowCommands.Run(action, parsed),
                "mcp" => McpCommands.Run(action, parsed),
                "help" => ShowGroupHelp(parsed, output),
                _ => throw new CommandLineException($"Unknown command group '{group}'.")
            };
        } catch (CommandLineException ex) {
            error.WriteLine($"Error: {ex.Message}");
            error.WriteLine();
            error.WriteLine(HelpText.GetGeneralHelp());
            return 1;
        } catch (Exception ex) {
            error.WriteLine($"Unhandled error: {ex.Message}");
            return 1;
        } finally {
            if (!ReferenceEquals(Console.Out, originalOutput)) {
                Console.SetOut(originalOutput);
            }
            if (!ReferenceEquals(Console.Error, originalError)) {
                Console.SetError(originalError);
            }
        }
    }

    internal static bool RequiresAction(string? group) {
        return group?.ToLowerInvariant() switch {
            "desktop" => true,
            "window" => true,
            "control" => true,
            "monitor" => true,
            "process" => true,
            "screenshot" => true,
            "target" => true,
            "control-target" => true,
            "layout" => true,
            "snapshot" => true,
            "diagnostic" => true,
            "workflow" => true,
            "mcp" => true,
            _ => false
        };
    }

    internal static string GetHelpText(string? topic) {
        return topic?.ToLowerInvariant() switch {
            "desktop" => HelpText.GetDesktopHelp(),
            "window" => HelpText.GetWindowHelp(),
            "control" => HelpText.GetControlHelp(),
            "monitor" => HelpText.GetMonitorHelp(),
            "process" => HelpText.GetProcessHelp(),
            "screenshot" => HelpText.GetScreenshotHelp(),
            "target" => HelpText.GetTargetHelp(),
            "control-target" => HelpText.GetControlTargetHelp(),
            "layout" => HelpText.GetLayoutHelp(),
            "snapshot" => HelpText.GetSnapshotHelp(),
            "diagnostic" => HelpText.GetDiagnosticHelp(),
            "workflow" => HelpText.GetWorkflowHelp(),
            "mcp" => HelpText.GetMcpHelp(),
            _ => HelpText.GetGeneralHelp()
        };
    }

    private static int ShowGroupHelp(CommandLineArguments parsed, TextWriter output) {
        string? topic = parsed.GetCommandPart(1);
        string help = GetHelpText(topic);

        output.WriteLine(help);
        return 0;
    }
}
