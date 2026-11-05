using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Reads the current Wi-Fi connection of a wireless LAN interface: network name, signal quality and link details.
/// </summary>
/// <remarks>
/// <para>This uses the location-sensitive Native Wi-Fi current-connection query, which <see cref="WifiProfileService"/>
/// deliberately avoids. Since Windows 11 24H2, Windows refuses the whole query to apps without location consent;
/// <see cref="GetCurrent"/> then throws <see cref="UnauthorizedAccessException"/>. Older versions may answer with
/// the SSID and BSSID left out.</para>
/// <para>Windows can show a location-consent prompt on first use and record location activity. Background polling
/// callers should obtain consent through their normal interactive setup before starting this query.</para>
/// <para>Only reads; it never scans, connects or changes Wi-Fi settings.</para>
/// </remarks>
[SupportedOSPlatform("windows6.0.6000.0")]
public static class WifiConnectionService {
    // WLAN_CONNECTION_ATTRIBUTES from wlanapi.h. Every member is 4-byte aligned with no pointers, so the
    // layout is the same in 32-bit and 64-bit processes.
    internal const int StateOffset = 0;                     // WLAN_INTERFACE_STATE isState
    internal const int ProfileNameOffset = 8;               // WCHAR strProfileName[256]
    internal const int SsidLengthOffset = 520;              // wlanAssociationAttributes.dot11Ssid.uSSIDLength
    internal const int SsidOffset = 524;                    // dot11Ssid.ucSSID[32]
    internal const int BssidOffset = 560;                   // dot11Bssid[6], after the 4-byte dot11BssType at 556
    internal const int PhyTypeOffset = 568;                 // dot11PhyType, 4-byte aligned after the BSSID
    internal const int SignalQualityOffset = 576;           // wlanSignalQuality, after uDot11PhyIndex at 572
    internal const int ReceiveRateOffset = 580;             // ulRxRate in kbps
    internal const int TransmitRateOffset = 584;            // ulTxRate in kbps
    internal const int MinimumAttributesSize = 588;         // end of WLAN_ASSOCIATION_ATTRIBUTES
    private const int MaximumSsidLength = 32;
    private const int BssidLength = 6;

    /// <summary>
    /// Gets the network the interface is connected to.
    /// </summary>
    /// <param name="interfaceId">The wireless LAN interface, as returned by <see cref="WifiProfileService.GetInterfaces"/>;
    /// it matches <c>NetworkInterface.Id</c> for the same adapter.</param>
    /// <returns>The current connection, or <c>null</c> when the interface is not connected.</returns>
    /// <exception cref="UnauthorizedAccessException">Windows needs location access for this app before it answers.</exception>
    /// <exception cref="Win32Exception">The Native Wi-Fi service failed or does not know the interface.</exception>
    public static DesktopWifiConnectionInfo? GetCurrent(Guid interfaceId) {
        uint error = NativeWifiMethods.WlanOpenHandle(
            NativeWifiMethods.ClientVersionLonghorn,
            IntPtr.Zero,
            out _,
            out SafeWlanClientHandle clientHandle);
        using (clientHandle) {
            NativeWifiProfileApi.ThrowWindowsError(error, "Open the Windows Native Wi-Fi service");
            error = NativeWifiMethods.WlanQueryInterface(
                clientHandle,
                ref interfaceId,
                NativeWifiMethods.InterfaceOpcodeCurrentConnection,
                IntPtr.Zero,
                out uint size,
                out IntPtr data,
                IntPtr.Zero);
            if (error == NativeWifiMethods.ErrorInvalidState) {
                return null;
            }
            if (error == NativeWifiMethods.ErrorAccessDenied) {
                throw new UnauthorizedAccessException("Windows answers the current Wi-Fi connection only for apps that have location access (Settings > Privacy & security > Location).");
            }
            try {
                NativeWifiProfileApi.ThrowWindowsError(error, $"Query the current connection of wireless LAN interface '{interfaceId}'");
                if (data == IntPtr.Zero || size < MinimumAttributesSize) {
                    throw new InvalidDataException($"Windows returned {size} bytes of connection attributes; at least {MinimumAttributesSize} are required.");
                }
                byte[] attributes = new byte[MinimumAttributesSize];
                Marshal.Copy(data, attributes, 0, attributes.Length);
                return ParseConnectionAttributes(interfaceId, attributes);
            } finally {
                if (data != IntPtr.Zero) {
                    NativeWifiMethods.WlanFreeMemory(data);
                }
            }
        }
    }

    internal static DesktopWifiConnectionInfo ParseConnectionAttributes(Guid interfaceId, byte[] attributes) {
        if (attributes == null) {
            throw new ArgumentNullException(nameof(attributes));
        }
        if (attributes.Length < MinimumAttributesSize) {
            throw new InvalidDataException($"Connection attributes need at least {MinimumAttributesSize} bytes.");
        }

        var state = NativeWifiProfileApi.ToInterfaceState((NativeWifiMethods.WlanInterfaceState)BitConverter.ToInt32(attributes, StateOffset));
        string profileName = Encoding.Unicode.GetString(attributes, ProfileNameOffset, NativeWifiMethods.MaxNameLength * 2);
        int terminator = profileName.IndexOf('\0');
        if (terminator >= 0) {
            profileName = profileName.Substring(0, terminator);
        }

        uint ssidLength = BitConverter.ToUInt32(attributes, SsidLengthOffset);
        string? ssid = ssidLength > 0 && ssidLength <= MaximumSsidLength
            ? Encoding.UTF8.GetString(attributes, SsidOffset, (int)ssidLength)
            : null;

        string? bssid = null;
        for (int index = 0; index < BssidLength; index++) {
            if (attributes[BssidOffset + index] != 0) {
                bssid = BitConverter.ToString(attributes, BssidOffset, BssidLength).Replace('-', ':').ToLowerInvariant();
                break;
            }
        }

        uint quality = BitConverter.ToUInt32(attributes, SignalQualityOffset);
        return new DesktopWifiConnectionInfo(
            interfaceId,
            state,
            profileName,
            ssid,
            bssid,
            ToPhyType(BitConverter.ToUInt32(attributes, PhyTypeOffset)),
            (int)Math.Min(quality, 100u),
            BitConverter.ToUInt32(attributes, ReceiveRateOffset),
            BitConverter.ToUInt32(attributes, TransmitRateOffset));
    }

    private static DesktopWifiPhyType ToPhyType(uint phyType) {
        // DOT11_PHY_TYPE values 1 to 11 run in the same order as the public enum; 0 is unknown and
        // 0x80000000 and above are vendor-defined.
        return phyType >= 1 && phyType <= (uint)DesktopWifiPhyType.ExtremelyHighThroughput
            ? (DesktopWifiPhyType)phyType
            : DesktopWifiPhyType.Unknown;
    }
}
