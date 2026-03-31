using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets the current desktop mouse state.</summary>
/// <para type="synopsis">Gets the current desktop mouse state.</para>
/// <para type="description">Returns the current cursor position, button state, and cursor visibility from the DesktopManager automation core.</para>
/// <example>
///   <summary>Read the current mouse state</summary>
///   <code>Get-DesktopMouseState</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopMouseState")]
public sealed class CmdletGetDesktopMouseState : PSCmdlet {
    /// <inheritdoc />
    protected override void BeginProcessing() {
        WriteObject(new DesktopAutomationService().GetMouseState());
    }
}
