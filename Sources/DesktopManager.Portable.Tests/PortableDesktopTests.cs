using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DesktopManager.Portable.Tests;

[TestClass]
public sealed class PortableDesktopTests {
    [TestMethod]
    public void WallpaperState_NormalizesMissingPathsAndPreservesOtherResultFields() {
        foreach (var missingPath in new string?[] { null, string.Empty, " \t\r\n" }) {
            var state = new DesktopWallpaperState(DesktopOperationStatus.Success, "No image assigned.", missingPath, DesktopWallpaperPosition.Fit);
            Assert.IsNull(state.FilePath);
            Assert.AreEqual(DesktopOperationStatus.Success, state.Status);
            Assert.AreEqual("No image assigned.", state.Message);
            Assert.AreEqual(DesktopWallpaperPosition.Fit, state.Position);
        }

        const string existingPath = "/photos/ background with spaces.png ";
        var assigned = new DesktopWallpaperState(DesktopOperationStatus.Failed, "Read failed.", existingPath, DesktopWallpaperPosition.Center);
        Assert.AreEqual(existingPath, assigned.FilePath);
        Assert.AreEqual(DesktopOperationStatus.Failed, assigned.Status);
        Assert.AreEqual("Read failed.", assigned.Message);
        Assert.AreEqual(DesktopWallpaperPosition.Center, assigned.Position);
    }

    [TestMethod]
    public void HeadlessSession_DoesNotOfferWallpaperOperations() {
        var runner = new FakeRunner();
        var backend = Linux(runner, new Dictionary<string, string> { ["XDG_CURRENT_DESKTOP"] = "GNOME" });
        Assert.IsFalse(backend.GetCapabilities().CanSetWallpaper);
        Assert.IsFalse(backend.GetSession().HasGraphicalSession);
        Assert.AreEqual(DesktopOperationStatus.Unavailable, backend.GetWallpaper(null).Status);
        Assert.AreEqual(0, runner.Calls.Count);
    }

    [TestMethod]
    public void SshContext_DoesNotClaimTheConsoleUsersGraphicalSession() {
        var environment = DesktopEnvironment("GNOME");
        environment["SSH_CONNECTION"] = "192.0.2.1 1234 192.0.2.2 22";
        var backend = Linux(new FakeRunner(), environment);
        Assert.IsFalse(backend.GetSession().HasGraphicalSession);
        Assert.IsFalse(backend.GetCapabilities().CanSetWallpaper);
    }

