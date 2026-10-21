using System.Linq;

namespace DesktopManager;

/// <summary>Adapts the existing Windows owner without duplicating its native display or wallpaper implementation.</summary>
internal sealed class WindowsDesktopBackend : IPortableDesktopBackend {
    private readonly MonitorService _monitors;
    private readonly Func<IReadOnlyList<MonitorSnapshot>> _snapshots;
    private readonly Func<bool> _isInteractive;

    internal WindowsDesktopBackend() : this(new MonitorService(() => (IDesktopManager)new DesktopManagerWrapper())) { }

    internal WindowsDesktopBackend(MonitorService monitors, Func<IReadOnlyList<MonitorSnapshot>>? snapshots = null, Func<bool>? isInteractive = null) {
        _monitors = monitors;
        _snapshots = snapshots ?? monitors.GetMonitorSnapshots;
        _isInteractive = isInteractive ?? (() => Environment.UserInteractive);
    }

    public DesktopCapabilities GetCapabilities() {
        var graphical = _isInteractive();
        var displaysAvailable = graphical;
        var perDisplay = false;
        var message = graphical ? "Current Windows user desktop; existing Windows APIs separately expose lock screen, all-user policy and slideshow operations."
            : "An interactive Windows user session is required to apply desktop wallpaper.";
        if (graphical) {
            try {
                perDisplay = _snapshots().Any(display => !string.IsNullOrWhiteSpace(display.DeviceId));
            } catch (Exception ex) {
                // Geometry failure must disable targeted capability, not prevent an
                // explicitly desktop-wide operation using the established fallback.
                displaysAvailable = false;
                message += " Display enumeration is unavailable: " + ex.Message;
            }
        }
        return new DesktopCapabilities("windows", graphical, displaysAvailable, true, perDisplay, true, message);
    }

    public IReadOnlyList<DesktopDisplayInfo> GetDisplays() {
        return _snapshots().Select(display => new DesktopDisplayInfo {
            Id = string.IsNullOrEmpty(display.DeviceId) ? "index:" + display.Index : display.DeviceId,
            Index = display.Index,
            DeviceName = display.DeviceName,
            IsPrimary = display.IsPrimary,
            Left = display.Left,
            Top = display.Top,
            Width = display.Width,
            Height = display.Height,
            Scale = display.Scale
        }).ToArray();
    }

    public DesktopUserSession GetSession() {
        var user = new DesktopSessionService().GetSessionUser();
        return new DesktopUserSession {
            UserName = user?.UserName ?? string.Empty,
            AccountName = user?.AccountName ?? string.Empty,
            DesktopEnvironment = "Windows",
            SessionType = "windows",
            HasGraphicalSession = _isInteractive()
        };
    }

    public DesktopWallpaperState GetWallpaper(string? displayId) {
        var display = ResolveDisplay(displayId);
        if (display == null) {
            return new DesktopWallpaperState(DesktopOperationStatus.Unavailable, "The requested display is not attached.");
        }
        if (displayId != null && display.Id.StartsWith("index:", StringComparison.Ordinal)) {
            return new DesktopWallpaperState(DesktopOperationStatus.Unsupported, "The display has no addressable wallpaper device path; its individual image cannot be read.");
        }
        return new DesktopWallpaperState(DesktopOperationStatus.Success, "Current Windows wallpaper.",
            ReadWallpaper(display), _monitors.GetWallpaperPosition());
    }

    public DesktopOperationResult ApplyWallpaper(DesktopWallpaperRequest request) {
        if (request.DisplayId == null) {
            _monitors.SetWallpaper(request.FilePath);
            _monitors.SetWallpaperPosition(request.Position);
        } else {
            var display = ResolveDisplay(request.DisplayId);
            if (display == null) {
                return new DesktopOperationResult(DesktopOperationStatus.Unavailable, "The requested display is not attached.");
            }
            if (display.Id.StartsWith("index:", StringComparison.Ordinal)) {
                return new DesktopOperationResult(DesktopOperationStatus.Unsupported, "The display has no addressable wallpaper device path; a targeted request cannot fall back to every display.");
            }
            _monitors.SetWallpaperForDisplayStrict(display.Id, request.FilePath);
            // Windows shares fit across the desktop. A rejected image target must
            // not change that global fit setting as an incidental partial update.
            _monitors.SetWallpaperPosition(request.Position);
        }
        return new DesktopOperationResult(DesktopOperationStatus.Success, "The Windows wallpaper API completed; desktop policy may override the image.");
    }

    private DesktopDisplayInfo? ResolveDisplay(string? id) {
        var displays = GetDisplays();
        return id == null ? displays.FirstOrDefault(display => display.IsPrimary) ?? displays.FirstOrDefault()
            : displays.FirstOrDefault(display => string.Equals(display.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private string ReadWallpaper(DesktopDisplayInfo display) {
        return display.Id.StartsWith("index:", StringComparison.Ordinal)
            ? _monitors.GetWallpaper(display.Index) : _monitors.GetWallpaperForDisplayStrict(display.Id);
    }
}
