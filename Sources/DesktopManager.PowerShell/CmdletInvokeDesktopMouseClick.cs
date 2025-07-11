using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Simulates a mouse click.</summary>
/// <para type="synopsis">Simulates a mouse click.</para>
/// <example>
///   <code>Invoke-DesktopMouseClick -Button Left</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopMouseClick", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletInvokeDesktopMouseClick : PSCmdlet {
    /// <summary>
    /// <para type="description">Button to click. Defaults to Left.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public MouseButton Button { get; set; } = MouseButton.Left;

    /// <summary>Performs the click.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess("Mouse", $"Click {Button}")) {
            var manager = new WindowManager();
            manager.ClickMouse(Button);
        }
    }
}
