using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Sets the default audio device.</summary>
/// <para type="synopsis">Sets the default audio device.</para>
/// <para type="description">Sets the system default audio playback device using Core Audio APIs.</para>
[Cmdlet(VerbsCommon.Set, "DefaultAudioDevice", SupportsShouldProcess = true)]
public sealed class CmdletSetDefaultAudioDevice : PSCmdlet {
    /// <summary>
    /// <para type="description">Identifier of the audio device.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string DeviceId;

    /// <summary>
    /// Begin processing the cmdlet.
    /// </summary>
    protected override void BeginProcessing() {
        if (ShouldProcess($"Audio device {DeviceId}", "Set as default")) {
            AudioService service = new AudioService();
            service.SetDefaultAudioDevice(DeviceId);
        }
    }
}
