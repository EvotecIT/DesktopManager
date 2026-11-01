using System.IO;

namespace DesktopManager;

/// <summary>Cross-platform display, user-session and wallpaper operations with explicit capability limits.</summary>
/// <remarks>Existing Windows window-management APIs remain Windows-only. No desktop settings change until ApplyWallpaper is called.</remarks>
public sealed class PortableDesktopService {
    private readonly IPortableDesktopBackend _backend;

    /// <summary>Selects the current platform and desktop backend. No settings are changed.</summary>
    public PortableDesktopService() : this(PortableDesktopBackendFactory.Create()) { }

    internal PortableDesktopService(IPortableDesktopBackend backend) {
        _backend = backend;
    }

    /// <summary>Reads backend availability and supported operations without changing settings.</summary>
    public DesktopCapabilities GetCapabilities() => _backend.GetCapabilities();

    /// <summary>Reads current display geometry. An unsupported backend returns an empty list; command failures throw.</summary>
    public IReadOnlyList<DesktopDisplayInfo> GetDisplays() => _backend.GetDisplays();

    /// <summary>Reads addressable wallpaper targets without assuming that their physical pixel geometry is available.</summary>
    /// <remarks>Targets describe the current session and topology. Rediscover them after display or activity changes; an unavailable backend returns an empty list.</remarks>
    public IReadOnlyList<DesktopWallpaperTarget> GetWallpaperTargets() => _backend.GetWallpaperTargets();

    /// <summary>Reads the current process's desktop user and session context.</summary>
    public DesktopUserSession GetSession() => _backend.GetSession();

    /// <summary>Reads a display's current wallpaper, or the backend's default desktop when displayId is null.</summary>
    public DesktopWallpaperState GetWallpaper(string? displayId = null) {
        try {
            return _backend.GetWallpaper(displayId);
        } catch (Exception ex) {
            return new DesktopWallpaperState(DesktopOperationStatus.Failed, ex.Message);
        }
    }

    /// <summary>Captures the native settings affected by this backend's wallpaper application, without changing the desktop.</summary>
    /// <remarks>Check Succeeded before applying a test image. Unsupported capture is a reason to leave the desktop unchanged.</remarks>
    public DesktopWallpaperSnapshot CaptureWallpaperState() {
        var backendId = "unknown";
        try {
            backendId = GetCapabilities().BackendId;
            return _backend is IDesktopWallpaperSnapshotBackend snapshots ? snapshots.CaptureWallpaperState()
                : new DesktopWallpaperSnapshot(DesktopOperationStatus.Unsupported, "Native wallpaper checkpoints are unavailable for this backend.", backendId, _backend);
        } catch (Exception ex) {
            return new DesktopWallpaperSnapshot(DesktopOperationStatus.Failed, ex.Message, backendId, _backend);
        }
    }

    /// <summary>Restores a successful checkpoint captured by this service, after native session and target validation.</summary>
    /// <remarks>A failure can describe a partial native restore; inspect the result and preserve the checkpoint for recovery.</remarks>
    public DesktopOperationResult RestoreWallpaperState(DesktopWallpaperSnapshot snapshot) {
        if (snapshot == null) {
            throw new ArgumentNullException(nameof(snapshot));
        }
        if (!ReferenceEquals(snapshot.Owner, _backend) || !snapshot.Succeeded || snapshot.Restore == null) {
            return new DesktopOperationResult(DesktopOperationStatus.Failed, "The checkpoint must be successful and belong to this desktop service.");
        }
        try {
            return snapshot.RestoreNative();
        } catch (Exception ex) {
            return new DesktopOperationResult(DesktopOperationStatus.Failed, "Native wallpaper restoration failed: " + ex.Message);
        }
    }

    /// <summary>Assigns a local image after validating the request, capability and target.</summary>
    public DesktopOperationResult ApplyWallpaper(DesktopWallpaperRequest request) {
        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.FilePath) || !File.Exists(request.FilePath)) {
            return new DesktopOperationResult(DesktopOperationStatus.Failed, "The wallpaper image must be an existing local file.");
        }
        if (!Enum.IsDefined(typeof(DesktopWallpaperPosition), request.Position)) {
            return new DesktopOperationResult(DesktopOperationStatus.Failed, "The wallpaper position is invalid.");
        }
        try {
            var capabilities = GetCapabilities();
            if (!capabilities.IsAvailable) {
                return new DesktopOperationResult(capabilities.BackendId == "unsupported" ? DesktopOperationStatus.Unsupported : DesktopOperationStatus.Unavailable, capabilities.Message);
            }
            if (!capabilities.CanSetWallpaper || (!string.IsNullOrEmpty(request.DisplayId) && !capabilities.SupportsPerDisplayWallpaper)) {
                return new DesktopOperationResult(DesktopOperationStatus.Unsupported, "This desktop does not support the requested wallpaper target.");
            }
            if (!System.Linq.Enumerable.Contains(capabilities.SupportedWallpaperPositions, request.Position)) {
                return new DesktopOperationResult(DesktopOperationStatus.Unsupported, "This desktop does not support the requested wallpaper position.");
            }
            if (request.ExpectedScopeId != null && capabilities.BackendId != "kde") {
                return new DesktopOperationResult(DesktopOperationStatus.Unsupported, "This backend does not expose a wallpaper scope identifier.");
            }
            return _backend.ApplyWallpaper(new DesktopWallpaperRequest {
                FilePath = Path.GetFullPath(request.FilePath),
                DisplayId = string.IsNullOrEmpty(request.DisplayId) ? null : request.DisplayId,
                ExpectedScopeId = request.ExpectedScopeId,
                Position = request.Position
            });
        } catch (Exception ex) {
            return new DesktopOperationResult(DesktopOperationStatus.Failed, ex.Message);
        }
    }
}

internal interface IPortableDesktopBackend {
    DesktopCapabilities GetCapabilities();
    IReadOnlyList<DesktopDisplayInfo> GetDisplays();
    IReadOnlyList<DesktopWallpaperTarget> GetWallpaperTargets();
    DesktopUserSession GetSession();
    DesktopWallpaperState GetWallpaper(string? displayId);
    DesktopOperationResult ApplyWallpaper(DesktopWallpaperRequest request);
}
