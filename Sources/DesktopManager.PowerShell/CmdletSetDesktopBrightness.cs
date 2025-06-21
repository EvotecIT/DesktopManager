using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Sets the brightness for one or more desktop monitors.</summary>
/// <para type="synopsis">Sets the brightness for one or more desktop monitors.</para>
/// <para type="description">Changes the brightness level for one or more monitors. You can target monitors by index, device ID or name, or limit the action to the primary monitor.</para>
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
    public SwitchParameter PrimaryOnly;

    /// <summary>
    /// <para type="description">Brightness level to set.</para>
    /// </summary>
    [ValidateRange(0, 100)]
    [Parameter(Mandatory = true, Position = 4)]
    public int Brightness;

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

        if (index != null && (deviceId != null || deviceName != null)) {
            var ex = new ArgumentException("-Index cannot be combined with -DeviceId or -DeviceName.");
            ThrowTerminatingError(new ErrorRecord(ex, "ParameterConflict", ErrorCategory.InvalidArgument, null));
        }

        var getMonitors = monitors.GetMonitors(connectedOnly: null, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
        foreach (var monitor in getMonitors) {
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
