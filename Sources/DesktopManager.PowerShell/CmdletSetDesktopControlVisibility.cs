using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Shows or hides a desktop control.</summary>
/// <para type="synopsis">Shows or hides a desktop control.</para>
/// <para type="description">Updates the visibility state of a previously resolved Win32-backed control.</para>
/// <example>
///   <summary>Hide a resolved control</summary>
///   <code>Get-DesktopWindowControl -ActiveWindow | Select-Object -First 1 | Set-DesktopControlVisibility -Visible:$false -PassThru</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopControlVisibility", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopControlVisibility : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to update.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">True to show the control; false to hide it.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public bool Visible { get; set; }

    /// <summary>
    /// <para type="description">Return the updated control state.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        if (!ShouldProcess(Control.Text ?? Control.ClassName, Visible ? "Show control" : "Hide control")) {
            return;
        }

        DesktopControlState state = new DesktopAutomationService().SetControlVisibility(Control, Visible);
        if (PassThru.IsPresent) {
            WriteObject(state);
        }
    }
}
