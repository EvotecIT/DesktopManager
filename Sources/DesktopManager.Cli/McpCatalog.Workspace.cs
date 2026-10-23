using System.Collections.Generic;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static partial class McpCatalog {
    private static McpToolDefinition[] CreateWorkspaceTools() => new[] {
        CreateTool("get_workspace_desktops", "Get Workspace Desktops", "Inspect live virtual desktop IDs, names and wallpaper paths using the experimental Windows shell contract.", CreateObjectSchema(), readOnly: true),
        CreateTool("capture_workspace", "Capture Workspace", "Save a desktop-aware window layout. Optional Explorer paths and experimental shell capture require explicit selection.", CreateObjectSchema(new Dictionary<string, object> {
            ["path"] = CreateStringSchema("Destination layout JSON file."),
            ["includeVirtualDesktops"] = CreateBooleanSchema("Use the experimental shell adapter to capture desktop assignments, pin states and wallpapers."),
            ["includeExplorerFolders"] = CreateBooleanSchema("Include Explorer filesystem paths.")
        }, new[] { "path" }), readOnly: false, idempotent: true),
        CreateTool("preview_workspace", "Preview Workspace", "Plan window matches from a saved layout without moving windows or reopening folders.", CreateObjectSchema(new Dictionary<string, object> {
            ["path"] = CreateStringSchema("Saved layout JSON file."),
            ["inspectVirtualDesktops"] = CreateBooleanSchema("Inspect the experimental shell to report deleted desktop targets.")
        }, new[] { "path" }), readOnly: true),
        CreateTool("restore_workspace", "Restore Workspace", "Restore uniquely matched windows and explicitly selected virtual desktop state. Reports incomplete recovery.", CreateObjectSchema(new Dictionary<string, object> {
            ["path"] = CreateStringSchema("Saved layout JSON file."),
            ["restoreVirtualDesktops"] = CreateBooleanSchema("Restore assignments and window pins using the experimental shell adapter."),
            ["restoreApplicationPins"] = CreateBooleanSchema("Also permit application-wide pin changes, affecting every window of the app."),
            ["reopenExplorerFolders"] = CreateBooleanSchema("Reopen missing saved Explorer filesystem folders."),
            ["restoreWallpapers"] = CreateBooleanSchema("Explicitly restore saved wallpapers without changing slideshow configuration.")
        }, new[] { "path" }), readOnly: false, idempotent: true)
    };

    private static bool TryCallWorkspaceTool(string name, JsonElement arguments, out object result) {
        var service = new WorkspaceRecoveryService();
        switch (name) {
            case "get_workspace_desktops":
                using (var desktops = new ExperimentalVirtualDesktopService()) { result = desktops.GetDesktops(); }
                return true;
            case "capture_workspace": {
                WindowLayout layout = service.Capture(new WorkspaceCaptureOptions {
                    IncludeVirtualDesktops = ReadBool(arguments, "includeVirtualDesktops"),
                    IncludeExplorerFolders = ReadBool(arguments, "includeExplorerFolders")
                });
                WorkspaceRecoveryService.Write(ReadRequiredString(arguments, "path"), layout);
                result = layout;
                return true;
            }
            case "preview_workspace":
                result = service.Plan(WorkspaceRecoveryService.Read(ReadRequiredString(arguments, "path")), ReadBool(arguments, "inspectVirtualDesktops"));
                return true;
            case "restore_workspace":
                result = service.Restore(WorkspaceRecoveryService.Read(ReadRequiredString(arguments, "path")), new WorkspaceRestoreOptions {
                    RestoreVirtualDesktops = ReadBool(arguments, "restoreVirtualDesktops"),
                    RestoreApplicationPins = ReadBool(arguments, "restoreApplicationPins"),
                    ReopenExplorerFolders = ReadBool(arguments, "reopenExplorerFolders"),
                    RestoreWallpapers = ReadBool(arguments, "restoreWallpapers")
                });
                return true;
            default: result = null!; return false;
        }
    }
}
