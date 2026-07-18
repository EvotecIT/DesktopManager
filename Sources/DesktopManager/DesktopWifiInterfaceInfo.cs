using System;

namespace DesktopManager;

/// <summary>
/// Represents one wireless LAN interface exposed by the Windows Native Wi-Fi API.
/// </summary>
public sealed class DesktopWifiInterfaceInfo {
    internal DesktopWifiInterfaceInfo(Guid interfaceId, string description, DesktopWifiInterfaceState state) {
        InterfaceId = interfaceId;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        State = state;
    }

    /// <summary>Gets the stable Windows interface identifier.</summary>
    public Guid InterfaceId { get; }

    /// <summary>Gets the Windows-provided interface description.</summary>
    public string Description { get; }

    /// <summary>Gets the current interface state without querying location-sensitive connection details.</summary>
    public DesktopWifiInterfaceState State { get; }
}
