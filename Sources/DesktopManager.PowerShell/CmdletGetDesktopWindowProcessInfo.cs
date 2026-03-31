using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets process information for a desktop window.</summary>
/// <para type="synopsis">Gets process information for a desktop window.</para>
/// <para type="description">Retrieves process metadata for a window, including process ID, thread ID, name, path, and elevation.</para>
/// <example>
///   <para>Get process info for a window</para>
///   <code>Get-DesktopWindow -Name "*Notepad*" | Get-DesktopWindowProcessInfo</code>
/// </example>
/// <example>
///   <para>Get owner process info for a window</para>
///   <code>Get-DesktopWindow -Name "*Notepad*" | Get-DesktopWindowProcessInfo -Owner</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopWindowProcessInfo")]
public sealed class CmdletGetDesktopWindowProcessInfo : PSCmdlet {
    /// <summary>
    /// <para type="description">Window to query.</para>
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
    public WindowInfo InputObject { get; set; }

    /// <summary>
    /// <para type="description">Return the owner window's process info instead of the window's own process.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Owner { get; set; }

    /// <summary>
    /// Retrieves process information for the specified window.
    /// </summary>
    protected override void ProcessRecord() {
        var automation = new DesktopAutomationService();
        WindowProcessInfo info = Owner.IsPresent
            ? automation.GetOwnerWindowProcessInfo(InputObject)
            : automation.GetWindowProcessInfo(InputObject);

        if (info == null) {
            WriteVerbose("Owner process information was not available.");
            return;
        }

        WriteObject(info);
    }
}
