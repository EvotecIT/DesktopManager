using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Tests whether the current DesktopManager host process is elevated.</summary>
/// <para type="synopsis">Tests whether the current DesktopManager host process is elevated.</para>
/// <para type="description">Returns <c>true</c> when the current DesktopManager host is running with administrative privileges; otherwise returns <c>false</c>.</para>
/// <example>
///   <summary>Check whether the current host is elevated</summary>
///   <code>Test-DesktopElevation</code>
/// </example>
[Cmdlet(VerbsDiagnostic.Test, "DesktopElevation")]
public sealed class CmdletTestDesktopElevation : PSCmdlet {
    /// <inheritdoc />
    protected override void BeginProcessing() {
        WriteObject(new DesktopAutomationService().IsElevated());
    }
}
