namespace DesktopManager.PowerShell;

[Cmdlet(VerbsCommon.Set, "DesktopPosition", DefaultParameterSetName = "Index")]
public sealed class CmdletSetDesktopPosition : PSCmdlet {
    [Parameter(Mandatory = false, Position = 0, ParameterSetName = "Index")]
    public int? Index;

    [Alias("MonitorID")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "Index")]
    public string DeviceId;

    [Parameter(Mandatory = false, Position = 2, ParameterSetName = "Index")]
    public string DeviceName;

    [Parameter(Mandatory = false, Position = 3, ParameterSetName = "Index")]
    public SwitchParameter ConnectedOnly;

    [Parameter(Mandatory = false, Position = 4, ParameterSetName = "Index")]
    public SwitchParameter? PrimaryOnly;

    [Parameter(Mandatory = true, Position = 5)]
    public int Left;
    [Parameter(Mandatory = true, Position = 6)]
    public int Top;
    [Parameter(Mandatory = true, Position = 7)]
    public int Right;
    [Parameter(Mandatory = true, Position = 8)]
    public int Bottom;

    protected override void BeginProcessing() {
        Monitors monitors = new Monitors();

        // Check if parameters are set by the user
        bool? connectedOnly = MyInvocation.BoundParameters.ContainsKey(nameof(ConnectedOnly)) ? (bool?)ConnectedOnly : null;
        bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
        int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
        string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
        string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

        // Get monitors
        var getMonitors = monitors.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName); foreach (var monitor in getMonitors) {
            monitor.SetMonitorPosition(Left, Top, Right, Bottom);
        }
    }
}