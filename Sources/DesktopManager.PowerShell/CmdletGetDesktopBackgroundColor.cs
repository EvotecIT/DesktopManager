using System.Management.Automation;
using DesktopManager;
namespace DesktopManager.PowerShell;

/// <summary>Gets the desktop background color.</summary>
/// <para type="synopsis">Gets the desktop background color.</para>
[Cmdlet(VerbsCommon.Get, "DesktopBackgroundColor")]
public sealed class CmdletGetDesktopBackgroundColor : PSCmdlet {
    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        Monitors monitors = new Monitors();
        WriteObject(monitors.GetBackgroundColor());
    }
}
