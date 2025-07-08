using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Moves or hides the taskbar for one or more monitors.</summary>
/// <para type="synopsis">Moves or hides the taskbar for one or more monitors.</para>
/// <para type="description">Allows changing taskbar position or visibility on specific monitors.</para>
/// <example>
/// <code>
/// Set-TaskbarPosition -PrimaryOnly -Position Top
/// </code>
/// </example>
[Cmdlet(VerbsCommon.Set, "TaskbarPosition", DefaultParameterSetName = "Index", SupportsShouldProcess = true)]
public sealed class CmdletSetTaskbarPosition : PSCmdlet {
    /// <summary>
    /// <para type="description">The index of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 0, ParameterSetName = "Index")]
    public int? Index;

    /// <summary>
    /// <para type="description">The device ID of the monitor.</para>
    /// </summary>
    [Alias("MonitorID")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "DeviceId")]
    public string DeviceId;

    /// <summary>
    /// <para type="description">The device name of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 2, ParameterSetName = "DeviceName")]
    public string DeviceName;

    /// <summary>
    /// <para type="description">Affects the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3, ParameterSetName = "PrimaryOnly")]
    public SwitchParameter PrimaryOnly;

    /// <summary>
    /// <para type="description">Affects all monitors.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 4, ParameterSetName = "All")]
    public SwitchParameter All;

    /// <summary>
    /// <para type="description">Desired taskbar position.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public TaskbarPosition? Position;

    /// <summary>
    /// <para type="description">Hide the taskbar.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public SwitchParameter Hide;

    /// <summary>
    /// <para type="description">Show the taskbar.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public SwitchParameter Show;

    private ActionPreference _errorAction;

    /// <summary>Begin processing.</summary>
    protected override void BeginProcessing() {
        _errorAction = CmdletHelper.GetErrorAction(this);
        if (!Position.HasValue && !Hide.IsPresent && !Show.IsPresent) {
            WriteWarning("No action specified. Use Position, Hide or Show.");
            return;
        }
        if (Hide.IsPresent && Show.IsPresent) {
            ThrowTerminatingError(new ErrorRecord(new ArgumentException("Specify only Hide or Show."), "ParameterConflict", ErrorCategory.InvalidArgument, null));
        }

        Monitors monitors = new Monitors();
        TaskbarService service = new TaskbarService();

        bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
        int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
        string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
        string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

        IEnumerable<Monitor> targets = All ? monitors.GetMonitors() :
            monitors.GetMonitors(connectedOnly: null, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);

        foreach (var monitor in targets) {
            if (ShouldProcess($"Monitor {monitor.DeviceName}", "Change taskbar")) {
                try {
                    if (Position.HasValue) {
                        service.SetTaskbarPosition(monitor.Index, Position.Value);
                    }
                    if (Hide.IsPresent) {
                        service.SetTaskbarVisibility(monitor.Index, false);
                    }
                    if (Show.IsPresent) {
                        service.SetTaskbarVisibility(monitor.Index, true);
                    }
                } catch (Exception ex) {
                    if (_errorAction == ActionPreference.Stop) { throw; }
                    WriteWarning($"Taskbar operation failed: {ex.Message}");
                }
            }
        }
    }
}
