namespace DesktopManager;

/// <summary>An opaque, in-memory native wallpaper checkpoint for restoration through the service that captured it.</summary>
/// <remarks>Capture is read-only. Keep this object in the same process; it is not a portable configuration or a serializable backup.</remarks>
public sealed class DesktopWallpaperSnapshot : DesktopOperationResult, IDisposable {
    private readonly object _sync = new object();
    private readonly IDisposable? _resource;
    private bool _disposed;
    internal DesktopWallpaperSnapshot(DesktopOperationStatus status, string message, string backendId, object owner,
        Func<DesktopOperationResult>? restore = null, IDisposable? resource = null) : base(status, message) {
        BackendId = backendId;
        Owner = owner;
        Restore = restore;
        _resource = resource;
    }

    /// <summary>Gets the native backend that owns this checkpoint.</summary>
    public string BackendId { get; }
    internal object Owner { get; }
    internal Func<DesktopOperationResult>? Restore { get; }

    internal DesktopOperationResult RestoreNative() {
        lock (_sync) {
            return _disposed || Restore == null
                ? new DesktopOperationResult(DesktopOperationStatus.Failed, "The wallpaper checkpoint has been disposed or cannot be restored.")
                : Restore();
        }
    }

    /// <summary>Releases native checkpoint resources after restoration or when the checkpoint is no longer needed. This does not change desktop settings.</summary>
    public void Dispose() {
        lock (_sync) {
            if (!_disposed) {
                _disposed = true;
                _resource?.Dispose();
            }
        }
    }
}

internal interface IDesktopWallpaperSnapshotBackend {
    DesktopWallpaperSnapshot CaptureWallpaperState();
}
