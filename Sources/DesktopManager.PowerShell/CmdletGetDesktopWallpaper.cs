namespace DesktopManager.PowerShell;

[Alias("Get-DesktopMonitors")]
[Cmdlet(VerbsCommon.Get, "DesktopMonitor")]
public sealed class CmdletGetDesktopMonitor : PSCmdlet {
    [Parameter(Mandatory = false, Position = 0)]
    public int Index;

    [Parameter(Mandatory = false, Position = 1)]
    public string DeviceId;

    [Parameter(Mandatory = false, Position = 2)]
    public string DeviceName;

    [Parameter(Mandatory = false, Position = 3)]
    public SwitchParameter ConnectedOnly;

    [Parameter(Mandatory = false, Position = 4)]
    public SwitchParameter PrimaryOnly;

    protected override void BeginProcessing() {
        Monitors monitors = new Monitors();
        var getMonitors = monitors.GetMonitors(connectedOnly: ConnectedOnly, primaryOnly: PrimaryOnly, index: Index, deviceId: DeviceId, deviceName: DeviceName);
        WriteObject(getMonitors);
    }
}