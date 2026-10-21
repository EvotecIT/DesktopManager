using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DesktopManager;

internal sealed class LinuxDesktopBackend : IPortableDesktopBackend {
    private const string BackgroundSchema = "org.gnome.desktop.background";
    private readonly IDesktopCommandRunner _runner;
    private readonly Func<string, string?> _environment;
    private readonly string _backend;
    private static readonly Lazy<string> KdeScript = new Lazy<string>(ReadKdeScript);

    internal LinuxDesktopBackend(IDesktopCommandRunner runner, Func<string, string?> environment) {
        _runner = runner;
        _environment = environment;
        var desktop = environment("XDG_CURRENT_DESKTOP") ?? environment("DESKTOP_SESSION") ?? string.Empty;
        _backend = desktop.IndexOf("GNOME", StringComparison.OrdinalIgnoreCase) >= 0 ? "gnome"
            : desktop.IndexOf("KDE", StringComparison.OrdinalIgnoreCase) >= 0 || desktop.IndexOf("plasma", StringComparison.OrdinalIgnoreCase) >= 0 ? "kde"
            : "unsupported";
    }

    public DesktopCapabilities GetCapabilities() {
        if (_backend == "unsupported") {
            return new DesktopCapabilities(_backend, false, false, false, false, false,
                "Only GNOME and KDE Plasma desktop sessions have Linux wallpaper adapters. Image generation remains available.");
        }
        var command = FindDesktopCommand();
        var available = GetSession().HasGraphicalSession && command != null;
        return new DesktopCapabilities(_backend, available, false, true, _backend == "kde", true,
            !available ? "A graphical user session, its D-Bus session bus, and " + (_backend == "gnome" ? "gsettings" : "qdbus6 or qdbus") + " are required."
                : _backend == "gnome" ? "GNOME user background settings; per-display images are unsupported. Specify image dimensions; physical display enumeration is not qualified for X11 transforms or Wayland."
                : "KDE static image wallpapers in the current activity. Explicit targets use screen:<number>. Specify image dimensions; physical display enumeration is unavailable.");
    }

    public DesktopUserSession GetSession() {
        var graphical = string.IsNullOrEmpty(_environment("SSH_CONNECTION")) && string.IsNullOrEmpty(_environment("SSH_TTY"))
            && (!string.IsNullOrEmpty(_environment("DISPLAY")) || !string.IsNullOrEmpty(_environment("WAYLAND_DISPLAY")))
            && !string.IsNullOrEmpty(_environment("DBUS_SESSION_BUS_ADDRESS"));
        return new DesktopUserSession {
            UserName = Environment.UserName,
            AccountName = Environment.UserName,
            DesktopEnvironment = _environment("XDG_CURRENT_DESKTOP") ?? string.Empty,
            SessionType = IsWayland ? "wayland" : "x11",
            HasGraphicalSession = graphical
        };
    }

    public IReadOnlyList<DesktopDisplayInfo> GetDisplays() {
        return Array.Empty<DesktopDisplayInfo>();
    }

    public DesktopWallpaperState GetWallpaper(string? displayId) {
        var capabilities = GetCapabilities();
        if (!capabilities.IsAvailable) {
            return new DesktopWallpaperState(_backend == "unsupported" ? DesktopOperationStatus.Unsupported : DesktopOperationStatus.Unavailable, capabilities.Message);
        }
        if (_backend == "gnome") {
            if (!string.IsNullOrEmpty(displayId)) {
                return new DesktopWallpaperState(DesktopOperationStatus.Unsupported, "GNOME background settings do not select individual displays.");
            }
            var key = GnomeImageKey();
            var path = DecodeGVariantString(RunGSettings("get", BackgroundSchema, key));
            var fit = DecodeGVariantString(RunGSettings("get", BackgroundSchema, "picture-options"));
            return new DesktopWallpaperState(DesktopOperationStatus.Success, "Current GNOME background for the active light/dark style.",
                LocalPath(path), GnomePosition(fit));
        }
        if (!ValidKdeDisplayId(displayId)) {
            return new DesktopWallpaperState(DesktopOperationStatus.Unsupported, "KDE targets use screen:<nonnegative number>.");
        }
        using var document = JsonDocument.Parse(RunKde("read", displayId, null, 0));
        if (!document.RootElement.GetProperty("supported").GetBoolean()) {
            return new DesktopWallpaperState(DesktopOperationStatus.Unsupported, document.RootElement.GetProperty("message").GetString() ?? "Unsupported KDE wallpaper plugin.");
        }
        return new DesktopWallpaperState(DesktopOperationStatus.Success, "Current KDE static wallpaper for the current activity.",
            LocalPath(document.RootElement.GetProperty("path").GetString()), KdePosition(document.RootElement.GetProperty("fillMode").GetInt32()));
    }

