namespace DesktopManager.PowerShell;

/// <summary>Gets Windows Core Audio endpoints.</summary>
/// <para type="description">Returns endpoint identity, direction, state, default roles, volume, and mute state.</para>
[Cmdlet(VerbsCommon.Get, "DesktopAudioEndpoint")]
[OutputType(typeof(AudioEndpointInfo))]
public sealed class CmdletGetDesktopAudioEndpoint : PSCmdlet {
    /// <summary><para type="description">Optional endpoint identifier.</para></summary>
    [Parameter(Position = 0)]
    public string DeviceId;

    /// <summary><para type="description">Endpoint direction to include.</para></summary>
    [Parameter]
    public AudioDataFlow DataFlow { get; set; } = AudioDataFlow.All;

    /// <summary><para type="description">Returns active endpoints only.</para></summary>
    [Parameter]
    public SwitchParameter ActiveOnly;

    /// <summary>Gets matching endpoints.</summary>
    protected override void BeginProcessing() {
        var service = new AudioService();
        if (!string.IsNullOrWhiteSpace(DeviceId)) {
            WriteObject(service.GetEndpoint(DeviceId));
            return;
        }

        WriteObject(service.GetEndpoints(
            DataFlow,
            ActiveOnly ? AudioEndpointState.Active : AudioEndpointState.All), true);
    }
}
