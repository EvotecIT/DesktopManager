using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Starts a desktop application or process.</summary>
/// <para type="synopsis">Starts a desktop application or process.</para>
/// <para type="description">Launches a desktop process and returns the launch metadata, including the launched process identifier and any correlated main window.</para>
/// <example>
///   <summary>Start Notepad</summary>
///   <code>Start-DesktopProcess -Path notepad.exe</code>
/// </example>
/// <example>
///   <summary>Launch and require a user-facing window</summary>
///   <code>Start-DesktopProcess -Path notepad.exe -RequireWindow -WaitForWindowMilliseconds 3000</code>
/// </example>
[Cmdlet(VerbsLifecycle.Start, "DesktopProcess", SupportsShouldProcess = true)]
public sealed class CmdletStartDesktopProcess : PSCmdlet {
    /// <summary>
    /// <para type="description">Executable path or shell command to launch.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Optional argument string passed to the launched process.</para>
    /// </summary>
    [Parameter]
    public string ArgumentList { get; set; }

    /// <summary>
    /// <para type="description">Optional working directory for the launched process.</para>
    /// </summary>
    [Parameter]
    public string WorkingDirectory { get; set; }

    /// <summary>
    /// <para type="description">Optional time to wait for UI input idle in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int? WaitForInputIdleMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Optional time to wait for a launched window in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int? WaitForWindowMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Polling interval while waiting for a launched window.</para>
    /// </summary>
    [Parameter]
    public int? WaitForWindowIntervalMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Optional launched-window title filter.</para>
    /// </summary>
    [Parameter]
    public string WindowTitle { get; set; }

    /// <summary>
    /// <para type="description">Optional launched-window class filter.</para>
    /// </summary>
    [Parameter]
    public string WindowClassName { get; set; }

    /// <summary>
    /// <para type="description">Require a user-facing launched window before returning.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter RequireWindow { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (!ShouldProcess(Path, "Start desktop process")) {
            return;
        }

        WriteObject(new DesktopAutomationService().LaunchProcess(new DesktopProcessStartOptions {
            FilePath = Path,
            Arguments = ArgumentList,
            WorkingDirectory = WorkingDirectory,
            WaitForInputIdleMilliseconds = WaitForInputIdleMilliseconds,
            WaitForWindowMilliseconds = WaitForWindowMilliseconds,
            WaitForWindowIntervalMilliseconds = WaitForWindowIntervalMilliseconds,
            WindowTitlePattern = WindowTitle,
            WindowClassNamePattern = WindowClassName,
            RequireWindow = RequireWindow
        }));
    }
}
