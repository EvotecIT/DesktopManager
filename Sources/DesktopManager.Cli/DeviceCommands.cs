using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static class DeviceCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "get" => Get(arguments),
            "drivers" => Drivers(arguments),
            "classes" => Classes(),
            "containers" => Containers(arguments),
            "enable" => Enable(arguments),
            "disable" => Disable(arguments),
            "restart" => Restart(arguments),
            "remove" => Remove(arguments),
            "scan" => Scan(arguments),
            "set-hardware-ids" => SetHardwareIds(arguments),
            "set-class-filters" => SetClassFilters(arguments),
            _ => throw new CommandLineException($"Unknown device command '{action}'.")
        };
    }

    private static int List(CommandLineArguments arguments) {
        var service = new DeviceManagementService();
        OutputFormatter.WriteJson(service.GetDevices(BuildQuery(arguments)));
        return 0;
    }

    private static int Get(CommandLineArguments arguments) {
        var service = new DeviceManagementService();
        OutputFormatter.WriteJson(service.GetDevice(arguments.GetRequiredOption("instance-id")));
        return 0;
    }

    private static int Drivers(CommandLineArguments arguments) {
        var service = new DeviceManagementService();
        OutputFormatter.WriteJson(service.GetCompatibleDrivers(arguments.GetRequiredOption("instance-id")));
        return 0;
    }

    private static int Classes() {
        var service = new DeviceManagementService();
        OutputFormatter.WriteJson(service.GetDeviceClasses());
        return 0;
    }

    private static int Containers(CommandLineArguments arguments) {
        var service = new DeviceManagementService();
        OutputFormatter.WriteJson(service.GetDeviceContainers(BuildQuery(arguments)));
        return 0;
    }

    private static int Enable(CommandLineArguments arguments) {
        RequireConfirmation(arguments);
        var service = new DeviceManagementService();
        return WriteResult(service.EnableDevice(arguments.GetRequiredOption("instance-id")));
    }

    private static int Disable(CommandLineArguments arguments) {
        RequireConfirmation(arguments);
        RequireExpertForFlag(arguments, "force");
        var service = new DeviceManagementService();
        return WriteResult(service.DisableDevice(
            arguments.GetRequiredOption("instance-id"),
            arguments.GetBoolFlag("force"),
            !arguments.GetBoolFlag("temporary")));
    }

    private static int Restart(CommandLineArguments arguments) {
        RequireConfirmation(arguments);
        var service = new DeviceManagementService();
        return WriteResult(service.RestartDevice(arguments.GetRequiredOption("instance-id")));
    }

    private static int Remove(CommandLineArguments arguments) {
        RequireConfirmation(arguments);
        if (!arguments.GetBoolFlag("device-only")) {
            RequireExpert(arguments, "Removing a device subtree");
        }
        var service = new DeviceManagementService();
        return WriteResult(service.RemoveDevice(
            arguments.GetRequiredOption("instance-id"),
            removeSubtree: !arguments.GetBoolFlag("device-only")));
    }

    private static int Scan(CommandLineArguments arguments) {
        RequireConfirmation(arguments);
        var service = new DeviceManagementService();
        return WriteResult(service.ScanDevices(
            arguments.GetOption("instance-id"),
            arguments.GetBoolFlag("asynchronous")));
    }

    private static int SetHardwareIds(CommandLineArguments arguments) {
        RequireConfirmation(arguments);
        RequireExpert(arguments, "Changing ROOT device hardware identifiers");
        IReadOnlyList<string> hardwareIds = arguments.GetOptions("hardware-id");
        if (hardwareIds.Count == 0) {
            throw new CommandLineException("At least one '--hardware-id' option is required.");
        }
        var service = new DeviceManagementService();
        return WriteResult(service.SetRootHardwareIds(
            arguments.GetRequiredOption("instance-id"),
            hardwareIds));
    }

    private static int SetClassFilters(CommandLineArguments arguments) {
        RequireConfirmation(arguments);
        RequireExpert(arguments, "Changing a device class filter chain");
        if (!Guid.TryParse(arguments.GetRequiredOption("class-guid"), out Guid classGuid)) {
            throw new CommandLineException("Option '--class-guid' expects a GUID value.");
        }
        if (!Enum.TryParse(
            arguments.GetRequiredOption("kind"),
            ignoreCase: true,
            out DesktopDeviceClassFilterKind kind)) {
            throw new CommandLineException("Option '--kind' must be Upper or Lower.");
        }
        var service = new DeviceManagementService();
        return WriteResult(service.SetClassFilters(classGuid, kind, arguments.GetOptions("service")));
    }

    private static DesktopDeviceQuery BuildQuery(CommandLineArguments arguments) {
        bool present = arguments.GetBoolFlag("present");
        bool nonPresent = arguments.GetBoolFlag("non-present");
        bool problem = arguments.GetBoolFlag("problem");
        bool noProblem = arguments.GetBoolFlag("no-problem");
        if (present && nonPresent) {
            throw new CommandLineException("Options '--present' and '--non-present' cannot be combined.");
        }
        if (problem && noProblem) {
            throw new CommandLineException("Options '--problem' and '--no-problem' cannot be combined.");
        }
        Guid? classGuid = null;
        string? classGuidValue = arguments.GetOption("class-guid");
        if (!string.IsNullOrWhiteSpace(classGuidValue)) {
            if (!Guid.TryParse(classGuidValue, out Guid parsed)) {
                throw new CommandLineException("Option '--class-guid' expects a GUID value.");
            }
            classGuid = parsed;
        }
        return new DesktopDeviceQuery {
            InstanceId = arguments.GetOption("instance-id"),
            DeviceId = arguments.GetOption("device-id"),
            ClassName = arguments.GetOption("class"),
            ClassGuid = classGuid,
            EnumeratorName = arguments.GetOption("enumerator"),
            Present = present ? true : nonPresent ? false : null,
            HasProblem = problem ? true : noProblem ? false : null,
            ProblemCode = arguments.GetUIntOption("problem-code"),
            IncludeRelations = arguments.GetBoolFlag("relations"),
            IncludeStack = arguments.GetBoolFlag("stack"),
            IncludeResources = arguments.GetBoolFlag("resources"),
            IncludeInterfaces = arguments.GetBoolFlag("interfaces"),
            IncludeProperties = arguments.GetBoolFlag("properties")
        };
    }

    internal static void RequireConfirmation(CommandLineArguments arguments) {
        if (!arguments.GetBoolFlag("confirm")) {
            throw new CommandLineException("This operation changes Windows device state and requires '--confirm'.");
        }
    }

    internal static void RequireExpert(CommandLineArguments arguments, string operation) {
        if (!arguments.GetBoolFlag("expert")) {
            throw new CommandLineException($"{operation} is an expert operation and requires '--expert'.");
        }
    }

    internal static void RequireExpertForFlag(CommandLineArguments arguments, string flag) {
        if (arguments.GetBoolFlag(flag)) {
            RequireExpert(arguments, $"Option '--{flag}'");
        }
    }

    internal static int WriteResult(DesktopDeviceOperationResult result) {
        OutputFormatter.WriteJson(result);
        return result.Succeeded ? 0 : 2;
    }
}
