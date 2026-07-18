using System;

namespace DesktopManager.Cli;

internal static class WifiCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "interfaces" => ListInterfaces(),
            "profiles" => ListProfiles(arguments),
            "connect" => Connect(arguments),
            _ => throw new CommandLineException($"Unknown wifi command '{action}'.")
        };
    }

    private static int ListInterfaces() {
        using var service = new WifiProfileService();
        OutputFormatter.WriteJson(service.GetInterfaces());
        return 0;
    }

    private static int ListProfiles(CommandLineArguments arguments) {
        Guid? interfaceId = ParseInterfaceId(arguments.GetOption("interface-id"));
        using var service = new WifiProfileService();
        OutputFormatter.WriteJson(service.GetProfiles(interfaceId));
        return 0;
    }

    private static int Connect(CommandLineArguments arguments) {
        string profileName = arguments.GetRequiredOption("profile");
        Guid? interfaceId = ParseInterfaceId(arguments.GetOption("interface-id"));
        int timeoutMilliseconds = arguments.GetIntOption("timeout-ms") ?? 30000;
        if (timeoutMilliseconds <= 0) {
            throw new CommandLineException("Option '--timeout-ms' expects a value greater than 0.");
        }

        using var service = new WifiProfileService();
        DesktopWifiConnectionResult result = service.ConnectProfileAsync(
            profileName,
            interfaceId,
            TimeSpan.FromMilliseconds(timeoutMilliseconds)).GetAwaiter().GetResult();
        OutputFormatter.WriteJson(result);
        return result.Succeeded ? 0 : 2;
    }

    private static Guid? ParseInterfaceId(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }
        if (Guid.TryParse(value, out Guid interfaceId)) {
            return interfaceId;
        }

        throw new CommandLineException("Option '--interface-id' expects a GUID value.");
    }
}
