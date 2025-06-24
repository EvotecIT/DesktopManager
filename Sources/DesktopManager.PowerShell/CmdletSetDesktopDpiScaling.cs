using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Sets DPI scaling for one or more desktop monitors.</summary>
/// <para type="synopsis">Sets DPI scaling for one or more desktop monitors.</para>
/// <para type="description">Changes the DPI scaling level for monitors. You can target monitors by index, device ID or name, or limit the action to the primary monitor.</para>
[Cmdlet(VerbsCommon.Set, "DesktopDpiScaling", DefaultParameterSetName = "Index", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopDpiScaling : PSCmdlet {
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
    /// <para type="description">Set scaling for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3, ParameterSetName = "PrimaryOnly")]
    public SwitchParameter PrimaryOnly;

    /// <summary>
    /// <para type="description">Scaling percentage to set.</para>
    /// </summary>
    [ValidateRange(100, 500)]
    [Parameter(Mandatory = true, Position = 4)]
    public int Scaling;

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
            if (ShouldProcess($"Monitor {monitor.DeviceName}", $"Set DPI scaling to {Scaling}%")) {
                try {
                    monitors.SetMonitorDpiScaling(monitor.DeviceId, Scaling);
                } catch (Exception ex) {
                    if (ErrorAction == ActionPreference.Stop) { throw; }
                    WriteWarning($"Failed to set DPI scaling for {monitor.DeviceName}: {ex.Message}");
                }
            }
        }
    }
}
