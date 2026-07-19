using System;

namespace DesktopManager.Cli;

internal static class DriverCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "stage" => Stage(arguments),
            "install" => Install(arguments),
            "update" => Update(arguments),
            "delete" => Delete(arguments),
            "export" => Export(arguments),
            "rollback" => Rollback(arguments),
            "create-root" => CreateRoot(arguments),
            _ => throw new CommandLineException($"Unknown driver command '{action}'.")
        };
    }

    private static int List(CommandLineArguments arguments) {
        Guid? classGuid = null;
        string? classGuidValue = arguments.GetOption("class-guid");
        if (!string.IsNullOrWhiteSpace(classGuidValue)) {
            if (!Guid.TryParse(classGuidValue, out Guid parsed)) {
                throw new CommandLineException("Option '--class-guid' expects a GUID value.");
            }
            classGuid = parsed;
        }
        var service = new DeviceManagementService();
        OutputFormatter.WriteJson(service.GetDriverPackages(new DesktopDriverPackageQuery {
            PublishedInfName = arguments.GetOption("published-name"),
            ClassGuid = classGuid,
            IncludeFiles = arguments.GetBoolFlag("files"),
            IncludeDevices = arguments.GetBoolFlag("devices")
        }));
        return 0;
    }

    private static int Stage(CommandLineArguments arguments) {
        DeviceCommands.RequireConfirmation(arguments);
        var service = new DeviceManagementService();
        return DeviceCommands.WriteResult(service.StageDriver(arguments.GetRequiredOption("inf")));
    }

    private static int Install(CommandLineArguments arguments) {
        DeviceCommands.RequireConfirmation(arguments);
        DeviceCommands.RequireExpertForFlag(arguments, "force");
        var service = new DeviceManagementService();
        return DeviceCommands.WriteResult(service.InstallDriver(
            arguments.GetRequiredOption("inf"),
            arguments.GetBoolFlag("force")));
    }

    private static int Update(CommandLineArguments arguments) {
        DeviceCommands.RequireConfirmation(arguments);
        DeviceCommands.RequireExpertForFlag(arguments, "force");
        var service = new DeviceManagementService();
        return DeviceCommands.WriteResult(service.UpdateDriver(
            arguments.GetRequiredOption("inf"),
            arguments.GetRequiredOption("hardware-id"),
            arguments.GetBoolFlag("force")));
    }

    private static int Delete(CommandLineArguments arguments) {
        DeviceCommands.RequireConfirmation(arguments);
        if (arguments.GetBoolFlag("force") || arguments.GetBoolFlag("uninstall-devices")) {
            DeviceCommands.RequireExpert(arguments, "Forced package deletion or device uninstallation");
        }
        var service = new DeviceManagementService();
        return DeviceCommands.WriteResult(service.DeleteDriver(
            arguments.GetRequiredOption("published-name"),
            arguments.GetBoolFlag("uninstall-devices"),
            arguments.GetBoolFlag("force")));
    }

    private static int Export(CommandLineArguments arguments) {
        DeviceCommands.RequireConfirmation(arguments);
        DeviceCommands.RequireExpertForFlag(arguments, "overwrite");
        var service = new DeviceManagementService();
        return DeviceCommands.WriteResult(service.ExportDriver(
            arguments.GetRequiredOption("published-name"),
            arguments.GetRequiredOption("destination"),
            arguments.GetBoolFlag("overwrite")));
    }

    private static int Rollback(CommandLineArguments arguments) {
        DeviceCommands.RequireConfirmation(arguments);
        var service = new DeviceManagementService();
        return DeviceCommands.WriteResult(service.RollbackDriver(arguments.GetRequiredOption("instance-id")));
    }

    private static int CreateRoot(CommandLineArguments arguments) {
        DeviceCommands.RequireConfirmation(arguments);
        DeviceCommands.RequireExpert(arguments, "Creating a ROOT-enumerated device");
        var service = new DeviceManagementService();
        return DeviceCommands.WriteResult(service.CreateRootDevice(
            arguments.GetRequiredOption("inf"),
            arguments.GetRequiredOption("hardware-id")));
    }
}
