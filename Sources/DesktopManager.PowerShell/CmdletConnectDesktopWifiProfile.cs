namespace DesktopManager.PowerShell;

/// <summary>Connects an exact saved Windows Wi-Fi profile without scanning nearby networks.</summary>
/// <para>The command waits for a Windows WLAN Auto Configuration completion notification. Cancelling or timing out stops the wait but does not cancel an attempt already accepted by Windows.</para>
/// <example>
///   <summary>Connect a saved Wi-Fi profile</summary>
///   <prefix>PS&gt; </prefix>
///   <code>Connect-DesktopWifiProfile -Name 'Corporate WiFi'</code>
///   <para>Connects the exact saved profile when it exists on one wireless LAN interface.</para>
/// </example>
[Cmdlet(VerbsCommunications.Connect, "DesktopWifiProfile", SupportsShouldProcess = true)]
[OutputType(typeof(DesktopWifiConnectionResult))]
[System.Runtime.Versioning.SupportedOSPlatform("windows6.0.6000.0")]
public sealed class CmdletConnectDesktopWifiProfile : PSCmdlet {
    /// <summary><para type="description">The case-sensitive saved Windows Wi-Fi profile name.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name;

    /// <summary><para type="description">Optional interface identifier used when the profile exists on multiple wireless LAN adapters.</para></summary>
    [Parameter]
    public Guid? InterfaceId;

    /// <summary><para type="description">How long to wait for a Windows connection completion notification.</para></summary>
    [Parameter]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Connects the saved profile and emits its observed result.</summary>
    protected override void BeginProcessing() {
        if (!ShouldProcess(Name, "Connect saved Wi-Fi profile")) {
            return;
        }

        using var service = new WifiProfileService();
        DesktopWifiConnectionResult result = service.ConnectProfileAsync(Name, InterfaceId, Timeout).GetAwaiter().GetResult();
        if (!result.Succeeded) {
            ThrowTerminatingError(new ErrorRecord(
                new InvalidOperationException(result.BuildFailureMessage()),
                "DesktopWifiProfileConnectionFailed",
                ErrorCategory.ConnectionError,
                result));
        }

        WriteObject(result);
    }
}
