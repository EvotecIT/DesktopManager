using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Focuses a desktop control.</summary>
/// <para type="synopsis">Focuses a desktop control.</para>
/// <para type="description">Sets focus to a previously resolved control and returns the observed post-mutation state when requested.</para>
/// <example>
///   <summary>Focus a resolved control</summary>
///   <code>Get-DesktopWindowControl -ActiveWindow | Select-Object -First 1 | Set-DesktopControlFocus</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopControlFocus", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopControlFocus : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to focus.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">Ensure the parent window becomes foreground before focusing the control.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter EnsureForeground { get; set; }

    /// <summary>
    /// <para type="description">Return the updated control state.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        if (!ShouldProcess(Control.Text ?? Control.ClassName, "Focus control")) {
            return;
        }

        DesktopControlState state = new DesktopAutomationService().FocusControl(Control, EnsureForeground);
        if (PassThru.IsPresent) {
            WriteObject(state);
        }
    }
}
