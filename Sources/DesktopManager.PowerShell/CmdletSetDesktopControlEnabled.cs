using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Enables or disables a desktop control.</summary>
/// <para type="synopsis">Enables or disables a desktop control.</para>
/// <para type="description">Updates the enabled state of a previously resolved Win32-backed control.</para>
/// <example>
///   <summary>Disable a resolved control</summary>
///   <code>Get-DesktopWindowControl -ActiveWindow | Select-Object -First 1 | Set-DesktopControlEnabled -Enabled:$false -PassThru</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopControlEnabled", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopControlEnabled : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to update.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">True to enable the control; false to disable it.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public bool Enabled { get; set; }

    /// <summary>
    /// <para type="description">Return the updated control state.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        if (!ShouldProcess(Control.Text ?? Control.ClassName, Enabled ? "Enable control" : "Disable control")) {
            return;
        }

        DesktopControlState state = new DesktopAutomationService().SetControlEnabled(Control, Enabled);
        if (PassThru.IsPresent) {
            WriteObject(state);
        }
    }
}
