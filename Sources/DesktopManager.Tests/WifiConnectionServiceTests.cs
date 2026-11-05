using System.ComponentModel;
using System.Text;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace DesktopManager.Tests;

/// <summary>
/// Protects the WLAN_CONNECTION_ATTRIBUTES layout and the read-only current-connection query.
/// </summary>
[TestClass]
#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows6.0.6000.0")]
#endif
public class WifiConnectionServiceTests {
    // Offsets below come from WLAN_CONNECTION_ATTRIBUTES in the Windows SDK's wlanapi.h, independently of the parser.
    private const int AttributesSize = 604;

    [TestMethod]
    public void ParseConnectionAttributes_ReadsAssociationFieldsAtWindowsSdkOffsets() {
        byte[] attributes = new byte[AttributesSize];
        WriteUInt32(attributes, 0, 1); // wlan_interface_state_connected
        WriteUInt32(attributes, 4, 0); // wlan_connection_mode_profile
        Encoding.Unicode.GetBytes("Home profile").CopyTo(attributes, 8);
        byte[] ssid = Encoding.UTF8.GetBytes("Café 5G");
        WriteUInt32(attributes, 520, (uint)ssid.Length);
        ssid.CopyTo(attributes, 524);
        WriteUInt32(attributes, 556, 1); // dot11_BSS_type_infrastructure
        new byte[] { 0x00, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E }.CopyTo(attributes, 560);
        WriteUInt32(attributes, 568, 10); // dot11_phy_type_he
        WriteUInt32(attributes, 572, 3); // uDot11PhyIndex
        WriteUInt32(attributes, 576, 87);
        WriteUInt32(attributes, 580, 1200900);
        WriteUInt32(attributes, 584, 864800);
        WriteUInt32(attributes, 588, 1); // bSecurityEnabled
        Guid interfaceId = Guid.NewGuid();

        DesktopWifiConnectionInfo connection = WifiConnectionService.ParseConnectionAttributes(interfaceId, attributes);

        Assert.AreEqual(interfaceId, connection.InterfaceId);
        Assert.AreEqual(DesktopWifiInterfaceState.Connected, connection.State);
        Assert.AreEqual("Home profile", connection.ProfileName);
        Assert.AreEqual("Café 5G", connection.Ssid);
        Assert.AreEqual("00:1a:2b:3c:4d:5e", connection.Bssid);
        Assert.AreEqual(DesktopWifiPhyType.HighEfficiency, connection.PhyType);
        Assert.AreEqual(87, connection.SignalQuality);
        Assert.AreEqual(1200900L, connection.ReceiveRateKbps);
        Assert.AreEqual(864800L, connection.TransmitRateKbps);
    }

    [TestMethod]
    public void ParseConnectionAttributes_WithheldNetworkIdentityKeepsSignal() {
        byte[] attributes = new byte[AttributesSize];
        WriteUInt32(attributes, 0, 1);
        WriteUInt32(attributes, 568, 0x80000001); // vendor-defined PHY
        WriteUInt32(attributes, 576, 64);

        DesktopWifiConnectionInfo connection = WifiConnectionService.ParseConnectionAttributes(Guid.NewGuid(), attributes);

        Assert.IsNull(connection.Ssid);
        Assert.IsNull(connection.Bssid);
        Assert.AreEqual(string.Empty, connection.ProfileName);
        Assert.AreEqual(DesktopWifiPhyType.Unknown, connection.PhyType);
        Assert.AreEqual(64, connection.SignalQuality);
    }

    [TestMethod]
    public void GetCurrent_ConnectedInterface_ReportsItsSignalWithoutChangingConnectivity() {
        IReadOnlyList<DesktopWifiInterfaceInfo> interfaces;
        try {
            using var service = new WifiProfileService();
            interfaces = service.GetInterfaces();
        } catch (Exception ex) when (ex is Win32Exception || ex is DllNotFoundException) {
            Assert.Inconclusive($"The Native Wi-Fi service is unavailable: {ex.Message}");
            return;
        }

        DesktopWifiInterfaceInfo[] connected = interfaces.Where(item => item.State == DesktopWifiInterfaceState.Connected).ToArray();
        if (connected.Length == 0) {
            Assert.Inconclusive("No connected Wi-Fi interface is available.");
        }

        foreach (DesktopWifiInterfaceInfo wifiInterface in connected) {
            DesktopWifiConnectionInfo? connection = WifiConnectionService.GetCurrent(wifiInterface.InterfaceId);
            if (connection == null) {
                continue; // The interface disconnected between the two reads.
            }

            Assert.AreEqual(wifiInterface.InterfaceId, connection.InterfaceId);
            Assert.IsTrue(connection.SignalQuality is >= 0 and <= 100);
            Assert.IsTrue(connection.Ssid == null || connection.Ssid.Length is > 0 and <= 32);
        }
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value) {
        BitConverter.GetBytes(value).CopyTo(buffer, offset);
    }
}
