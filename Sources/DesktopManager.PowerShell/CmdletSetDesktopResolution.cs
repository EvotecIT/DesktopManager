using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Sets the resolution of a desktop monitor.</summary>
/// <para type="synopsis">Sets the resolution of a desktop monitor.</para>
/// <para type="description">Allows changing the resolution and orientation of a monitor identified by index or device ID.</para>
[Cmdlet(VerbsCommon.Set, "DesktopResolution", DefaultParameterSetName = "Index", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopResolution : PSCmdlet {
    /// <summary>
    /// <para type="description">The index of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 0, ParameterSetName = "Index")]
    public int? Index;

    /// <summary>
    /// <para type="description">The device ID of the monitor.</para>
    /// </summary>
    [Alias("MonitorID")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "DeviceID")]
    public string DeviceId;

    /// <summary>
    /// <para type="description">The device name of the monitor.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 2, ParameterSetName = "DeviceName")]
    public string DeviceName;

    /// <summary>
    /// <para type="description">Set resolution for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3, ParameterSetName = "PrimaryOnly")]
    public SwitchParameter? PrimaryOnly;

    /// <summary>
    /// <para type="description">Resolution width.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 4)]
    public int Width;

    /// <summary>
    /// <para type="description">Resolution height.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 5)]
    public int Height;

    /// <summary>
    /// <para type="description">Optional display orientation.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 6)]
    public DisplayOrientation? Orientation;

    private ActionPreference ErrorAction;

    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        ErrorAction = CmdletHelper.GetErrorAction(this);
        Monitors monitors = new Monitors();

        bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
        int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
        string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
        string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

        var getMonitors = monitors.GetMonitors(connectedOnly: null, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
        foreach (var monitor in getMonitors) {
            var action = $"Set resolution to {Width}x{Height}" + (Orientation != null ? $" orientation {Orientation}" : string.Empty);
            if (ShouldProcess($"Monitor {monitor.DeviceName}", action)) {
                try {
                    monitors.SetMonitorResolution(monitor.DeviceId, Width, Height);
                    if (Orientation != null) {
                        monitors.SetMonitorOrientation(monitor.DeviceId, Orientation.Value);
                    }
                } catch (Exception ex) {
                    if (ErrorAction == ActionPreference.Stop) { throw; }
                    WriteWarning($"Failed to set resolution for {monitor.DeviceName}: {ex.Message}");
                }
            }
        }
    }
}
