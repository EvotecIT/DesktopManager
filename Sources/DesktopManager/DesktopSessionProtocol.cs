namespace DesktopManager;

/// <summary>
/// Describes the transport used by a Windows desktop session.
/// </summary>
public enum DesktopSessionProtocol {
    /// <summary>The session uses the local console.</summary>
    Console = 0,
    /// <summary>The session uses a legacy transport.</summary>
    Legacy = 1,
    /// <summary>The session uses Remote Desktop Protocol.</summary>
    RemoteDesktop = 2,
    /// <summary>The protocol is unknown.</summary>
    Unknown = 255
}
