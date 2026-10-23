using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Restores window positions from a saved layout.</summary>
/// <para type="synopsis">Restores desktop window layout.</para>
[Cmdlet(VerbsData.Restore, "DesktopWindowLayout", SupportsShouldProcess = true)]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
public sealed class CmdletRestoreDesktopWindowLayout : PSCmdlet {
    /// <summary>
    /// <para type="description">Path to the layout file.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; }

    /// <summary>
    /// <para type="description">Validate layout before applying.</para>
    /// </summary>
    [Parameter()]
    public SwitchParameter Validate { get; set; }

    /// <para>Preview one-to-one matches without moving windows or reopening folders.</para>
    [Parameter]
    public SwitchParameter Preview { get; set; }

    /// <para>Restore saved desktop assignments and window pins using the experimental shell contract.</para>
    [Parameter]
    public SwitchParameter RestoreVirtualDesktops { get; set; }

    /// <para>Also permit application-wide pin changes affecting every window of an application.</para>
    [Parameter]
    public SwitchParameter RestoreApplicationPins { get; set; }

    /// <para>Explicitly restore saved per-desktop wallpapers. Slideshow configuration is untouched.</para>
    [Parameter]
    public SwitchParameter RestoreWallpapers { get; set; }

    /// <para>Reopen missing saved Explorer filesystem folders. Other applications are not launched.</para>
    [Parameter]
    public SwitchParameter ReopenExplorerFolders { get; set; }

    /// <para>Return individual recovery outcomes, including missing or ambiguous matches.</para>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <summary>
    /// Begin processing.
    /// </summary>
    protected override void BeginProcessing() {
        if (Preview) {
            WriteObject(new WorkspaceRecoveryService().Plan(WorkspaceRecoveryService.Read(Path, Validate), RestoreVirtualDesktops));
            return;
        }
        var automation = new DesktopAutomationService();
        if (ShouldProcess($"Layout file '{Path}'", "Restore desktop window layout")) {
            if (RestoreVirtualDesktops || RestoreApplicationPins || RestoreWallpapers || ReopenExplorerFolders || PassThru) {
                WorkspaceRestoreResult result = new WorkspaceRecoveryService().Restore(WorkspaceRecoveryService.Read(Path, Validate), new WorkspaceRestoreOptions {
                    RestoreVirtualDesktops = RestoreVirtualDesktops,
                    RestoreApplicationPins = RestoreApplicationPins,
                    RestoreWallpapers = RestoreWallpapers,
                    ReopenExplorerFolders = ReopenExplorerFolders
                });
                foreach (string warning in result.Warnings) { WriteWarning(warning); }
                foreach (WorkspaceWindowResult window in result.Windows) {
                    if (window.Error != null) { WriteWarning($"{window.Title}: {window.Error}"); }
                    else if (!window.PositionApplied) { WriteWarning($"{window.Title}: {window.MatchStatus}"); }
                }
                foreach (VirtualDesktopWallpaperResult wallpaper in result.Wallpapers) {
                    if (wallpaper.Error != null) { WriteWarning($"Wallpaper {wallpaper.DesktopId}: {wallpaper.Error}"); }
                }
                if (PassThru) { WriteObject(result); }
            } else {
                automation.LoadLayout(Path, Validate.IsPresent);
            }
        }
    }
}
