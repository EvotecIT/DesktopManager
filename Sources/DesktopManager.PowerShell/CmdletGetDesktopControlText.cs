using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Gets text from a window control.</summary>
/// <para type="synopsis">Retrieves the text displayed by a control.</para>
/// <example>
///   <code>Get-DesktopControlText -Control $ctrl</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopControlText")]
[SupportedOSPlatform("windows")]
public sealed class CmdletGetDesktopControlText : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to query.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        string text = WindowControlService.GetControlText(Control);
        WriteObject(text);
    }
}
