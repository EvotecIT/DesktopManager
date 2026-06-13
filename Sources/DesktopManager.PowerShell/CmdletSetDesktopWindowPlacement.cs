using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Applies a reliable reusable placement operation to matching desktop windows.</summary>
/// <para type="synopsis">Applies reliable desktop window placement.</para>
/// <para type="description">Uses the shared DesktopManager placement engine to move, resize, restore, or maximize matching windows with root-handle normalization, retry, and verification support.</para>
/// <example>
///   <para>Move a window to a monitor and maximize it</para>
///   <code>Set-DesktopWindowPlacement -Name "Remote Desktop Manager*" -Placement Maximize -MonitorIndex 1 -PassThru</code>
/// </example>
/// <example>
///   <para>Move a window to an exact rectangle, including negative virtual-desktop coordinates</para>
///   <code>Set-DesktopWindowPlacement -Name "Visual Studio Code*" -Placement ExactRectangle -Left -3840 -Top 19 -Width 1920 -Height 2088</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopWindowPlacement", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopWindowPlacement : PSCmdlet {
    /// <summary>
    /// <para type="description">The title of the window to place. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">The placement to apply.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public WindowPlacementKind Placement { get; set; }

    /// <summary>
    /// <para type="description">The monitor target to use when MonitorIndex is not specified.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public WindowMonitorTargetKind MonitorTarget { get; set; } = WindowMonitorTargetKind.Current;

    /// <summary>
    /// <para type="description">Explicit DesktopManager monitor index to target.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int? MonitorIndex { get; set; }

    /// <summary>
    /// <para type="description">Exact left coordinate for ExactRectangle placement. Negative virtual-desktop coordinates are supported.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int? Left { get; set; }

    /// <summary>
    /// <para type="description">Exact top coordinate for ExactRectangle placement. Negative virtual-desktop coordinates are supported.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int? Top { get; set; }

    /// <summary>
    /// <para type="description">Exact width for ExactRectangle placement.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int? Width { get; set; }

    /// <summary>
    /// <para type="description">Exact height for ExactRectangle placement.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int? Height { get; set; }

    /// <summary>
    /// <para type="description">Skip post-action geometry verification.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public SwitchParameter NoVerify { get; set; }

    /// <summary>
    /// <para type="description">Return the placement result, including observed final window state and diagnostic snapshots.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public SwitchParameter PassThru { get; set; }

    /// <summary>
    /// Applies placement to matching windows.
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

        if (windows.Count == 0) {
            WriteWarning($"No windows matched '{Name}'.");
            return;
        }

        foreach (WindowInfo window in windows) {
            if (!ShouldProcess($"Window '{window.Title}'", GetActionDescription())) {
                continue;
            }

            try {
                WindowPlacementResult result = automation.ApplyWindowPlacement(new WindowPlacementRequest {
                    TargetWindowHandle = window.Handle,
                    Placement = Placement,
                    MonitorTarget = MonitorTarget,
                    MonitorIndex = MonitorIndex,
                    ExactLeft = Left,
                    ExactTop = Top,
                    ExactWidth = Width,
                    ExactHeight = Height,
                    VerifyAfterAction = !NoVerify.IsPresent
                });

                if (PassThru.IsPresent) {
                    WriteObject(result);
                }
            } catch (Exception ex) {
                WriteWarning($"Failed to place window '{window.Title}': {ex.Message}");
            }
        }
    }

    private string GetActionDescription() {
        var parts = new List<string> {
            Placement.ToString()
        };

        if (MonitorIndex.HasValue) {
            parts.Add($"monitor {MonitorIndex.Value}");
        } else if (MonitorTarget != WindowMonitorTargetKind.Current) {
            parts.Add(MonitorTarget.ToString());
        }

        if (Left.HasValue || Top.HasValue || Width.HasValue || Height.HasValue) {
            parts.Add($"rectangle {Left?.ToString() ?? "*"}, {Top?.ToString() ?? "*"}, {Width?.ToString() ?? "*"}x{Height?.ToString() ?? "*"}");
        }

        return string.Join(" ", parts);
    }
}
