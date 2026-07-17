using System.Threading;

namespace DesktopManager.PowerShell;

/// <summary>Connects an exact saved Windows Wi-Fi profile without scanning nearby networks.</summary>
/// <para>The command waits for exclusive access and a Windows WLAN Auto Configuration completion notification. Cancelling or timing out stops the wait but does not cancel an attempt already accepted by Windows, so a later same-process call waits for that attempt to finish before starting another one. If Windows never reports completion, the library releases the retained notification handle after two minutes and requires restarting the hosting process before another connection attempt.</para>
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
    private readonly object _cancellationSync = new();
    private CancellationTokenSource _cancellation;

    /// <summary><para type="description">The case-sensitive saved Windows Wi-Fi profile name.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name;

    /// <summary><para type="description">Optional interface identifier used when the profile exists on multiple wireless LAN adapters.</para></summary>
    [Parameter]
    public Guid? InterfaceId;

    /// <summary><para type="description">How long to wait for exclusive access and a Windows connection completion notification. The default is 30 seconds and the maximum is 2147483647 milliseconds.</para></summary>
    [Parameter]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Connects the saved profile and emits its observed result.</summary>
    protected override void BeginProcessing() {
        if (!ShouldProcess(Name, "Connect saved Wi-Fi profile")) {
            return;
        }

        using var cancellation = new CancellationTokenSource();
        lock (_cancellationSync) {
            _cancellation = cancellation;
        }
        try {
            using var service = new WifiProfileService();
            DesktopWifiConnectionResult result = service.ConnectProfileAsync(
                Name,
                InterfaceId,
                Timeout,
                cancellation.Token).GetAwaiter().GetResult();
            if (!result.Succeeded) {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException(result.BuildFailureMessage()),
                    "DesktopWifiProfileConnectionFailed",
                    ErrorCategory.ConnectionError,
                    result));
            }

            WriteObject(result);
        } finally {
            lock (_cancellationSync) {
                _cancellation = null;
            }
        }
    }

    /// <summary>Stops waiting for the active Windows connection attempt.</summary>
    protected override void StopProcessing() {
        lock (_cancellationSync) {
            _cancellation?.Cancel();
        }
        base.StopProcessing();
    }
}
