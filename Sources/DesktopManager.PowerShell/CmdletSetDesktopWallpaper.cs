using System.IO;

namespace DesktopManager.PowerShell;

/// <summary>Sets the desktop wallpaper for one or more monitors.</summary>
/// <para type="synopsis">Sets the desktop wallpaper for one or more monitors.</para>
/// <para type="description">Sets the desktop wallpaper for one or more monitors. You can specify the monitor by index, device ID, or device name. You can also set the wallpaper for all monitors or only the primary monitor. Optionally, you can specify the wallpaper position.</para>
/// <example>
///  <para>Set the wallpaper for all monitors</para>
///  <para></para>
///  <code>Set-DesktopWallpaper -All -WallpaperPath "C:\Path\To\Wallpaper.jpg"</code>
/// </example>
/// <example>
///  <para>Set the wallpaper for a specific monitor by index</para>
///  <para></para>
///  <code>Set-DesktopWallpaper -Index 1 -WallpaperPath "C:\Path\To\Wallpaper.jpg"</code>
/// </example>
/// <example>
///  <para>Set the wallpaper for the primary monitor only</para>
///  <para></para>
///  <code>Set-DesktopWallpaper -PrimaryOnly -WallpaperPath "C:\Path\To\Wallpaper.jpg"</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopWallpaper", DefaultParameterSetName = "Index", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopWallpaper : PSCmdlet {
    /// <summary>
    /// <para type="description">The index of the monitor to set the wallpaper for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 0, ParameterSetName = "Index")]
    public int? Index;

    /// <summary>
    /// <para type="description">The device ID of the monitor to set the wallpaper for.</para>
    /// </summary>
    [Alias("MonitorID")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "Index")]
    public string DeviceId;

    /// <summary>
    /// <para type="description">The device name of the monitor to set the wallpaper for.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 2, ParameterSetName = "Index")]
    public string DeviceName;

    /// <summary>
    /// <para type="description">Set the wallpaper for connected monitors only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3, ParameterSetName = "Index")]
    public SwitchParameter ConnectedOnly;

    /// <summary>
    /// <para type="description">Set the wallpaper for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 4, ParameterSetName = "Index")]
    public SwitchParameter? PrimaryOnly;

    /// <summary>
    /// <para type="description">Set the wallpaper for all monitors.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 5, ParameterSetName = "All")]
    public SwitchParameter All;

    /// <summary>
    /// <para type="description">The position of the wallpaper on the monitor.</para>
    /// </summary>
    [Alias("Position")]
    [Parameter(Mandatory = false, Position = 6)]
    public DesktopWallpaperPosition? WallpaperPosition;

    /// <summary>
    /// <para type="description">The file path of the wallpaper image.</para>
    /// </summary>
    [Alias("FilePath", "Path")]
    [Parameter(Mandatory = true, Position = 7)]
    public string WallpaperPath;
    /// <summary>
    /// Error action preference, as set by the user
    /// </summary>
    private ActionPreference ErrorAction;

    /// <summary>
    /// <para type="description">Begin processing the command.</para>
    /// </summary>
    /// <exception cref="FileNotFoundException"></exception>
    protected override void BeginProcessing() {
        ErrorAction = CmdletHelper.GetErrorAction(this);
        if (string.IsNullOrEmpty(WallpaperPath)) {
            if (ErrorAction == ActionPreference.Stop) {
                throw new FileNotFoundException("The wallpaper file path is required.", WallpaperPath);
            }
            WriteWarning("The wallpaper file path is required.");
            return;
        }
        if (!File.Exists(WallpaperPath)) {
            if (ErrorAction == ActionPreference.Stop) {
                throw new FileNotFoundException("The wallpaper file path does not exist.", WallpaperPath);
            }
            WriteWarning($"The wallpaper file path does not exist: {WallpaperPath}.");
            return;
        }

        // Check if parameters are set by the user
        bool? connectedOnly = MyInvocation.BoundParameters.ContainsKey(nameof(ConnectedOnly)) ? (bool?)ConnectedOnly : null;
        bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
        int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
        string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
        string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

        Monitors monitors = new Monitors();
        if (All) {
            var getMonitors = monitors.GetMonitors();
            foreach (var monitor in getMonitors) {
                if (ShouldProcess($"Monitor {monitor.DeviceName}", $"Set wallpaper to {WallpaperPath}")) {
                    monitors.SetWallpaper(monitor.DeviceId, WallpaperPath);
                }
            }
        } else {
            // Get monitors
            var getMonitors = monitors.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
            foreach (var monitor in getMonitors) {
                if (ShouldProcess($"Monitor {monitor.DeviceName}", $"Set wallpaper to {WallpaperPath}")) {
                    monitors.SetWallpaper(monitor.DeviceId, WallpaperPath);
                }
            }
        }
        if (WallpaperPosition != null) {
            if (ShouldProcess("All monitors", $"Set wallpaper position to {WallpaperPosition.Value}")) {
                monitors.SetWallpaperPosition(WallpaperPosition.Value);
            }
        }
    }
}
