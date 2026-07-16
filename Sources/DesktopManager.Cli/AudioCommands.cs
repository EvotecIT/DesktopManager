using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DesktopManager.Cli;

internal static class AudioCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "set-default" => SetDefault(arguments),
            "set-volume" => SetVolume(arguments),
            "set-mute" => SetMute(arguments),
            _ => throw new CommandLineException($"Unknown audio command '{action}'.")
        };
    }

    private static int List(CommandLineArguments arguments) {
        AudioDataFlow flow = ParseEnum(arguments.GetOption("flow"), AudioDataFlow.All, "flow");
        AudioEndpointState states = arguments.GetBoolFlag("active")
            ? AudioEndpointState.Active
            : AudioEndpointState.All;
        IReadOnlyList<AudioEndpointInfo> endpoints = new AudioService().GetEndpoints(flow, states);
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(endpoints);
            return 0;
        }

        IReadOnlyList<IReadOnlyList<string>> rows = endpoints.Select(endpoint => (IReadOnlyList<string>)new[] {
            endpoint.DataFlow.ToString(),
            endpoint.State.ToString(),
            endpoint.IsDefault ? string.Join(",", endpoint.DefaultRoles) : string.Empty,
            endpoint.VolumePercent?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty,
            endpoint.IsMuted?.ToString() ?? string.Empty,
            endpoint.Name,
            endpoint.Id
        }).ToArray();
        OutputFormatter.WriteTable(new[] { "Flow", "State", "DefaultRoles", "Volume", "Muted", "Name", "Id" }, rows);
        return 0;
    }

    private static int SetDefault(CommandLineArguments arguments) {
        string id = arguments.GetRequiredOption("id");
        IReadOnlyList<string> values = arguments.GetOptions("role");
        AudioRole[] roles = values.Count == 0
            ? Array.Empty<AudioRole>()
            : values.Select(value => ParseEnum<AudioRole>(value, default, "role")).ToArray();
        var service = new AudioService();
        service.SetDefaultAudioDevice(id, roles);
        OutputFormatter.WriteJson(service.GetEndpoint(id));
        return 0;
    }

    private static int SetVolume(CommandLineArguments arguments) {
        string id = arguments.GetRequiredOption("id");
        double volume = arguments.GetDoubleOption("volume")
            ?? throw new CommandLineException("Missing required option '--volume'.");
        var service = new AudioService();
        service.SetEndpointVolume(id, (float)volume);
        OutputFormatter.WriteJson(service.GetEndpoint(id));
        return 0;
    }

    private static int SetMute(CommandLineArguments arguments) {
        bool on = arguments.GetBoolFlag("on");
        bool off = arguments.GetBoolFlag("off");
        if (on == off) {
            throw new CommandLineException("Specify exactly one of '--on' or '--off'.");
        }

        string id = arguments.GetRequiredOption("id");
        var service = new AudioService();
        service.SetEndpointMute(id, on);
        OutputFormatter.WriteJson(service.GetEndpoint(id));
        return 0;
    }

    private static T ParseEnum<T>(string? value, T fallback, string option) where T : struct {
        if (string.IsNullOrWhiteSpace(value)) {
            return fallback;
        }
        if (Enum.TryParse(value, true, out T parsed)) {
            return parsed;
        }
        throw new CommandLineException($"Option '--{option}' has unsupported value '{value}'.");
    }
}
