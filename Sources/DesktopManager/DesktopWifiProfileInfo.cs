using System;

namespace DesktopManager;

/// <summary>
/// Represents a saved Windows wireless LAN profile without exposing profile XML or credentials.
/// </summary>
public sealed class DesktopWifiProfileInfo {
    internal DesktopWifiProfileInfo(
        DesktopWifiInterfaceInfo wifiInterface,
        string name,
        bool isGroupPolicy,
        bool isUserProfile) {
        Interface = wifiInterface ?? throw new ArgumentNullException(nameof(wifiInterface));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsGroupPolicy = isGroupPolicy;
        IsUserProfile = isUserProfile;
    }

    /// <summary>Gets the interface on which Windows stored the profile.</summary>
    public DesktopWifiInterfaceInfo Interface { get; }

    /// <summary>Gets the stable Windows interface identifier.</summary>
    public Guid InterfaceId => Interface.InterfaceId;

    /// <summary>Gets the Windows-provided interface description.</summary>
    public string InterfaceDescription => Interface.Description;

    /// <summary>Gets the case-sensitive saved profile name.</summary>
    public string Name { get; }

    /// <summary>Gets whether Group Policy supplied the profile.</summary>
    public bool IsGroupPolicy { get; }

    /// <summary>Gets whether the profile belongs to the current user.</summary>
    public bool IsUserProfile { get; }

    /// <summary>Gets whether the profile is an all-user profile.</summary>
    public bool IsAllUserProfile => !IsUserProfile;
}
