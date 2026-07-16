namespace DesktopManager.PowerShell;

/// <summary>Sets master volume or mute for a Windows audio endpoint.</summary>
/// <para type="description">Applies only explicitly bound settings and can return the resulting endpoint.</para>
[Cmdlet(VerbsCommon.Set, "DesktopAudioEndpoint", SupportsShouldProcess = true)]
[OutputType(typeof(AudioEndpointInfo))]
public sealed class CmdletSetDesktopAudioEndpoint : PSCmdlet {
    /// <summary><para type="description">The stable Windows endpoint identifier.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string DeviceId;

    /// <summary><para type="description">Master volume from 0 through 100.</para></summary>
    [Parameter]
    [ValidateRange(0, 100)]
    public float Volume;

    /// <summary><para type="description">The explicit master mute state.</para></summary>
    [Parameter]
    public bool Muted;

    /// <summary><para type="description">Returns the resulting endpoint snapshot.</para></summary>
    [Parameter]
    public SwitchParameter PassThru;

    /// <summary>Applies bound endpoint settings.</summary>
    protected override void BeginProcessing() {
        bool hasVolume = MyInvocation.BoundParameters.ContainsKey(nameof(Volume));
        bool hasMuted = MyInvocation.BoundParameters.ContainsKey(nameof(Muted));
        if (!hasVolume && !hasMuted) {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException("Specify Volume or Muted."),
                "MissingAudioMutation",
                ErrorCategory.InvalidArgument,
                DeviceId));
        }

        if (!ShouldProcess(DeviceId, "Set audio endpoint state")) {
            return;
        }
        var service = new AudioService();
        if (hasVolume) {
            service.SetEndpointVolume(DeviceId, Volume);
        }
        if (hasMuted) {
            service.SetEndpointMute(DeviceId, Muted);
        }
        if (PassThru) {
            WriteObject(service.GetEndpoint(DeviceId));
        }
    }
}
