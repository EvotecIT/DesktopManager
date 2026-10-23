using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Saves the current desktop window layout to a file.</summary>
/// <para type="synopsis">Saves the current desktop window layout.</para>
[Cmdlet(VerbsData.Save, "DesktopWindowLayout", SupportsShouldProcess = true)]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
public sealed class CmdletSaveDesktopWindowLayout : PSCmdlet {
    /// <summary>
    /// <para type="description">Path where the layout should be stored.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; }

    /// <para>Capture virtual desktops, pins and wallpapers using the experimental Windows 11 shell contract.</para>
    [Parameter]
    public SwitchParameter IncludeVirtualDesktops { get; set; }

    /// <para>Capture Explorer filesystem folder paths for matching and optional reopening.</para>
    [Parameter]
    public SwitchParameter IncludeExplorerFolders { get; set; }

    /// <summary>
    /// Begin processing.
    /// </summary>
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        if (ShouldProcess($"Layout file '{Path}'", "Save desktop window layout")) {
            if (IncludeVirtualDesktops || IncludeExplorerFolders) {
                WindowLayout layout = new WorkspaceRecoveryService().Capture(new WorkspaceCaptureOptions {
                    IncludeVirtualDesktops = IncludeVirtualDesktops,
                    IncludeExplorerFolders = IncludeExplorerFolders
                });
                WorkspaceRecoveryService.Write(Path, layout);
                foreach (string warning in layout.CaptureWarnings) { WriteWarning(warning); }
            } else {
                automation.SaveLayout(Path);
            }
            WriteObject(Path);
        }
    }
}
