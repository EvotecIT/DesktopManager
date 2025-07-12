using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Drags the mouse cursor.</summary>
/// <para type="synopsis">Simulates dragging the mouse.</para>
/// <example>
///   <code>Invoke-DesktopMouseDrag -Button Left -StartX 0 -StartY 0 -EndX 100 -EndY 100</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopMouseDrag", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletInvokeDesktopMouseDrag : PSCmdlet {
    /// <summary>
    /// <para type="description">Button to hold. Defaults to Left.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public MouseButton Button { get; set; } = MouseButton.Left;

    /// <summary>
    /// <para type="description">Starting X coordinate.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public int StartX { get; set; }

    /// <summary>
    /// <para type="description">Starting Y coordinate.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public int StartY { get; set; }

    /// <summary>
    /// <para type="description">Ending X coordinate.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 2)]
    public int EndX { get; set; }

    /// <summary>
    /// <para type="description">Ending Y coordinate.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 3)]
    public int EndY { get; set; }

    /// <summary>
    /// <para type="description">Delay in milliseconds between steps.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int StepDelay { get; set; } = 0;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (ShouldProcess("Mouse", $"Drag {Button} from {StartX},{StartY} to {EndX},{EndY}")) {
            var manager = new WindowManager();
            manager.DragMouse(Button, StartX, StartY, EndX, EndY, StepDelay);
        }
    }
}
