using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Stops the process that owns a desktop window.</summary>
/// <para type="synopsis">Stops the process that owns a desktop window.</para>
/// <para type="description">Terminates the process associated with a resolved desktop window and optionally returns the termination result from the DesktopManager automation core.</para>
/// <example>
///   <summary>Stop the process that owns the active window</summary>
///   <code>Get-DesktopWindow -ActiveWindow | Stop-DesktopWindowProcess</code>
/// </example>
[Cmdlet(VerbsLifecycle.Stop, "DesktopWindowProcess", SupportsShouldProcess = true)]
public sealed class CmdletStopDesktopWindowProcess : PSCmdlet {
    /// <summary>
    /// <para type="description">Window whose owning process should be terminated.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public WindowInfo InputObject { get; set; } = null!;

    /// <summary>
    /// <para type="description">Whether to terminate the full process tree.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter EntireProcessTree { get; set; }

    /// <summary>
    /// <para type="description">How long to wait for the process to exit in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int WaitForExitMilliseconds { get; set; } = 5000;

    /// <summary>
    /// <para type="description">Return the termination result.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        string target = string.IsNullOrWhiteSpace(InputObject.Title)
            ? $"PID {InputObject.ProcessId}"
            : $"{InputObject.Title} (PID {InputObject.ProcessId})";
        if (!ShouldProcess(target, "Terminate window process")) {
            return;
        }

        DesktopProcessTerminationResult result = new DesktopAutomationService().TerminateWindowProcess(
            InputObject,
            EntireProcessTree,
            WaitForExitMilliseconds);
        if (PassThru.IsPresent) {
            WriteObject(result);
        }
    }
}
