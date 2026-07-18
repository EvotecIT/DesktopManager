namespace DesktopManager.PowerShell;

/// <summary>Gets saved Windows Wi-Fi profiles without scanning nearby networks.</summary>
/// <para>Profile XML and credentials are never returned.</para>
/// <example>
///   <summary>List saved Wi-Fi profiles</summary>
///   <prefix>PS&gt; </prefix>
///   <code>Get-DesktopWifiProfile</code>
///   <para>Returns profiles already stored by Windows on every wireless LAN interface.</para>
/// </example>
[Cmdlet(VerbsCommon.Get, "DesktopWifiProfile")]
[OutputType(typeof(DesktopWifiProfileInfo))]
[System.Runtime.Versioning.SupportedOSPlatform("windows6.0.6000.0")]
public sealed class CmdletGetDesktopWifiProfile : PSCmdlet {
    /// <summary><para type="description">Optional exact wireless LAN interface identifier.</para></summary>
    [Parameter]
    public Guid? InterfaceId;

    /// <summary>Gets saved Wi-Fi profile snapshots.</summary>
    protected override void BeginProcessing() {
        using var service = new WifiProfileService();
        WriteObject(service.GetProfiles(InterfaceId), true);
    }
}
