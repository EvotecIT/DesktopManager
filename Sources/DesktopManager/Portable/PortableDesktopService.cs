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
            return _backend.ApplyWallpaper(new DesktopWallpaperRequest {
                FilePath = Path.GetFullPath(request.FilePath),
                DisplayId = string.IsNullOrEmpty(request.DisplayId) ? null : request.DisplayId,
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
    DesktopUserSession GetSession();
    DesktopWallpaperState GetWallpaper(string? displayId);
    DesktopOperationResult ApplyWallpaper(DesktopWallpaperRequest request);
}
