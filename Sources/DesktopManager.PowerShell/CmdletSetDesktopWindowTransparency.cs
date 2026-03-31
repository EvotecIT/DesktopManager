using System;
using System.Linq;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Sets the transparency level of a desktop window.</summary>
/// <para type="synopsis">Sets the transparency level of a desktop window.</para>
/// <example>
///   <para>Make Notepad semi-transparent</para>
///   <code>Set-DesktopWindowTransparency -Name "*Notepad*" -Alpha 128</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopWindowTransparency", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopWindowTransparency : PSCmdlet {
    /// <summary>
    /// <para type="description">Title of the window. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Transparency alpha from 0 (transparent) to 255 (opaque).</para>
    /// </summary>
    [ValidateRange(0, 255)]
    [Parameter(Mandatory = true, Position = 1)]
    public byte Alpha { get; set; }

    /// <summary>
    /// Applies the transparency setting to matching windows.
    /// </summary>
    protected override void BeginProcessing() {
        var automation = new DesktopAutomationService();
        IReadOnlyList<WindowInfo> windows = automation.GetWindows(new WindowQueryOptions {
            TitlePattern = Name,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });
        foreach (var window in windows) {
            if (ShouldProcess($"Window '{window.Title}'", $"Set transparency to {Alpha}")) {
                try {
                    automation.SetWindowTransparency(window.Handle, Alpha);
                } catch (Exception ex) {
                    WriteWarning($"Failed to set transparency for '{window.Title}': {ex.Message}");
                }
            }
        }
    }
}
