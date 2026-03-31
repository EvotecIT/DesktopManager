using System;
using System.Linq;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Shows or hides a desktop window.</summary>
/// <para type="synopsis">Shows or hides a desktop window.</para>
/// <example>
///   <para>Hide all Notepad windows</para>
///   <code>Set-DesktopWindowVisibility -Name "*Notepad*" -Hide</code>
/// </example>
/// <example>
///   <para>Show all Notepad windows</para>
///   <code>Set-DesktopWindowVisibility -Name "*Notepad*" -Show</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopWindowVisibility", SupportsShouldProcess = true, DefaultParameterSetName = "Show")]
public sealed class CmdletSetDesktopWindowVisibility : PSCmdlet {
    /// <summary>
    /// <para type="description">Window title to match. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// <para type="description">Show the window.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Show")]
    public SwitchParameter Show { get; set; }

    /// <summary>
    /// <para type="description">Hide the window.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Hide")]
    public SwitchParameter Hide { get; set; }

    /// <summary>
    /// Changes visibility of matching windows.
    /// </summary>
    protected override void BeginProcessing() {
        bool visible = ParameterSetName == "Show";
        var automation = new DesktopAutomationService();
        IReadOnlyList<WindowInfo> windows = automation.GetWindows(new WindowQueryOptions {
            TitlePattern = Name,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });
        foreach (var window in windows) {
            if (ShouldProcess($"Window '{window.Title}'", visible ? "Show" : "Hide")) {
                try {
                    automation.SetWindowVisibility(window.Handle, visible);
                } catch (Exception ex) {
                    WriteWarning($"Failed to change visibility for '{window.Title}': {ex.Message}");
                }
            }
        }
    }
}
