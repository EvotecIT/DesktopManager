using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets the brightness for one or more desktop monitors.</summary>
/// <para type="synopsis">Gets the brightness for one or more desktop monitors.</para>
/// <para type="description">Retrieves the current brightness level for one or more monitors. You can specify the monitor by index, device ID, or device name, or limit the query to connected or primary monitors.</para>
[Cmdlet(VerbsCommon.Get, "DesktopBrightness")]
public sealed class CmdletGetDesktopBrightness : PSCmdlet {
    /// <summary>
    /// <para type="description">The index of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 0)]
    public int Index;

    /// <summary>
    /// <para type="description">The device ID of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 1)]
    public string DeviceId;

    /// <summary>
    /// <para type="description">The device name of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 2)]
    public string DeviceName;

    /// <summary>
    /// <para type="description">Get brightness for connected monitors only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3)]
    public SwitchParameter ConnectedOnly;

    /// <summary>
    /// <para type="description">Get brightness for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 4)]
    public SwitchParameter PrimaryOnly;

    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        Monitors monitors = new Monitors();

        bool? connectedOnly = MyInvocation.BoundParameters.ContainsKey(nameof(ConnectedOnly)) ? (bool?)ConnectedOnly : null;
        bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
        int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
        string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
        string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

        var getMonitors = monitors.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
        foreach (var monitor in getMonitors) {
            WriteObject(monitors.GetMonitorBrightness(monitor.DeviceId));
        }
    }
}
