using System;

namespace DesktopManager;

/// <summary>
/// Describes the network a wireless LAN interface is currently connected to.
/// </summary>
/// <remarks>
/// Windows versions before 11 24H2 may leave out the SSID and BSSID for apps without location consent, so
/// <see cref="Ssid"/> and <see cref="Bssid"/> can be <c>null</c> while <see cref="SignalQuality"/> is still reported.
/// </remarks>
public sealed class DesktopWifiConnectionInfo {
    internal DesktopWifiConnectionInfo(
        Guid interfaceId,
        DesktopWifiInterfaceState state,
        string profileName,
        string? ssid,
        string? bssid,
        DesktopWifiPhyType phyType,
        int signalQuality,
        long receiveRateKbps,
        long transmitRateKbps) {
        InterfaceId = interfaceId;
        State = state;
        ProfileName = profileName ?? throw new ArgumentNullException(nameof(profileName));
        Ssid = ssid;
        Bssid = bssid;
        PhyType = phyType;
        SignalQuality = signalQuality;
        ReceiveRateKbps = receiveRateKbps;
        TransmitRateKbps = transmitRateKbps;
    }

    /// <summary>Gets the wireless LAN interface the connection belongs to.</summary>
    public Guid InterfaceId { get; }

    /// <summary>Gets the interface state Windows reported with the connection.</summary>
    public DesktopWifiInterfaceState State { get; }

    /// <summary>Gets the saved Windows profile used for the connection, or an empty string when Windows reports none.</summary>
    public string ProfileName { get; }

    /// <summary>
    /// Gets the network name, or <c>null</c> when Windows withholds it (for example without location consent).
    /// </summary>
    /// <remarks>SSIDs are raw octets; they are decoded as UTF-8, which is what nearly all networks use.</remarks>
    public string? Ssid { get; }

    /// <summary>
    /// Gets the access point's MAC address as lowercase colon-separated hex, or <c>null</c> when Windows withholds it.
    /// </summary>
    public string? Bssid { get; }

    /// <summary>Gets the 802.11 physical layer in use.</summary>
    public DesktopWifiPhyType PhyType { get; }

    /// <summary>Gets the signal quality from 0 (none) to 100 (strongest), which Windows maps linearly from -100 to -50 dBm.</summary>
    public int SignalQuality { get; }

    /// <summary>Gets the receive link rate in kilobits per second.</summary>
    public long ReceiveRateKbps { get; }

    /// <summary>Gets the transmit link rate in kilobits per second.</summary>
    public long TransmitRateKbps { get; }
}
