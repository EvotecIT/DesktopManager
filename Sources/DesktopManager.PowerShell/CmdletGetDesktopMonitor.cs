namespace DesktopManager.PowerShell;

/// <summary>
/// <para type="synopsis">Gets the desktop monitors information.</para>
/// <para type="description">Retrieves information about the desktop monitors connected to the system. You can filter the monitors by index, device ID, device name, connection status, or primary monitor status.</para>
/// <example>
///  <para>Get information for all monitors</para>
///  <para>Retrieves information for all connected desktop monitors.</para>
///  <code>Get-DesktopMonitor</code>
/// </example>
/// <example>
///  <para>Get information for a specific monitor by index</para>
///  <para>Retrieves information for the monitor specified by the index.</para>
///  <code>Get-DesktopMonitor -Index 1</code>
/// </example>
/// <example>
///  <para>Get information for connected monitors only</para>
///  <para>Retrieves information for all connected monitors only.</para>
///  <code>Get-DesktopMonitor -ConnectedOnly</code>
/// </example>
/// <example>
///  <para>Get information for the primary monitor only</para>
///  <para>Retrieves information for the primary monitor only.</para>
///  <code>Get-DesktopMonitor -PrimaryOnly</code>
/// </example>
/// </summary>
[Alias("Get-DesktopMonitors")]
[Cmdlet(VerbsCommon.Get, "DesktopMonitor")]
public sealed class CmdletGetDesktopMonitor : PSCmdlet {
    /// <summary>
    /// <para type="description">The index of the monitor to get information for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 0)]
    public int Index;

    /// <summary>
    /// <para type="description">The device ID of the monitor to get information for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 1)]
    public string DeviceId;

    /// <summary>
    /// <para type="description">The device name of the monitor to get information for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 2)]
    public string DeviceName;

    /// <summary>
    /// <para type="description">Get information for connected monitors only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3)]
    public SwitchParameter ConnectedOnly;

    /// <summary>
    /// <para type="description">Get information for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 4)]
    public SwitchParameter PrimaryOnly;

    /// <summary>
    /// <para type="description">Begin processing the command.</para>
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
        // Write monitors
        WriteObject(getMonitors);
    }
}
