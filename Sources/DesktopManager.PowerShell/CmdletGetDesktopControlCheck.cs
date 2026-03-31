using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Gets the check state of a button control.</summary>
/// <para type="synopsis">Retrieves the check state of a control.</para>
/// <example>
///   <code>Get-DesktopControlCheck -Control $ctrl</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopControlCheck")]
[SupportedOSPlatform("windows")]
public sealed class CmdletGetDesktopControlCheck : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to query.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        bool state = new DesktopAutomationService().GetControlCheckState(Control);
        WriteObject(state);
    }
}
