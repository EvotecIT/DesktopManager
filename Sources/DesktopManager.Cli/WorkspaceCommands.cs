using System.Linq;

namespace DesktopManager.Cli;

internal static class WorkspaceCommands {
    internal const string Help = """
Workspace commands:
  desktopmanager workspace capture --path <layout.json> [--virtual-desktops] [--explorer-folders]
  desktopmanager workspace preview --path <layout.json>
  desktopmanager workspace restore --path <layout.json> [--virtual-desktops] [--application-pins] [--reopen-explorer] [--wallpapers]
  desktopmanager workspace desktops

Virtual desktop inspection, assignments, pins and wallpapers use an experimental Windows 11 shell contract.
Restore reports every skipped or failed action and returns exit code 2 when recovery is incomplete.
Explorer reopening is opt-in. Other applications must already be running. Wallpapers are restored only on explicit request.
""";

    public static int Run(string action, CommandLineArguments arguments) {
        var service = new WorkspaceRecoveryService();
        switch (action) {
            case "capture": {
                WindowLayout layout = service.Capture(new WorkspaceCaptureOptions {
                    IncludeVirtualDesktops = arguments.GetBoolFlag("virtual-desktops"),
                    IncludeExplorerFolders = arguments.GetBoolFlag("explorer-folders")
                });
                WorkspaceRecoveryService.Write(arguments.GetRequiredOption("path"), layout);
                OutputFormatter.WriteJson(layout);
                return layout.CaptureWarnings.Count == 0 ? 0 : 2;
            }
            case "preview": {
                WindowLayoutPlan plan = service.Plan(WorkspaceRecoveryService.Read(arguments.GetRequiredOption("path")), arguments.GetBoolFlag("virtual-desktops"));
                OutputFormatter.WriteJson(plan);
                return plan.Warnings.Count == 0 && plan.Windows.All(window => window.Status == WindowLayoutMatchStatus.Matched) ? 0 : 2;
            }
            case "restore": {
                WorkspaceRestoreResult result = service.Restore(WorkspaceRecoveryService.Read(arguments.GetRequiredOption("path")), new WorkspaceRestoreOptions {
                    RestoreVirtualDesktops = arguments.GetBoolFlag("virtual-desktops"),
                    RestoreApplicationPins = arguments.GetBoolFlag("application-pins"),
                    ReopenExplorerFolders = arguments.GetBoolFlag("reopen-explorer"),
                    RestoreWallpapers = arguments.GetBoolFlag("wallpapers")
                });
                OutputFormatter.WriteJson(result);
                return IsComplete(result) ? 0 : 2;
            }
            case "desktops": {
                using var desktops = new ExperimentalVirtualDesktopService();
                OutputFormatter.WriteJson(desktops.GetDesktops());
                return 0;
            }
            default: throw new CommandLineException($"Unknown workspace command '{action}'.");
        }
    }

    internal static bool IsComplete(WorkspaceRestoreResult result) => result.IsComplete;
}
