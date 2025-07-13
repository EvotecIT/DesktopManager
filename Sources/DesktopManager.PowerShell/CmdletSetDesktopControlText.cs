using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Sets text of a window control.</summary>
/// <para type="synopsis">Updates the text displayed by a control.</para>
/// <example>
///   <code>Set-DesktopControlText -Control $ctrl -Text "Hello"</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopControlText", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopControlText : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to modify.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">Text to assign.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public string Text { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (ShouldProcess(Control.Text ?? Control.ClassName, "Set text")) {
            WindowControlService.SetControlText(Control, Text);
        }
    }
}
