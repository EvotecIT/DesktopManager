using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Scrolls the mouse wheel.</summary>
/// <para type="synopsis">Scrolls the mouse wheel.</para>
/// <example>
///   <code>Invoke-DesktopMouseScroll -Delta 120</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopMouseScroll", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletInvokeDesktopMouseScroll : PSCmdlet {
    /// <summary>
    /// <para type="description">Scroll amount. Positive scrolls up.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public int Delta { get; set; }

    /// <summary>Scrolls the wheel.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess("Mouse", $"Scroll {Delta}")) {
            var manager = new WindowManager();
            manager.ScrollMouse(Delta);
        }
    }
}