    public DesktopOperationResult ApplyWallpaper(DesktopWallpaperRequest request) {
        var uri = new Uri(request.FilePath).AbsoluteUri;
        if (_backend == "gnome") {
            // Preflight every key before the first write so a locked style cannot yield a partial update.
            var keys = GnomeImageKeys();
            foreach (var key in keys.Concat(new[] { "picture-options" })) {
                if (!string.Equals(RunGSettings("writable", BackgroundSchema, key).Trim(), "true", StringComparison.Ordinal)) {
                    return new DesktopOperationResult(DesktopOperationStatus.Unavailable, "GNOME background setting is locked: " + key);
                }
            }
            foreach (var key in keys) {
                RunGSettings("set", BackgroundSchema, key, EncodeGVariantString(uri));
            }
            RunGSettings("set", BackgroundSchema, "picture-options", EncodeGVariantString(GnomeFit(request.Position)));
        } else {
            if (request.Position == DesktopWallpaperPosition.Span || !ValidKdeDisplayId(request.DisplayId)) {
                return new DesktopOperationResult(DesktopOperationStatus.Unsupported, "KDE does not support Span in this adapter; targets use screen:<nonnegative number>.");
            }
            var result = RunKde("apply", request.DisplayId, uri, KdeFit(request.Position));
            if (!string.Equals(result.Trim(), "true", StringComparison.Ordinal)) {
                return new DesktopOperationResult(DesktopOperationStatus.Failed, "KDE did not confirm the wallpaper operation.");
            }
        }
        return new DesktopOperationResult(DesktopOperationStatus.Success, "The desktop command completed; administrator policy may override the image.");
    }

    private bool IsWayland => string.Equals(_environment("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrEmpty(_environment("WAYLAND_DISPLAY"));

    private string? FindDesktopCommand() => _backend == "gnome" ? _runner.FindExecutable("gsettings") : _runner.FindExecutable("qdbus6", "qdbus");
    private string RunGSettings(params string[] arguments) => _runner.Run(FindDesktopCommand() ?? throw new InvalidOperationException("gsettings is unavailable."), arguments);

    private string[] GnomeImageKeys() {
        var keys = RunGSettings("list-keys", BackgroundSchema).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return keys.Contains("picture-uri-dark") ? new[] { "picture-uri", "picture-uri-dark" } : new[] { "picture-uri" };
    }

    private string GnomeImageKey() {
        if (GnomeImageKeys().Length > 1) {
            var style = DecodeGVariantString(RunGSettings("get", "org.gnome.desktop.interface", "color-scheme"));
            if (style == "prefer-dark") {
                return "picture-uri-dark";
            }
        }
        return "picture-uri";
    }

    private string RunKde(string action, string? displayId, string? uri, int fillMode) {
        // Write the fixed schema directly so request construction does not require runtime reflection.
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer)) {
            writer.WriteStartObject();
            writer.WriteString("action", action);
            writer.WriteString("displayId", displayId);
            writer.WriteString("uri", uri);
            writer.WriteNumber("fillMode", fillMode);
            writer.WriteEndObject();
        }
        var request = Encoding.UTF8.GetString(buffer.ToArray());
        return _runner.Run(FindDesktopCommand() ?? throw new InvalidOperationException("qdbus6 or qdbus is unavailable."),
            "org.kde.plasmashell", "/PlasmaShell", "org.kde.PlasmaShell.evaluateScript", "var request = " + request + ";\n" + KdeScript.Value);
    }

