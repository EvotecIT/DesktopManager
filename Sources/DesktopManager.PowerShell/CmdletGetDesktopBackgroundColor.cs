using System.Management.Automation;
using DesktopManager;
namespace DesktopManager.PowerShell;

/// <summary>Gets the desktop background color.</summary>
/// <para type="synopsis">Gets the desktop background color.</para>
/// <para type="description">Returns the RGB color value currently used as the desktop background.</para>
/// <example>
///   <summary>Retrieve current background color</summary>
///   <code>Get-DesktopBackgroundColor</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopBackgroundColor")]
public sealed class CmdletGetDesktopBackgroundColor : PSCmdlet {
    /// <summary>Begin processing.</summary>
    protected override void BeginProcessing() {
        Monitors monitors = new Monitors();
        WriteObject(monitors.GetBackgroundColor());
    }
}
