namespace DesktopManager;

/// <summary>Captures a signed-in Windows session user and how its session was selected.</summary>
public readonly struct DesktopSessionUser {
    internal DesktopSessionUser(int sessionId, string userName, string domainName, bool isConsoleFallback) {
        SessionId = sessionId;
        UserName = userName;
        DomainName = domainName;
        IsConsoleFallback = isConsoleFallback;
    }

    /// <summary>Gets the Windows session identifier from which the user was read.</summary>
    public int SessionId { get; }

    /// <summary>Gets the signed-in user's account name without its domain.</summary>
    public string UserName { get; }

    /// <summary>Gets the account's domain, or an empty string when WTS does not provide it.</summary>
    public string DomainName { get; }

    /// <summary>Gets <c>DOMAIN\user</c>, or just the user name when the domain is unavailable.</summary>
    public string AccountName => string.IsNullOrEmpty(DomainName) ? UserName : DomainName + "\\" + UserName;

    /// <summary>Gets whether an implicit lookup from process session 0 selected the active console session.</summary>
    public bool IsConsoleFallback { get; }
}