    private static bool ValidKdeDisplayId(string? id) => id == null || id.StartsWith("screen:", StringComparison.Ordinal)
        && int.TryParse(id.Substring(7), NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= 0;

    private static string? LocalPath(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile) {
            return uri.LocalPath;
        }
        return Path.IsPathRooted(value) ? value : null;
    }

    internal static string DecodeGVariantString(string value) {
        var text = value.Trim();
        if (text.Length < 2 || (text[0] != '\'' && text[0] != '"') || text[text.Length - 1] != text[0]) {
            throw new FormatException("The desktop returned an invalid string setting.");
        }
        var result = new StringBuilder();
        for (var i = 1; i < text.Length - 1; i++) {
            if (text[i] == '\\') {
                i++;
                if (i >= text.Length - 1) {
                    throw new FormatException("The desktop returned an unfinished string escape.");
                }
                switch (text[i]) {
                    case '\\': case '\'': case '"': result.Append(text[i]); break;
                    case 'a': result.Append('\a'); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'v': result.Append('\v'); break;
                    case 'u': case 'U':
                        var length = text[i] == 'u' ? 4 : 8;
                        if (i + length >= text.Length - 1 || !int.TryParse(text.Substring(i + 1, length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint)) {
                            throw new FormatException("The desktop returned an invalid Unicode string escape.");
                        }
                        result.Append(char.ConvertFromUtf32(codePoint));
                        i += length;
                        break;
                    default: throw new FormatException("The desktop returned an unsupported string escape.");
                }
            } else {
                result.Append(text[i]);
            }
        }
        return result.ToString();
    }

    private static string EncodeGVariantString(string value) => "'" + value.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
    private static string GnomeFit(DesktopWallpaperPosition fit) => fit == DesktopWallpaperPosition.Center ? "centered"
        : fit == DesktopWallpaperPosition.Tile ? "wallpaper" : fit == DesktopWallpaperPosition.Stretch ? "stretched"
        : fit == DesktopWallpaperPosition.Fit ? "scaled" : fit == DesktopWallpaperPosition.Span ? "spanned" : "zoom";
    private static DesktopWallpaperPosition? GnomePosition(string fit) => fit == "centered" ? DesktopWallpaperPosition.Center
        : fit == "wallpaper" ? DesktopWallpaperPosition.Tile : fit == "stretched" ? DesktopWallpaperPosition.Stretch
        : fit == "scaled" ? DesktopWallpaperPosition.Fit : fit == "spanned" ? DesktopWallpaperPosition.Span
        : fit == "zoom" ? DesktopWallpaperPosition.Fill : (DesktopWallpaperPosition?)null;
    private static int KdeFit(DesktopWallpaperPosition fit) => fit == DesktopWallpaperPosition.Center ? 6
        : fit == DesktopWallpaperPosition.Tile ? 3 : fit == DesktopWallpaperPosition.Stretch ? 0
        : fit == DesktopWallpaperPosition.Fit ? 1 : 2;
    private static DesktopWallpaperPosition? KdePosition(int fit) => fit == 6 ? DesktopWallpaperPosition.Center
        : fit == 3 ? DesktopWallpaperPosition.Tile : fit == 0 ? DesktopWallpaperPosition.Stretch
        : fit == 1 ? DesktopWallpaperPosition.Fit : fit == 2 ? DesktopWallpaperPosition.Fill : (DesktopWallpaperPosition?)null;

    private static string ReadKdeScript() {
        using var stream = typeof(LinuxDesktopBackend).GetTypeInfo().Assembly.GetManifestResourceStream("DesktopManager.Portable.KdeDesktop.js")
            ?? throw new InvalidOperationException("The KDE desktop adapter resource is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
