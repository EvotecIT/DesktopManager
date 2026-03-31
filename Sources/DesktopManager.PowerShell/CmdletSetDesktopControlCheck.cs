using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Sets the check state of a button control.</summary>
/// <para type="synopsis">Checks or unchecks a button control.</para>
/// <example>
///   <code>Set-DesktopControlCheck -Control $ctrl -Check $true</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopControlCheck", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletSetDesktopControlCheck : PSCmdlet {
    /// <summary>
    /// <para type="description">Control to modify.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">Desired check state.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public bool Check { get; set; }

    /// <summary>
    /// <para type="description">Re-query the control after changing the check state and report the observed postcondition.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Verify { get; set; }

    /// <summary>
    /// <para type="description">Return a structured mutation result object for the targeted control.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        string action = Check ? "Check" : "Uncheck";
        if (ShouldProcess(Control.Text ?? Control.ClassName, action)) {
            var automation = new DesktopAutomationService();
            try {
                automation.SetControlCheckState(Control, Check);
                if (Verify.IsPresent || PassThru.IsPresent) {
                    WriteObject(DesktopControlMutationVerifier.Verify(
                        automation,
                        Check ? "check" : "uncheck",
                        Control,
                        expectedCheckState: Check));
                }
            } catch (Exception ex) {
                if (!Verify.IsPresent && !PassThru.IsPresent) {
                    throw;
                }

                WriteWarning($"Failed to set check state on control '{Control.Text ?? Control.ClassName}': {ex.Message}");
                WriteObject(DesktopControlMutationVerifier.CreateFailureRecord(Check ? "check" : "uncheck", Control, ex.Message, Verify.IsPresent));
            }
        }
    }
}
