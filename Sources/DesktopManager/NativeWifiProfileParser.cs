using System;
using System.IO;
using System.Xml;

namespace DesktopManager;

/// <summary>
/// Reads only connection metadata required by the Native Wi-Fi connection contract.
/// </summary>
internal static class NativeWifiProfileParser {
    internal static NativeWifiMethods.Dot11BssType ReadBssType(string profileXml) {
        if (string.IsNullOrWhiteSpace(profileXml)) {
            throw new InvalidDataException("Windows returned empty Wi-Fi profile metadata.");
        }

        var settings = new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var textReader = new StringReader(profileXml);
        using XmlReader reader = XmlReader.Create(textReader, settings);
        while (reader.Read()) {
            if (reader.NodeType != XmlNodeType.Element ||
                !string.Equals(reader.LocalName, "connectionType", StringComparison.Ordinal)) {
                continue;
            }

            string connectionType = reader.ReadElementContentAsString().Trim();
            return connectionType switch {
                "ESS" => NativeWifiMethods.Dot11BssType.Infrastructure,
                "IBSS" => NativeWifiMethods.Dot11BssType.Independent,
                _ => throw new InvalidDataException(
                    $"Windows returned unsupported Wi-Fi profile connection type '{connectionType}'.")
            };
        }

        throw new InvalidDataException("Windows Wi-Fi profile metadata does not contain a connection type.");
    }
}
