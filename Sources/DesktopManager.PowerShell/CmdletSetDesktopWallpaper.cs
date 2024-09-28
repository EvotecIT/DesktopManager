using System.IO;

namespace DesktopManager.PowerShell;

/// <summary>
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
/// </summary>
[Cmdlet(VerbsCommon.Set, "DesktopWallpaper", DefaultParameterSetName = "Index")]
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
    /// <para type="description">Set the wallpaper for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3, ParameterSetName = "Index")]
    public SwitchParameter? PrimaryOnly;

    /// <summary>
    /// <para type="description">Set the wallpaper for all monitors.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 4, ParameterSetName = "All")]
    public SwitchParameter All;

    /// <summary>
    /// <para type="description">The position of the wallpaper on the monitor.</para>
    /// </summary>
    [Alias("Position")]
    [Parameter(Mandatory = false, Position = 5)]
    public DesktopWallpaperPosition? WallpaperPosition;

    /// <summary>
    /// <para type="description">The file path of the wallpaper image.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 6)]
    public string WallpaperPath;

    private ActionPreference ErrorAction;

    /// <summary>
    /// Begin processing the command
    /// </summary>
    /// <exception cref="FileNotFoundException"></exception>
    protected override void BeginProcessing() {
        ErrorAction = GetErrorAction();
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
            WriteWarning($"The wallpaper file path does not exists {WallpaperPath}.");
            return;
        }

        Monitors monitors = new Monitors();
        if (All) {
            var getMonitors = monitors.GetMonitors();
            foreach (var monitor in getMonitors) {
                monitors.SetWallpaper(monitor.DeviceId, WallpaperPath);
            }
        } else {
            var getMonitors = monitors.GetMonitors(primaryOnly: PrimaryOnly, index: Index, deviceId: DeviceId, deviceName: DeviceName);
            foreach (var monitor in getMonitors) {
                monitors.SetWallpaper(monitor.DeviceId, WallpaperPath);
            }
        }
        if (WallpaperPosition != null) {
            monitors.SetWallpaperPosition(WallpaperPosition.Value);
        }
    }

    /// <summary>
    /// Get the error action preference
    /// </summary>
    /// <returns></returns>
    private ActionPreference GetErrorAction() {
        // Get the error action preference as user requested
        // It first sets the error action to the default error action preference
        // If the user has specified the error action, it will set the error action to the user specified error action
        ActionPreference errorAction = (ActionPreference)this.SessionState.PSVariable.GetValue("ErrorActionPreference");
        if (this.MyInvocation.BoundParameters.ContainsKey("ErrorAction")) {
            string errorActionString = this.MyInvocation.BoundParameters["ErrorAction"].ToString();
            if (Enum.TryParse(errorActionString, true, out ActionPreference actionPreference)) {
                errorAction = actionPreference;
            }
        }
        return errorAction;
    }
}