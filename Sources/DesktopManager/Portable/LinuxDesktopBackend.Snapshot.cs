using System.IO;
using System.Linq;
using System.Text.Json;

namespace DesktopManager;

internal sealed partial class LinuxDesktopBackend {
    public DesktopWallpaperSnapshot CaptureWallpaperState() {
        var capabilities = GetCapabilities();
        if (!capabilities.IsAvailable) {
            return new DesktopWallpaperSnapshot(DesktopOperationStatus.Unavailable, capabilities.Message, _backend, this);
        }
        if (_backend == "kde") {
            return CaptureKdeWallpaperState();
        }
        var keys = GnomeImageKeys().Concat(new[] { "picture-options" }).ToArray();
        var values = new Dictionary<string, string>();
        foreach (var key in keys) {
            var value = RunGSettings("get", BackgroundSchema, key).Trim();
            // Validate the returned variant now, rather than discovering a corrupt checkpoint during recovery.
            DecodeGVariantString(value);
            values.Add(key, value);
        }
        if (!GnomeCheckpointSourceAvailable(values)) {
            return new DesktopWallpaperSnapshot(DesktopOperationStatus.Unsupported,
                "The active GNOME image must expose an existing local file before its checkpoint can be restored.", _backend, this);
        }
        var bus = _environment("DBUS_SESSION_BUS_ADDRESS");
        var display = _environment("DISPLAY");
        var wayland = _environment("WAYLAND_DISPLAY");
        return new DesktopWallpaperSnapshot(DesktopOperationStatus.Success, "Captured GNOME light/dark image URIs and image fit, including disabled images.",
            _backend, this, () => RestoreGnome(values, bus, display, wayland));
    }

    private DesktopWallpaperSnapshot CaptureKdeWallpaperState() {
        var checkpoint = RunKde("capture", null, null, 0).Trim();
        using var document = JsonDocument.Parse(checkpoint);
        var activity = document.RootElement.GetProperty("activity").GetString();
        if (string.IsNullOrEmpty(activity) || document.RootElement.GetProperty("desktops").GetArrayLength() == 0) {
            throw new FormatException("KDE did not return a restorable activity checkpoint.");
        }
        if (!KdeCheckpointSourcesAvailable(document.RootElement)) {
            return new DesktopWallpaperSnapshot(DesktopOperationStatus.Unsupported,
                "The active KDE image must expose an existing local file before its checkpoint can be restored; unresolved packages are unsupported.", _backend, this);
        }
        var bus = _environment("DBUS_SESSION_BUS_ADDRESS");
        var display = _environment("DISPLAY");
        var wayland = _environment("WAYLAND_DISPLAY");
        return new DesktopWallpaperSnapshot(DesktopOperationStatus.Success,
            "Captured current KDE activity screens, plugins and static image configuration, including absent keys.", _backend, this, () => {
                if (!SameGraphicalSession(bus, display, wayland)) {
                    return new DesktopOperationResult(DesktopOperationStatus.Unavailable, "The graphical session changed; no settings were restored.");
                }
                using (var original = JsonDocument.Parse(checkpoint)) {
                    if (!KdeCheckpointSourcesAvailable(original.RootElement)) {
                        return new DesktopOperationResult(DesktopOperationStatus.Unavailable,
                            "An original KDE image is unavailable; no settings were restored.");
                    }
                }
                var result = RunKde("restore", null, null, 0, activity, checkpoint);
                if (result.Trim() != "true" || RunKde("capture", null, null, 0, activity).Trim() != checkpoint) {
                    return new DesktopOperationResult(DesktopOperationStatus.Failed, "KDE restoration did not match native plugin/configuration readback.");
                }
                return new DesktopOperationResult(DesktopOperationStatus.Success, "KDE plugins, image/fit values and key absence were restored and read back.");
            });
    }

    private bool SameGraphicalSession(string? bus, string? display, string? wayland) => GetCapabilities().IsAvailable
        && bus == _environment("DBUS_SESSION_BUS_ADDRESS") && display == _environment("DISPLAY") && wayland == _environment("WAYLAND_DISPLAY");

    private bool GnomeCheckpointSourceAvailable(Dictionary<string, string> values) {
        if (DecodeGVariantString(values["picture-options"]) == "none") { return true; }
        var source = DecodeGVariantString(values[GnomeImageKey()]);
        if (source.Length == 0) { return true; }
        var path = LocalPath(source);
        return path != null && File.Exists(path);
    }

    private static bool KdeCheckpointSourcesAvailable(JsonElement checkpoint) {
        foreach (var desktop in checkpoint.GetProperty("desktops").EnumerateArray()) {
            if (desktop.GetProperty("plugin").GetString() != "org.kde.image") { continue; }
            var path = LocalPath(desktop.GetProperty("image").GetString());
            if (path == null || !File.Exists(path)) { return false; }
        }
        return true;
    }

    private DesktopOperationResult RestoreGnome(Dictionary<string, string> values, string? bus, string? display, string? wayland) {
        if (!SameGraphicalSession(bus, display, wayland)) {
            return new DesktopOperationResult(DesktopOperationStatus.Unavailable, "The graphical session changed; the checkpoint cannot be applied to another desktop session.");
        }
        var keys = GnomeImageKeys().Concat(new[] { "picture-options" }).ToArray();
        if (keys.Length != values.Count || keys.Any(key => !values.ContainsKey(key))) {
            return new DesktopOperationResult(DesktopOperationStatus.Unavailable, "The GNOME background schema changed; no settings were restored.");
        }
        if (!GnomeCheckpointSourceAvailable(values)) {
            return new DesktopOperationResult(DesktopOperationStatus.Unavailable,
                "The original active GNOME image is unavailable; no settings were restored.");
        }
        foreach (var key in keys) {
            if (RunGSettings("writable", BackgroundSchema, key).Trim() != "true") {
                return new DesktopOperationResult(DesktopOperationStatus.Unavailable, "GNOME background setting is locked: " + key);
            }
        }
        var failures = new List<string>();
        foreach (var key in keys) {
            try {
                RunGSettings("set", BackgroundSchema, key, values[key]);
            } catch (Exception ex) {
                failures.Add(key + ": " + ex.Message);
            }
        }
        foreach (var key in keys) {
            try {
                if (DecodeGVariantString(RunGSettings("get", BackgroundSchema, key)) != DecodeGVariantString(values[key])) {
                    failures.Add(key + ": restored value did not match native readback.");
                }
            } catch (Exception ex) {
                failures.Add(key + " readback: " + ex.Message);
            }
        }
        if (failures.Count > 0) {
            return new DesktopOperationResult(DesktopOperationStatus.Failed, "GNOME restoration was incomplete: " + string.Join("; ", failures));
        }
        return new DesktopOperationResult(DesktopOperationStatus.Success, "GNOME light/dark background URIs and image fit were restored and read back.");
    }
}
