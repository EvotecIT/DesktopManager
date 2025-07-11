﻿namespace DesktopManager.PowerShell;

/// <summary>Sets the position of the desktop for one or more monitors.</summary>
/// <para type="synopsis">Sets the position of the desktop for one or more monitors.</para>
/// <para type="description">Sets the position of the desktop for one or more monitors. You can specify the monitor by index, device ID, or device name. You can also set the position for all monitors or only the primary monitor.</para>
/// <example>
/// <code>
/// <para>Set the position for a specific monitor by index</para>
/// 
/// Set-DesktopPosition -Index 1 -Left 0 -Top 0 -Right 1920 -Bottom 1080
/// </code>
/// </example>
/// <example>
/// <code>
/// <para>Set the position for the primary monitor only</para>
/// 
/// Set-DesktopPosition -PrimaryOnly -Left 0 -Top 0 -Right 1920 -Bottom 1080
/// </code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopPosition", DefaultParameterSetName = "Index", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopPosition : PSCmdlet {
    /// <summary>
    /// <para type="description">The index of the monitor to set the position for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 0, ParameterSetName = "Index")]
    public int? Index;

    /// <summary>
    /// <para type="description">The device ID of the monitor to set the position for.</para>
    /// </summary>
    [Alias("MonitorID")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "DeviceID")]
    public string DeviceId;

    /// <summary>
    /// <para type="description">The device name of the monitor to set the position for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 2, ParameterSetName = "DeviceName")]
    public string DeviceName;

    /// <summary>
    /// <para type="description">Set the position for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3, ParameterSetName = "PrimaryOnly")]
    public SwitchParameter PrimaryOnly;

    /// <summary>
    /// <para type="description">The left position of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 4)]
    public int Left;

    /// <summary>
    /// <para type="description">The top position of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 5)]
    public int Top;

    /// <summary>
    /// <para type="description">The right position of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 6)]
    public int Right;

    /// <summary>
    /// <para type="description">The bottom position of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 7)]
    public int Bottom;

    /// <summary>
    /// Error action preference, as set by the user
    /// </summary>
    private ActionPreference ErrorAction;

    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        ErrorAction = CmdletHelper.GetErrorAction(this);

        Monitors monitors = new Monitors();

        // Check if parameters are set by the user
        bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
        int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
        string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
        string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

        // Get monitors
        var getMonitors = monitors.GetMonitors(connectedOnly: null, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
        foreach (var monitor in getMonitors) {
            var currentPosition = monitor.GetMonitorPosition();
            var newPosition = new MonitorPosition(Left, Top, Right, Bottom);
            if (ShouldProcess(
                    $"Monitor {monitor.DeviceName}",
                    $"Change position from Left: {currentPosition.Left}, Top: {currentPosition.Top}, Right: {currentPosition.Right}, Bottom: {currentPosition.Bottom} " +
                    $"to Left: {newPosition.Left}, Top: {newPosition.Top}, Right: {newPosition.Right}, Bottom: {newPosition.Bottom}")) {
                try {
                    monitor.SetMonitorPosition(newPosition);
                } catch (Exception ex) {
                    if (ErrorAction == ActionPreference.Stop) { throw; }
                    WriteWarning($"Error setting monitor position: {ex.Message}");
                }
            }
        }
    }
}
