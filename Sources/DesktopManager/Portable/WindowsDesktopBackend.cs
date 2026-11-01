using System.Linq;

namespace DesktopManager;

/// <summary>Adapts the existing Windows owner without duplicating its native display or wallpaper implementation.</summary>
internal sealed class WindowsDesktopBackend : IPortableDesktopBackend, IDesktopWallpaperSnapshotBackend {
    private readonly MonitorService _monitors;
    private readonly Func<IReadOnlyList<MonitorSnapshot>> _snapshots;
    private readonly Func<bool> _isInteractive;
    private readonly WindowsWallpaperSelection? _selection;

    internal WindowsDesktopBackend() : this(new MonitorService(DesktopManagerWrapper.Create), selection: new WindowsWallpaperSelection()) { }

    internal WindowsDesktopBackend(MonitorService monitors, Func<IReadOnlyList<MonitorSnapshot>>? snapshots = null, Func<bool>? isInteractive = null,
        WindowsWallpaperSelection? selection = null) {
        _monitors = monitors;
        _snapshots = snapshots ?? monitors.GetMonitorSnapshots;
        _isInteractive = isInteractive ?? (() => Environment.UserInteractive);
        _selection = selection;
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

    public IReadOnlyList<DesktopWallpaperTarget> GetWallpaperTargets() {
        if (!_isInteractive()) {
            return Array.Empty<DesktopWallpaperTarget>();
        }
        var displays = GetDisplays();
        var targets = displays.Where(display => !display.Id.StartsWith("index:", StringComparison.Ordinal))
            .Select((display, index) => new DesktopWallpaperTarget {
                DisplayId = display.Id, Index = index, Name = display.DeviceName, Display = display
            }).ToArray();
        return targets.Length > 0 ? targets : new[] { new DesktopWallpaperTarget { Name = "Windows desktop (all displays)" } };
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
        var background = _monitors.ReadNativeWallpaperBackground(out var enabled);
        var mode = _selection?.Read();
        if (_selection != null && (!mode.HasValue || mode < 0 || mode > 2)) {
            return new DesktopWallpaperState(DesktopOperationStatus.Unsupported,
                "The selected Windows background mode is absent, unknown or dynamic Spotlight; a retained COM image is not a qualified effective source.");
        }
        enabled &= mode != 1;
        return new DesktopWallpaperState(DesktopOperationStatus.Success, "Current Windows wallpaper and solid background color.",
            enabled ? ReadWallpaper(display) : null, _monitors.GetWallpaperPosition(), background, enabled ? 1 : 0);
    }

    public DesktopOperationResult ApplyWallpaper(DesktopWallpaperRequest request) {
        var originalMode = _selection?.Read();
        if (originalMode.HasValue) { _selection!.EnsureWritable(); }
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
        // COM can paint an image without changing Explorer's selected solid-color mode.
        // Keep the selected mode coherent with the explicitly requested image assignment.
        if (originalMode.HasValue) { _selection!.Set(0); }
        return new DesktopOperationResult(DesktopOperationStatus.Success, "The Windows wallpaper API completed; desktop policy may override the image.");
    }

    private DesktopDisplayInfo? ResolveDisplay(string? id) {
        var displays = GetDisplays();
        return id == null ? displays.FirstOrDefault(display => display.IsPrimary) ?? displays.FirstOrDefault()
            : displays.FirstOrDefault(display => string.Equals(display.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public DesktopWallpaperSnapshot CaptureWallpaperState() {
        return !_isInteractive()
            ? new DesktopWallpaperSnapshot(DesktopOperationStatus.Unavailable, "An interactive Windows session is required.", "windows", this)
            : _monitors.CaptureNativeWallpaperCheckpoint(this, _isInteractive, _snapshots, _selection);
    }

    private string ReadWallpaper(DesktopDisplayInfo display) {
        return display.Id.StartsWith("index:", StringComparison.Ordinal)
            ? _monitors.GetWallpaper(display.Index) : _monitors.GetWallpaperForDisplayStrict(display.Id);
    }
}
