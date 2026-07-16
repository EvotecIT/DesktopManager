using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static class RadioCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(),
            "set" => Set(arguments),
            "airplane" => Airplane(arguments),
            _ => throw new CommandLineException($"Unknown radio command '{action}'.")
        };
    }

    private static int List() {
        using var service = new RadioService();
        OutputFormatter.WriteJson(service.GetRadiosAsync().GetAwaiter().GetResult());
        return 0;
    }

    private static int Set(CommandLineArguments arguments) {
        DesktopRadioKind kind = ParseEnum<DesktopRadioKind>(arguments.GetRequiredOption("kind"), "kind");
        DesktopRadioState state = ParseEnum<DesktopRadioState>(arguments.GetRequiredOption("state"), "state");
        if (state != DesktopRadioState.On && state != DesktopRadioState.Off) {
            throw new CommandLineException("Option '--state' must be On or Off.");
        }
        using var service = new RadioService();
        IReadOnlyList<DesktopRadioSetResult> results = service.SetRadioStateAsync(
            kind,
            state,
            arguments.GetOption("name")).GetAwaiter().GetResult();
        OutputFormatter.WriteJson(results);
        return results.All(result => result.Applied) ? 0 : 2;
    }

    private static int Airplane(CommandLineArguments arguments) {
        if (!arguments.GetBoolFlag("experimental")) {
            throw new CommandLineException("Global airplane mode is experimental and requires '--experimental'.");
        }
        string operation = arguments.GetCommandPart(2)?.ToLowerInvariant() ?? "get";
        var service = new ExperimentalAirplaneModeService();
        AirplaneModeState state = operation switch {
            "get" => service.GetState(),
            "set" => service.SetState(ParseEnum<AirplaneModeState>(arguments.GetRequiredOption("state"), "state")),
            _ => throw new CommandLineException($"Unknown radio airplane command '{operation}'.")
        };
        OutputFormatter.WriteJson(new { experimental = true, state });
        return 0;
    }

    private static T ParseEnum<T>(string value, string option) where T : struct {
        if (Enum.TryParse(value, true, out T parsed)) {
            return parsed;
        }
        throw new CommandLineException($"Option '--{option}' has unsupported value '{value}'.");
    }
}
