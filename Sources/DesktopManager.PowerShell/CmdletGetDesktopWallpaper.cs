namespace DesktopManager.PowerShell;

[Cmdlet(VerbsCommon.Get, "DesktopWallpaper")]
public sealed class CmdletGetDesktopWallpaper : PSCmdlet {
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

        // Check if parameters are set by the user
        bool? connectedOnly = MyInvocation.BoundParameters.ContainsKey(nameof(ConnectedOnly)) ? (bool?)ConnectedOnly : null;
        bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
        int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
        string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
        string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

        // Get monitors
        var getMonitors = monitors.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
        foreach (var monitor in getMonitors) {
            // Get wallpaper
            WriteObject(monitor.GetWallpaper());
        }
    }
}