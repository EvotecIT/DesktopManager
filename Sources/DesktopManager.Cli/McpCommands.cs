namespace DesktopManager.Cli;

internal static class McpCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "serve" => Serve(arguments),
            _ => throw new CommandLineException($"Unknown mcp command '{action}'.")
        };
    }

    private static int Serve(CommandLineArguments arguments) {
        bool readOnly = arguments.GetBoolFlag("read-only");
        bool allowMutations = arguments.GetBoolFlag("allow-mutations");
        bool allowForegroundInput = arguments.GetBoolFlag("allow-foreground-input");
        bool allowSystemSettings = arguments.GetBoolFlag("allow-system-settings");
        bool allowExperimental = arguments.GetBoolFlag("allow-experimental");
        bool dryRun = arguments.GetBoolFlag("dry-run");
        if (readOnly && allowMutations) {
            throw new CommandLineException("Choose either '--read-only' or '--allow-mutations', not both.");
        }

        var safetyPolicy = new McpSafetyPolicy(
            allowMutations,
            allowForegroundInput,
            dryRun,
            arguments.GetOptions("allow-process"),
            arguments.GetOptions("deny-process"),
            allowSystemSettings,
            allowExperimental);
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(new {
                status = "ready",
                protocolVersion = "2025-06-18",
                mode = "stdio",
                safetyPolicy = safetyPolicy.ToModel()
            });
            return 0;
        }

        return new McpServer(safetyPolicy).Run();
    }
}
