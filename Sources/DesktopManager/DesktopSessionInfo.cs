using System;

namespace DesktopManager;

/// <summary>
/// Captures the current interactive Windows session state.
/// </summary>
public sealed class DesktopSessionInfo {
    internal DesktopSessionInfo(
        int sessionId,
        string userName,
        string domainName,
        string clientName,
        DesktopSessionConnectState connectState,
        DesktopSessionProtocol protocol,
        bool isRemote,
        bool? isLocked,
        TimeSpan idleTime) {
        SessionId = sessionId;
        UserName = userName;
        DomainName = domainName;
        ClientName = clientName;
        ConnectState = connectState;
        Protocol = protocol;
        IsRemote = isRemote;
        IsLocked = isLocked;
        IdleTime = idleTime;
    }

    /// <summary>Gets the Windows session identifier.</summary>
    public int SessionId { get; }

    /// <summary>Gets the session user name.</summary>
    public string UserName { get; }

    /// <summary>Gets the session domain name.</summary>
    public string DomainName { get; }

    /// <summary>Gets the remote client name, or an empty string for a local session.</summary>
    public string ClientName { get; }

    /// <summary>Gets the Terminal Services connection state.</summary>
    public DesktopSessionConnectState ConnectState { get; }

    /// <summary>Gets the session transport.</summary>
    public DesktopSessionProtocol Protocol { get; }

    /// <summary>Gets whether the session is remote.</summary>
    public bool IsRemote { get; }

    /// <summary>Gets whether the interactive desktop appears locked, or <c>null</c> when it cannot be determined.</summary>
    public bool? IsLocked { get; }

    /// <summary>Gets the time since the most recent user input in the current session.</summary>
    public TimeSpan IdleTime { get; }
}
