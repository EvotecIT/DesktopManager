using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static class WorkstationCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "save" => Save(arguments),
            "list" => List(arguments),
            "show" => Show(arguments),
            "apply" => Apply(arguments),
            "delete" => Delete(arguments),
            _ => throw new CommandLineException($"Unknown workstation command '{action}'.")
        };
    }

    private static int Save(CommandLineArguments arguments) {
        string name = arguments.GetRequiredOption("name");
        WorkstationProfile profile = new WorkstationProfileService().SaveProfile(name);
        OutputFormatter.WriteJson(new {
            name,
            path = DesktopStateStore.GetWorkstationProfilePath(name),
            profile
        });
        return 0;
    }

    private static int List(CommandLineArguments arguments) {
        IReadOnlyList<string> names = WorkstationProfileStore.List();
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
        OutputFormatter.WriteJson(WorkstationProfileStore.Load(arguments.GetRequiredOption("name")));
        return 0;
    }

    private static int Apply(CommandLineArguments arguments) {
        var options = new WorkstationProfileApplyOptions {
            RequireAllMonitors = !arguments.GetBoolFlag("allow-missing-monitors"),
            ApplyDisplays = !arguments.GetBoolFlag("skip-displays"),
            ApplyAudio = !arguments.GetBoolFlag("skip-audio"),
            ApplyPersonalization = !arguments.GetBoolFlag("skip-personalization"),
            ApplyMachinePolicies = arguments.GetBoolFlag("include-machine-policies"),
            ApplyTaskbars = !arguments.GetBoolFlag("skip-taskbars"),
            RollbackOnFailure = !arguments.GetBoolFlag("no-rollback")
        };
        WorkstationProfileApplyResult result = new WorkstationProfileService()
            .ApplyProfile(arguments.GetRequiredOption("name"), options);
        OutputFormatter.WriteJson(result);
        return result.Succeeded ? 0 : 2;
    }

    private static int Delete(CommandLineArguments arguments) {
        string name = arguments.GetRequiredOption("name");
        bool deleted = WorkstationProfileStore.Delete(name);
        OutputFormatter.WriteJson(new { name, deleted });
        return deleted ? 0 : 2;
    }
}
