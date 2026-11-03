namespace DesktopManager;

/// <summary>Reports a media read; a successful null session means no session is active.</summary>
public sealed class MediaSessionResult : DesktopOperationResult {
    internal MediaSessionResult(DesktopOperationStatus status, string message, MediaSessionInfo? session = null)
        : base(status, message) {
        Session = session;
    }

    /// <summary>Gets the current snapshot, or null when no session is active or the read did not succeed.</summary>
    public MediaSessionInfo? Session { get; }
}
