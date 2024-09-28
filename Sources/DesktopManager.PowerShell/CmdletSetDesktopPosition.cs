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
        var getMonitors = monitors.GetMonitors(primaryOnly: PrimaryOnly, index: Index, deviceId: DeviceId, deviceName: DeviceName);
        foreach (var monitor in getMonitors) {
            monitor.SetMonitorPosition(Left, Top, Right, Bottom);
        }
    }
}