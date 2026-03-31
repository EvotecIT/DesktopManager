using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Gets the observable state for a desktop control.</summary>
/// <para type="synopsis">Gets the observable state for a desktop control.</para>
/// <para type="description">Returns the current enabled, visible, focused, and capability state for a previously resolved window control.</para>
/// <example>
///   <summary>Inspect the current state of a resolved control</summary>
///   <code>Get-DesktopWindowControl -ActiveWindow | Select-Object -First 1 | Get-DesktopControlState</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopControlState")]
[SupportedOSPlatform("windows")]
public sealed class CmdletGetDesktopControlState : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to inspect.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <inheritdoc />
    protected override void ProcessRecord() {
        WriteObject(new DesktopAutomationService().GetControlState(Control));
    }
}
