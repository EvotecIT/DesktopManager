using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Sets the brightness level of desktop monitors.</summary>
/// <para type="synopsis">Sets the brightness level of desktop monitors.</para>
/// <para type="description">Changes the brightness level for one or more monitors identified by index, device ID or device name.</para>
[Cmdlet(VerbsCommon.Set, "DesktopBrightness", DefaultParameterSetName = "Index", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopBrightness : PSCmdlet {
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
    /// <para type="description">Set brightness for the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 3, ParameterSetName = "PrimaryOnly")]
    public SwitchParameter? PrimaryOnly;

    /// <summary>
    /// <para type="description">Brightness level from 0 to 100.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 4)]
    public int Brightness;

    private ActionPreference ErrorAction;

    protected override void BeginProcessing() {
        ErrorAction = CmdletHelper.GetErrorAction(this);
        Monitors monitors = new Monitors();

        bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
        int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
        string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
        string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

        var list = monitors.GetMonitors(primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
        foreach (var monitor in list) {
            if (ShouldProcess($"Monitor {monitor.DeviceName}", $"Set brightness to {Brightness}")) {
                try {
                    monitors.SetMonitorBrightness(monitor.DeviceId, Brightness);
                } catch (Exception ex) {
                    if (ErrorAction == ActionPreference.Stop) { throw; }
                    WriteWarning($"Failed to set brightness for {monitor.DeviceName}: {ex.Message}");
                }
            }
        }
    }
}
