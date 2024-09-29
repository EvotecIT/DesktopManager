namespace DesktopManager.PowerShell;

/// <summary>
/// <para type="synopsis">Sets the position of the desktop for one or more monitors.</para>
/// <para type="description">Sets the position of the desktop for one or more monitors. You can specify the monitor by index, device ID, or device name. You can also set the position for all monitors or only the primary monitor.</para>
/// <example>
///  <para>Set the position for a specific monitor by index</para>
///  <para></para>
///  <code>Set-DesktopPosition -Index 1 -Left 0 -Top 0 -Right 1920 -Bottom 1080</code>
/// </example>
/// <example>
///  <para>Set the position for the primary monitor only</para>
///  <para></para>
///  <code>Set-DesktopPosition -PrimaryOnly -Left 0 -Top 0 -Right 1920 -Bottom 1080</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Set, "DesktopPosition", DefaultParameterSetName = "Index")]
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
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "Index")]
    public string DeviceId;

    /// <summary>
    /// <para type="description">The device name of the monitor to set the position for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 2, ParameterSetName = "Index")]
    public string DeviceName;

    /// <summary>
    /// <para type="description">Set the position for connected monitors only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3, ParameterSetName = "Index")]
    public SwitchParameter ConnectedOnly;

    /// <summary>
    /// <para type="description">Set the position for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 4, ParameterSetName = "Index")]
    public SwitchParameter? PrimaryOnly;

    /// <summary>
    /// <para type="description">The left position of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 5)]
    public int Left;

    /// <summary>
    /// <para type="description">The top position of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 6)]
    public int Top;

    /// <summary>
    /// <para type="description">The right position of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 7)]
    public int Right;

    /// <summary>
    /// <para type="description">The bottom position of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 8)]
    public int Bottom;

    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        Monitors monitors = new Monitors();

        // Check if parameters are set by the user
        bool? connectedOnly = MyInvocation.BoundParameters.ContainsKey(nameof(ConnectedOnly)) ? (bool?)ConnectedOnly : null;
        bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
        int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
        string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
        string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

        // Get monitors
        var getMonitors = monitors.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
        foreach (var monitor in getMonitors) {
            monitor.SetMonitorPosition(Left, Top, Right, Bottom);
        }
    }
}