    [TestMethod]
    public void UnknownLinuxDesktop_ReportsUnsupportedWithoutChangingSettings() {
        WithImage(path => {
            var runner = new FakeRunner();
            var service = new PortableDesktopService(Linux(runner, DesktopEnvironment("sway")));
            Assert.AreEqual(DesktopOperationStatus.Unsupported, service.GetWallpaper().Status);
            Assert.AreEqual(DesktopOperationStatus.Unsupported, service.ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path }).Status);
            Assert.AreEqual(0, runner.Calls.Count);
        });
    }

    [TestMethod]
    public void GnomeStringReadback_PreservesEscapedPathsAndRejectsMalformedEscapes() {
        Assert.AreEqual("file:///home/alice/it's-a-\"picture\".jpg", LinuxDesktopBackend.DecodeGVariantString("'file:///home/alice/it\\'s-a-\\\"picture\\\".jpg'"));
        Assert.AreEqual("file:///home/alice/ł.jpg", LinuxDesktopBackend.DecodeGVariantString("'file:///home/alice/\\u0142.jpg'"));
        Assert.ThrowsExactly<FormatException>(() => LinuxDesktopBackend.DecodeGVariantString("'file:///broken\\q.jpg'"));
    }

    [TestMethod]
    public void GnomeDarkStyle_ReadsTheActiveImageAndFitMode() {
        var runner = new FakeRunner {
            Execute = arguments => arguments[0] == "list-keys" ? "picture-uri\npicture-uri-dark\npicture-options\n"
                : arguments[2] == "color-scheme" ? "'prefer-dark'"
                : arguments[2] == "picture-options" ? "'scaled'"
                : arguments[2] == "picture-uri-dark" ? "'file:///home/alice/Pictures/dark%20image.jpg'"
                : throw new InvalidOperationException("The light image must not be read for a dark desktop.")
        };
        var state = Linux(runner).GetWallpaper(null);
        Assert.IsTrue(state.Succeeded);
        Assert.AreEqual("/home/alice/Pictures/dark image.jpg", state.FilePath);
        Assert.AreEqual(DesktopWallpaperPosition.Fit, state.Position);
        Assert.IsFalse(Linux(runner).GetCapabilities().CanEnumerateDisplays);
    }

    [TestMethod]
    public void GnomeLockedSetting_RejectsTheWholeUpdateBeforeWriting() {
        WithImage(path => {
            var runner = new FakeRunner {
                Execute = args => args[0] == "list-keys" ? "picture-uri\npicture-uri-dark\n"
                    : args[0] == "writable" ? (args[2] == "picture-uri-dark" ? "false" : "true")
                    : throw new InvalidOperationException("No setting should be written.")
            };
            var result = new PortableDesktopService(Linux(runner)).ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path });
            Assert.AreEqual(DesktopOperationStatus.Unavailable, result.Status);
            Assert.IsFalse(runner.Calls.Any(call => call[0] == "set"));
        });
    }

    [TestMethod]
    public void GnomeApply_UpdatesBothStylesWithALocalFileUri() {
        WithImage(path => {
            var assignments = new Dictionary<string, string>();
            var runner = new FakeRunner {
                Execute = args => {
                    if (args[0] == "list-keys") {
                        return "picture-uri\npicture-uri-dark\n";
                    }
                    if (args[0] == "writable") {
                        return "true";
                    }
                    assignments[args[2]] = LinuxDesktopBackend.DecodeGVariantString(args[3]);
                    return string.Empty;
                }
            };
            var result = new PortableDesktopService(Linux(runner)).ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path, Position = DesktopWallpaperPosition.Fit });
            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(new Uri(path).AbsoluteUri, assignments["picture-uri"]);
            Assert.AreEqual(assignments["picture-uri"], assignments["picture-uri-dark"]);
            Assert.AreEqual("scaled", assignments["picture-options"]);
        });
    }

    [TestMethod]
    public void PerDisplayGnomeAndSpanKde_AreUnsupportedBeforeAnyWrite() {
        WithImage(path => {
            var runner = new FakeRunner();
            var gnome = new PortableDesktopService(Linux(runner));
            Assert.AreEqual(DesktopOperationStatus.Unsupported, gnome.ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path, DisplayId = "HDMI-1" }).Status);
            var kde = new PortableDesktopService(Linux(runner, DesktopEnvironment("KDE")));
            Assert.AreEqual(DesktopOperationStatus.Unsupported, kde.ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path, Position = DesktopWallpaperPosition.Span }).Status);
            Assert.AreEqual(0, runner.Calls.Count);
        });
    }

    [TestMethod]
    public void KdeRead_ReportsNonImagePluginsAsUnsupported() {
        var runner = new FakeRunner { Execute = _ => "{\"supported\":false,\"message\":\"The current KDE wallpaper plugin is not a static image.\"}" };
        var state = Linux(runner, DesktopEnvironment("KDE")).GetWallpaper("screen:0");
        Assert.AreEqual(DesktopOperationStatus.Unsupported, state.Status);
        Assert.IsNull(state.FilePath);
    }

    [TestMethod]
    public void KdeRequests_PreserveTargetsUrisAndTypedNullableFields() {
        WithImage(path => {
            var runner = new FakeRunner {
                Execute = arguments => {
                    CollectionAssert.AreEqual(new[] { "org.kde.plasmashell", "/PlasmaShell", "org.kde.PlasmaShell.evaluateScript" }, arguments.Take(3).ToArray());
                    const string prefix = "var request = ";
                    StringAssert.StartsWith(arguments[3], prefix);
                    var end = arguments[3].IndexOf(";\n", StringComparison.Ordinal);
                    Assert.IsGreaterThan(prefix.Length, end);
                    using var document = JsonDocument.Parse(arguments[3].Substring(prefix.Length, end - prefix.Length));
                    var payload = document.RootElement;
                    Assert.AreEqual(4, payload.EnumerateObject().Count());
                    Assert.AreEqual(JsonValueKind.Number, payload.GetProperty("fillMode").ValueKind);
                    if (payload.GetProperty("action").GetString() == "read") {
                        Assert.AreEqual(JsonValueKind.Null, payload.GetProperty("displayId").ValueKind);
                        Assert.AreEqual(JsonValueKind.Null, payload.GetProperty("uri").ValueKind);
                        Assert.AreEqual(0, payload.GetProperty("fillMode").GetInt32());
                        return "{\"supported\":true,\"path\":null,\"fillMode\":2}";
                    }
                    Assert.AreEqual("apply", payload.GetProperty("action").GetString());
                    Assert.AreEqual("screen:3", payload.GetProperty("displayId").GetString());
                    Assert.AreEqual(new Uri(path).AbsoluteUri, payload.GetProperty("uri").GetString());
                    Assert.AreEqual(1, payload.GetProperty("fillMode").GetInt32());
                    return "true";
                }
            };
            var service = new PortableDesktopService(Linux(runner, DesktopEnvironment("KDE")));
            Assert.IsTrue(service.GetWallpaper().Succeeded);
            Assert.IsTrue(service.ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path, DisplayId = "screen:3", Position = DesktopWallpaperPosition.Fit }).Succeeded);
            Assert.AreEqual(2, runner.Calls.Count);
        });
    }

    [TestMethod]
    public void X11Session_RequiresExplicitDimensionsRatherThanUnqualifiedTransformedGeometry() {
        var environment = DesktopEnvironment("GNOME");
        environment.Remove("WAYLAND_DISPLAY");
        environment["XDG_SESSION_TYPE"] = "x11";
        environment["DISPLAY"] = ":0";
        var runner = new FakeRunner { Execute = _ => throw new InvalidOperationException("Unqualified geometry must not be used to size images.") };
        var desktop = Linux(runner, environment);
        Assert.IsTrue(desktop.GetCapabilities().CanSetWallpaper);
        Assert.IsFalse(desktop.GetCapabilities().CanEnumerateDisplays);
        Assert.AreEqual(0, desktop.GetDisplays().Count);
    }

    [TestMethod]
    public void CommandRunner_PreservesArgumentDataWithoutShellEvaluation() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("This process-boundary proof uses the Unix platform printf facility.");
        }
        var arguments = new[] { "a path with spaces", "a'quote\"and\\slashes\\", "$(printf injected); `printf injected`", "two\nlines" };
        var argv = new[] { "[%s]" }.Concat(arguments).ToArray();
        var output = new DesktopCommandRunner().Run("/usr/bin/printf", argv);
        Assert.AreEqual(string.Concat(arguments.Select(argument => "[" + argument + "]")), output);
    }

    [TestMethod]
    public void Factory_ReadOnlyProbeLoadsOnTheCurrentPlatform() {
        var service = new PortableDesktopService();
        var capabilities = service.GetCapabilities();
        Assert.IsFalse(string.IsNullOrWhiteSpace(capabilities.BackendId));
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            Assert.AreEqual("macos", capabilities.BackendId);
            if (capabilities.IsAvailable) {
                var displays = service.GetDisplays();
                Assert.IsGreaterThan(0, displays.Count);
                Assert.IsGreaterThan(0, displays[0].Width);
                var state = service.GetWallpaper(displays[0].Id);
                Assert.IsTrue(state.Succeeded, state.Message);
            }
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))) {
            Assert.IsFalse(capabilities.CanSetWallpaper);
        }
    }

    private static LinuxDesktopBackend Linux(FakeRunner runner, Dictionary<string, string>? values = null) {
        var environment = values ?? DesktopEnvironment("GNOME");
        return new LinuxDesktopBackend(runner, name => environment.TryGetValue(name, out var value) ? value : null);
    }

    private static Dictionary<string, string> DesktopEnvironment(string desktop) => new Dictionary<string, string> {
        ["XDG_CURRENT_DESKTOP"] = desktop,
        ["XDG_SESSION_TYPE"] = "wayland",
        ["WAYLAND_DISPLAY"] = "wayland-0",
        ["DBUS_SESSION_BUS_ADDRESS"] = "unix:path=/run/user/1000/bus"
    };

    private static void WithImage(Action<string> action) {
        var root = Path.Combine(Path.GetTempPath(), "desktop-portable-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "image with 'quotes.jpg");
        try {
            File.WriteAllBytes(path, new byte[] { 1 });
            action(path);
        } finally {
            Directory.Delete(root, true);
        }
    }

    private sealed class FakeRunner : IDesktopCommandRunner {
        internal Func<string[], string> Execute { get; set; } = _ => string.Empty;
        internal List<string[]> Calls { get; } = new List<string[]>();
        public string? FindExecutable(params string[] names) => "/platform/" + names[0];
        public string Run(string executable, params string[] arguments) {
            Calls.Add(arguments);
            return Execute(arguments);
        }
    }
}
