using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Starts a desktop process and waits for a correlated final window.</summary>
/// <para type="synopsis">Starts a desktop process and waits for a correlated final window.</para>
/// <para type="description">Launches a desktop process, correlates the initial launch window when possible, and then waits for a final matching window using DesktopManager core workflow logic.</para>
/// <example>
///   <summary>Launch Notepad and wait for its window</summary>
///   <code>Start-DesktopProcessAndWait -Path notepad.exe -TimeoutMilliseconds 5000</code>
/// </example>
[Cmdlet(VerbsLifecycle.Start, "DesktopProcessAndWait", SupportsShouldProcess = true)]
public sealed class CmdletStartDesktopProcessAndWait : PSCmdlet {
    /// <summary>
    /// <para type="description">Executable path or shell command to launch.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; }

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
    /// <para type="description">Optional launch-time window correlation wait in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int? LaunchWaitForWindowMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Polling interval while correlating the launch-time window.</para>
    /// </summary>
    [Parameter]
    public int? LaunchWaitForWindowIntervalMilliseconds { get; set; }

    /// <summary>
    /// <para type="description">Optional launch-time window title filter.</para>
    /// </summary>
    [Parameter]
    public string LaunchWindowTitle { get; set; }

    /// <summary>
    /// <para type="description">Optional launch-time window class filter.</para>
    /// </summary>
    [Parameter]
    public string LaunchWindowClassName { get; set; }

    /// <summary>
    /// <para type="description">Optional final window title filter.</para>
    /// </summary>
    [Parameter]
    public string WindowTitle { get; set; }

    /// <summary>
    /// <para type="description">Optional final window class filter.</para>
    /// </summary>
    [Parameter]
    public string WindowClassName { get; set; }

    /// <summary>
    /// <para type="description">Include hidden windows during the final wait.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeHidden { get; set; }

    /// <summary>
    /// <para type="description">Include windows with empty titles during the final wait.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeEmptyTitles { get; set; }

    /// <summary>
    /// <para type="description">Return all matching windows instead of the first match.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter All { get; set; }

    /// <summary>
    /// <para type="description">Allow the final wait to follow the launched app's same-name process family when no resolved process window is available.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter FollowProcessFamily { get; set; }

    /// <summary>
    /// <para type="description">Maximum final wait time in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int TimeoutMilliseconds { get; set; } = 10000;

    /// <summary>
    /// <para type="description">Polling interval used during the final wait in milliseconds.</para>
    /// </summary>
    [Parameter]
    public int IntervalMilliseconds { get; set; } = 200;

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (!ShouldProcess(Path, "Start desktop process and wait for window")) {
            return;
        }

        WriteObject(new DesktopAutomationService().LaunchAndWaitForWindow(new DesktopProcessLaunchAndWaitOptions {
            FilePath = Path,
            Arguments = ArgumentList,
            WorkingDirectory = WorkingDirectory,
            WaitForInputIdleMilliseconds = WaitForInputIdleMilliseconds,
            LaunchWaitForWindowMilliseconds = LaunchWaitForWindowMilliseconds,
            LaunchWaitForWindowIntervalMilliseconds = LaunchWaitForWindowIntervalMilliseconds,
            LaunchWindowTitlePattern = LaunchWindowTitle,
            LaunchWindowClassNamePattern = LaunchWindowClassName,
            WindowTitlePattern = WindowTitle,
            WindowClassNamePattern = WindowClassName,
            IncludeHidden = IncludeHidden,
            IncludeEmptyTitles = IncludeEmptyTitles,
            All = All,
            FollowProcessFamily = FollowProcessFamily,
            TimeoutMilliseconds = TimeoutMilliseconds,
            IntervalMilliseconds = IntervalMilliseconds
        }));
    }
}
