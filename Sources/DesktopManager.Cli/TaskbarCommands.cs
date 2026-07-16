using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static class TaskbarCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "set" => Set(arguments),
            "set-auto-hide" => SetAutoHide(arguments),
            _ => throw new CommandLineException($"Unknown taskbar command '{action}'.")
        };
    }

    private static int List(CommandLineArguments arguments) {
        var service = new TaskbarService();
        object result = new {
            autoHide = service.GetTaskbarAutoHide(),
            taskbars = service.GetTaskbars()
        };
        OutputFormatter.WriteJson(result);
        return 0;
    }

    private static int Set(CommandLineArguments arguments) {
        int monitorIndex = arguments.GetRequiredIntOption("monitor-index");
        bool show = arguments.GetBoolFlag("show");
        bool hide = arguments.GetBoolFlag("hide");
        if (show && hide) {
            throw new CommandLineException("Cannot combine '--show' and '--hide'.");
        }
        string? positionValue = arguments.GetOption("position");
        var service = new TaskbarService();
        if (!string.IsNullOrWhiteSpace(positionValue)) {
            if (!Enum.TryParse(positionValue, true, out TaskbarPosition position)) {
                throw new CommandLineException($"Unsupported taskbar position '{positionValue}'.");
            }
            service.SetTaskbarPosition(monitorIndex, position);
        }
        if (show || hide) {
            service.SetTaskbarVisibility(monitorIndex, show);
        }
        OutputFormatter.WriteJson(service.GetTaskbars().Where(taskbar => taskbar.MonitorIndex == monitorIndex).ToArray());
        return 0;
    }

    private static int SetAutoHide(CommandLineArguments arguments) {
        bool on = arguments.GetBoolFlag("on");
        bool off = arguments.GetBoolFlag("off");
        if (on == off) {
            throw new CommandLineException("Specify exactly one of '--on' or '--off'.");
        }
        var service = new TaskbarService();
        service.SetTaskbarAutoHide(on);
        OutputFormatter.WriteJson(new { autoHide = service.GetTaskbarAutoHide() });
        return 0;
    }
}
