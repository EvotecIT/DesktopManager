using System.IO;
using System.Reflection;
using System.Text.Json;

namespace DesktopManager;

internal sealed class MacDesktopBackend : IPortableDesktopBackend {
    private readonly IDesktopCommandRunner _runner;
    private readonly Func<string, string?> _environment;
    private static readonly Lazy<string> Script = new Lazy<string>(ReadScript);

    internal MacDesktopBackend(IDesktopCommandRunner runner, Func<string, string?> environment) {
        _runner = runner;
        _environment = environment;
    }

    public DesktopCapabilities GetCapabilities() {
        var available = _runner.FindExecutable("/usr/bin/osascript") != null && GetSession().HasGraphicalSession;
        return new DesktopCapabilities("macos", available, true, true, true, true,
            available ? "Public AppKit desktop images for the current user's active Spaces/screens. Tile, Span, all-Spaces enumeration and lock screen changes are unsupported."
                : "The logged-in macOS console user's graphical session and /usr/bin/osascript are required.");
    }

    public IReadOnlyList<DesktopDisplayInfo> GetDisplays() {
        if (!GetCapabilities().IsAvailable) {
            return Array.Empty<DesktopDisplayInfo>();
        }
        using var document = JsonDocument.Parse(Invoke("displays", string.Empty));
        var displays = new List<DesktopDisplayInfo>();
        foreach (var item in document.RootElement.EnumerateArray()) {
            displays.Add(new DesktopDisplayInfo {
                Id = item.GetProperty("id").GetString() ?? string.Empty,
                Index = item.GetProperty("index").GetInt32(),
                DeviceName = item.GetProperty("name").GetString() ?? string.Empty,
                IsPrimary = item.GetProperty("primary").GetBoolean(),
                Left = item.GetProperty("left").GetInt32(),
                Top = item.GetProperty("top").GetInt32(),
                Width = item.GetProperty("width").GetInt32(),
                Height = item.GetProperty("height").GetInt32(),
                Scale = item.GetProperty("scale").GetDouble()
            });
        }
        return displays;
    }

    public DesktopUserSession GetSession() {
        var graphical = false;
        if (string.IsNullOrEmpty(_environment("SSH_CONNECTION")) && string.IsNullOrEmpty(_environment("SSH_TTY"))) {
            try {
                var consoleUser = _runner.Run("/usr/bin/stat", "-f", "%Su", "/dev/console").Trim();
                graphical = string.Equals(consoleUser, Environment.UserName, StringComparison.Ordinal) && consoleUser != "root";
            } catch (Exception) {
                // A missing or inaccessible console session must never select another user's desktop.
            }
        }
        return new DesktopUserSession {
            UserName = Environment.UserName,
            AccountName = Environment.UserName,
            DesktopEnvironment = "macOS",
            SessionType = "aqua",
            HasGraphicalSession = graphical
        };
    }

    public DesktopWallpaperState GetWallpaper(string? displayId) {
        var capabilities = GetCapabilities();
        if (!capabilities.IsAvailable) {
            return new DesktopWallpaperState(DesktopOperationStatus.Unavailable, capabilities.Message);
        }
        using var document = JsonDocument.Parse(Invoke("read", displayId ?? string.Empty));
        var path = document.RootElement.GetProperty("path");
        var position = document.RootElement.GetProperty("position");
        return new DesktopWallpaperState(DesktopOperationStatus.Success, "Current active macOS desktop image.",
            path.ValueKind == JsonValueKind.String ? path.GetString() : null,
            position.ValueKind == JsonValueKind.Number ? (DesktopWallpaperPosition?)position.GetInt32() : null);
    }

    public DesktopOperationResult ApplyWallpaper(DesktopWallpaperRequest request) {
        if (request.Position == DesktopWallpaperPosition.Tile || request.Position == DesktopWallpaperPosition.Span) {
            return new DesktopOperationResult(DesktopOperationStatus.Unsupported, "The macOS adapter supports Center, Stretch, Fit and Fill; Tile and Span are unsupported.");
        }
        Invoke("apply", request.DisplayId ?? string.Empty, request.FilePath, ((int)request.Position).ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new DesktopOperationResult(DesktopOperationStatus.Success, "AppKit accepted the image for the selected current desktops; management policy may override it.");
    }

    private string Invoke(params string[] arguments) {
        var commandArguments = new List<string> { "-l", "JavaScript", "-e", Script.Value, "--" };
        commandArguments.AddRange(arguments);
        return _runner.Run("/usr/bin/osascript", commandArguments.ToArray());
    }

    private static string ReadScript() {
        using var stream = typeof(MacDesktopBackend).GetTypeInfo().Assembly.GetManifestResourceStream("DesktopManager.Portable.MacDesktop.js")
            ?? throw new InvalidOperationException("The macOS desktop adapter resource is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
