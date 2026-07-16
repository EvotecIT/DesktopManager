using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static class PersonalizationCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "capture" => Capture(arguments),
            "list" => List(arguments),
            "show" => Show(arguments),
            "restore" => Restore(arguments),
            "apply" => Apply(arguments),
            "delete" => Delete(arguments),
            _ => throw new CommandLineException($"Unknown personalization command '{action}'.")
        };
    }

    private static int Capture(CommandLineArguments arguments) {
        string name = arguments.GetRequiredOption("name");
        PersonalizationSnapshot snapshot = new PersonalizationService().CaptureSnapshot();
        PersonalizationStateStore.SaveSnapshot(name, snapshot);
        OutputFormatter.WriteJson(new { name, path = PersonalizationStateStore.GetSnapshotPath(name), snapshot });
        return 0;
    }

    private static int List(CommandLineArguments arguments) {
        IReadOnlyList<string> names = PersonalizationStateStore.ListSnapshots();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(names);
        } else {
            foreach (string name in names) {
                Console.WriteLine(name);
            }
        }
        return 0;
    }

    private static int Show(CommandLineArguments arguments) {
        OutputFormatter.WriteJson(PersonalizationStateStore.LoadSnapshot(arguments.GetRequiredOption("name")));
        return 0;
    }

    private static int Restore(CommandLineArguments arguments) {
        string name = arguments.GetRequiredOption("name");
        var service = new PersonalizationService();
        service.Restore(
            PersonalizationStateStore.LoadSnapshot(name),
            restoreMachinePolicies: !arguments.GetBoolFlag("skip-machine-policies"));
        OutputFormatter.WriteJson(service.CaptureSnapshot());
        return 0;
    }

    private static int Apply(CommandLineArguments arguments) {
        string path = Path.GetFullPath(arguments.GetRequiredOption("file"));
        if (!File.Exists(path)) {
            throw new CommandLineException($"Personalization settings file '{path}' was not found.");
        }
        PersonalizationSettings? settings = JsonSerializer.Deserialize<PersonalizationSettings>(
            File.ReadAllText(path),
            JsonUtilities.SerializerOptions);
        if (settings == null) {
            throw new CommandLineException("The personalization settings JSON could not be deserialized.");
        }

        var service = new PersonalizationService();
        service.Apply(settings);
        OutputFormatter.WriteJson(service.CaptureSnapshot());
        return 0;
    }

    private static int Delete(CommandLineArguments arguments) {
        string name = arguments.GetRequiredOption("name");
        bool deleted = PersonalizationStateStore.DeleteSnapshot(name);
        OutputFormatter.WriteJson(new { name, deleted });
        return deleted ? 0 : 2;
    }
}
