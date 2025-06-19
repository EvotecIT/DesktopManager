using System.Management.Automation;
using DesktopManager;

namespace DesktopManager.PowerShell;

/// <summary>Sets the desktop background color.</summary>
/// <para type="synopsis">Sets the desktop background color.</para>
[Cmdlet(VerbsCommon.Set, "DesktopBackgroundColor", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopBackgroundColor : PSCmdlet {
    /// <summary>
    /// <para type="description">Color as RGB value.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public uint Color;

    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        if (ShouldProcess("Desktop", $"Set background color to 0x{Color:X6}")) {
            Monitors monitors = new Monitors();
            monitors.SetBackgroundColor(Color);
        }
    }
}
