namespace DesktopManager.PowerShell;

/// <summary>
/// <para type="synopsis">Gets the current desktop wallpaper for one or more monitors.</para>
/// <para type="description">Retrieves the current desktop wallpaper for one or more monitors. You can specify the monitor by index, device ID, or device name. You can also get the wallpaper for all monitors or only the primary monitor.</para>
/// <example>
///  <para>Get the wallpaper for all monitors</para>
///  <para></para>
///  <code>Get-DesktopWallpaper</code>
/// </example>
/// <example>
///  <para>Get the wallpaper for a specific monitor by index</para>
///  <para></para>
///  <code>Get-DesktopWallpaper -Index 1</code>
/// </example>
/// <example>
///  <para>Get the wallpaper for the primary monitor only</para>
///  <para></para>
///  <code>Get-DesktopWallpaper -PrimaryOnly</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "DesktopWallpaper")]
public sealed class CmdletGetDesktopWallpaper : PSCmdlet {
    /// <summary>
    /// <para type="description">The index of the monitor to get the wallpaper for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 0)]
    public int Index;

    /// <summary>
    /// <para type="description">The device ID of the monitor to get the wallpaper for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 1)]
    public string DeviceId;

    /// <summary>
    /// <para type="description">The device name of the monitor to get the wallpaper for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 2)]
    public string DeviceName;

    /// <summary>
    /// <para type="description">Get the wallpaper for connected monitors only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3)]
    public SwitchParameter ConnectedOnly;

    /// <summary>
    /// <para type="description">Get the wallpaper for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 4)]
    public SwitchParameter PrimaryOnly;

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
            // Get wallpaper
            WriteObject(monitor.GetWallpaper());
        }
    }
}
