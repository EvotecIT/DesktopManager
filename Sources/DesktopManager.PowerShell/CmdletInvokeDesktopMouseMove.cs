using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Moves the mouse cursor.</summary>
/// <para type="synopsis">Moves the mouse cursor to specific coordinates.</para>
/// <example>
///   <code>Invoke-DesktopMouseMove -X 100 -Y 100</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopMouseMove", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletInvokeDesktopMouseMove : PSCmdlet {
    /// <summary>
    /// <para type="description">X coordinate in pixels.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public int X { get; set; }

    /// <summary>
    /// <para type="description">Y coordinate in pixels.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public int Y { get; set; }

    /// <summary>Moves the cursor.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess("Mouse", $"Move to {X},{Y}")) {
            var manager = new WindowManager();
            manager.MoveMouse(X, Y);
        }
    }
}
