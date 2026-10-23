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
        Assert.AreEqual(0, backend.GetWallpaperTargets().Count);
        Assert.AreEqual(0, backend.GetCapabilities().SupportedWallpaperPositions.Count);
        Assert.AreEqual(0, runner.Calls.Count);
    }

    [TestMethod]
    public void WallpaperTargets_AreDiscoverableWithoutInventingLinuxPixelGeometry() {
        var gnome = new PortableDesktopService(Linux(new FakeRunner()));
        var desktop = gnome.GetWallpaperTargets().Single();
        Assert.IsTrue(gnome.GetCapabilities().CanEnumerateWallpaperTargets);
        Assert.IsNull(desktop.DisplayId);
        Assert.IsNull(desktop.Display);

        var runner = new FakeRunner {
            Execute = args => {
                StringAssert.Contains(args[3], "\"action\":\"targets\"");
                return "[{\"displayId\":\"screen:2\",\"activity\":\"work\"},{\"displayId\":\"screen:7\",\"activity\":\"work\"}]";
            }
        };
        var kde = new PortableDesktopService(Linux(runner, DesktopEnvironment("KDE")));
        var targets = kde.GetWallpaperTargets();
        Assert.IsFalse(kde.GetCapabilities().CanEnumerateDisplays);
        CollectionAssert.AreEqual(new[] { "screen:2", "screen:7" }, targets.Select(target => target.DisplayId).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1 }, targets.Select(target => target.Index).ToArray());
        Assert.IsTrue(targets.All(target => target.ScopeId == "work" && target.Display == null));
        Assert.AreEqual(1, runner.Calls.Count);
    }

    [TestMethod]
    public void KdeWallpaperTargets_RejectMalformedOrDuplicateNativeIdentities() {
        foreach (var payload in new[] {
            "[{\"displayId\":\"screen:-1\",\"activity\":\"work\"}]",
            "[{\"displayId\":\"screen:2\",\"activity\":\"work\"},{\"displayId\":\"screen:2\",\"activity\":\"work\"}]"
        }) {
            var runner = new FakeRunner { Execute = _ => payload };
            Assert.ThrowsExactly<FormatException>(() => Linux(runner, DesktopEnvironment("KDE")).GetWallpaperTargets());
        }
    }

    [TestMethod]
    public void SupportedWallpaperPositions_DescribePlatformLimitsAndAreImmutable() {
        var kde = Linux(new FakeRunner(), DesktopEnvironment("KDE")).GetCapabilities();
        CollectionAssert.AreEquivalent(new[] { DesktopWallpaperPosition.Center, DesktopWallpaperPosition.Tile,
            DesktopWallpaperPosition.Stretch, DesktopWallpaperPosition.Fit, DesktopWallpaperPosition.Fill }, kde.SupportedWallpaperPositions.ToArray());
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<DesktopWallpaperPosition>)kde.SupportedWallpaperPositions)[0] = DesktopWallpaperPosition.Span);
        var runner = new FakeRunner { Execute = _ => Environment.UserName };
        var mac = new MacDesktopBackend(runner, _ => null).GetCapabilities();
        CollectionAssert.AreEquivalent(new[] { DesktopWallpaperPosition.Center, DesktopWallpaperPosition.Stretch,
            DesktopWallpaperPosition.Fit, DesktopWallpaperPosition.Fill }, mac.SupportedWallpaperPositions.ToArray());
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
    public void GnomeDisabledImage_DoesNotExposeTheRetainedLightOrDarkUri() {
        var runner = new FakeRunner {
            Execute = arguments => arguments[0] == "list-keys" ? "picture-uri\npicture-uri-dark\npicture-options\n"
                : arguments[2] == "color-scheme" ? "'prefer-dark'"
                : arguments[2] == "picture-options" ? "'none'"
                : "'file:///home/alice/Pictures/hidden.jpg'"
        };
        var state = new PortableDesktopService(Linux(runner)).GetWallpaper();
        Assert.AreEqual(DesktopOperationStatus.Success, state.Status);
        Assert.IsNull(state.FilePath, "An image disabled by GNOME must not become a consumer's background.");
        Assert.IsNull(state.Position);
    }

    [TestMethod]
    public void GnomeBackground_ReadsSolidAndGradientColorsWithoutInventingAnImage() {
        foreach (var shading in new[] { "solid", "horizontal", "vertical" }) {
            var runner = new FakeRunner { Execute = args => args[0] == "list-keys"
                ? "primary-color\nsecondary-color\ncolor-shading-type\npicture-opacity\n"
                : args[2] == "picture-options" ? "'none'"
                : args[2] == "color-shading-type" ? "'" + shading + "'"
                : args[2] == "primary-color" ? "'#123456'"
                : args[2] == "secondary-color" ? "'#abcdef'"
                : args[2] == "picture-opacity" ? "35"
                : throw new InvalidOperationException("A disabled background image must not be read.") };
            var state = new PortableDesktopService(Linux(runner)).GetWallpaper();
            Assert.IsTrue(state.Succeeded, state.Message);
            Assert.IsNull(state.FilePath);
            Assert.AreEqual(0.0, state.ImageOpacity);
            Assert.AreEqual("#123456", state.Background!.PrimaryColor);
            Assert.AreEqual(shading == "solid" ? null : "#abcdef", state.Background.SecondaryColor);
            Assert.AreEqual(shading, state.Background.Shading.ToString().ToLowerInvariant());
            Assert.IsFalse(runner.Calls.Any(call => call[0] == "set"));
        }
    }

    [TestMethod]
    public void GnomeRetainedOpacity_IsNotPresentedAsEffectiveShellOpacity() {
        foreach (var percentage in new[] { "35", "101" }) {
            var runner = new FakeRunner { Execute = args => args[0] == "list-keys" ? "picture-uri\npicture-opacity\n"
                : args[2] == "picture-options" ? "'scaled'"
                : args[2] == "picture-opacity" ? percentage
                : "'file:///photos/original.jpg'" };
            var state = new PortableDesktopService(Linux(runner)).GetWallpaper();
            Assert.IsTrue(state.Succeeded, state.Message);
            Assert.IsNull(state.ImageOpacity);
            Assert.AreEqual("/photos/original.jpg", state.FilePath);
            Assert.IsFalse(runner.Calls.Any(call => call.Length > 2 && call[2] == "picture-opacity"));
        }
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
    public void KdeBlurredMargins_AreUnsupportedForFitAndCenterWithoutRejectingFill() {
        foreach (var fill in new[] { 1, 2, 6 }) {
            foreach (var blur in new[] { false, true }) {
                var runner = new FakeRunner { Execute = _ => JsonSerializer.Serialize(new {
                    supported = true, path = "/photos/current.jpg", fillMode = fill, blur, color = "#112233"
                }) };
                var state = new PortableDesktopService(Linux(runner, DesktopEnvironment("KDE"))).GetWallpaper("screen:0");
                if (blur && fill != 2) {
                    Assert.AreEqual(DesktopOperationStatus.Unsupported, state.Status);
                    Assert.IsNull(state.FilePath);
                    Assert.IsNull(state.Background);
                    StringAssert.Contains(state.Message, "blurred");
                } else {
                    Assert.IsTrue(state.Succeeded, state.Message);
                    Assert.AreEqual("#112233", state.Background!.PrimaryColor);
                }
                Assert.AreEqual(1, runner.Calls.Count, "Readback must not write configuration.");
            }
        }
    }

    [TestMethod]
    public void KdeRead_PackageDirectoryIsUnsupportedButOrdinaryImageRemainsReadable() {
        WithImage(path => {
            foreach (var source in new[] { path, Path.GetDirectoryName(path)! }) {
                var runner = new FakeRunner {
                    Execute = _ => JsonSerializer.Serialize(new { supported = true, path = new Uri(source).AbsoluteUri, fillMode = 2 })
                };
                var state = new PortableDesktopService(Linux(runner, DesktopEnvironment("KDE"))).GetWallpaper("screen:0");
                if (Directory.Exists(source)) {
                    Assert.AreEqual(DesktopOperationStatus.Unsupported, state.Status);
                    Assert.IsNull(state.FilePath);
                    StringAssert.Contains(state.Message, "package");
                } else {
                    Assert.IsTrue(state.Succeeded, state.Message);
                    Assert.AreEqual(source, state.FilePath);
                    Assert.AreEqual(DesktopWallpaperPosition.Fill, state.Position);
                }
            }
        });
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

    [TestMethod]
    public void GnomeCheckpoint_RestoresBothOriginalUrisAndDisabledFitWithoutReadingImageFiles() {
        var values = new Dictionary<string, string> {
            ["picture-uri"] = "'file:///photos/original%20light.jpg'",
            ["picture-uri-dark"] = "'file:///photos/original%20dark.jpg'",
            ["picture-options"] = "'none'"
        };
        var original = new Dictionary<string, string>(values);
        var runner = new FakeRunner { Execute = args => {
            if (args[0] == "list-keys") { return string.Join("\n", values.Keys); }
            if (args[0] == "writable") { return "true"; }
            if (args[0] == "get") { return values[args[2]]; }
            values[args[2]] = args[3];
            return string.Empty;
        } };
        var service = new PortableDesktopService(Linux(runner));
        var snapshot = service.CaptureWallpaperState();
        Assert.IsTrue(snapshot.Succeeded, snapshot.Message);
        Assert.IsFalse(runner.Calls.Any(call => call[0] == "set"));
        foreach (var key in values.Keys.ToArray()) { values[key] = "'changed'"; }
        var restored = service.RestoreWallpaperState(snapshot);
        Assert.IsTrue(restored.Succeeded, restored.Message);
        foreach (var key in original.Keys) { Assert.AreEqual(original[key], values[key], key); }
    }

    [TestMethod]
    public void GnomeCheckpoint_RefusesForeignServiceChangedSessionAndLockedSettingsBeforeWriting() {
        foreach (var rejection in new[] { "foreign", "session", "locked" }) {
            var locked = false;
            var environment = DesktopEnvironment("GNOME");
            var runner = new FakeRunner { Execute = args => args[0] == "list-keys" ? "picture-uri\npicture-uri-dark\n"
                : args[0] == "writable" ? (locked && args[2] == "picture-uri-dark" ? "false" : "true")
                : args[0] == "get" ? "'original'" : throw new InvalidOperationException("No write is permitted.") };
            var service = new PortableDesktopService(Linux(runner, environment));
            var snapshot = service.CaptureWallpaperState();
            Assert.IsTrue(snapshot.Succeeded, snapshot.Message);
            if (rejection == "foreign") { service = new PortableDesktopService(Linux(runner, environment)); }
            if (rejection == "session") { environment["DBUS_SESSION_BUS_ADDRESS"] = "another-bus"; }
            if (rejection == "locked") { locked = true; }
            Assert.IsFalse(service.RestoreWallpaperState(snapshot).Succeeded, rejection);
            Assert.IsFalse(runner.Calls.Any(call => call[0] == "set"), rejection);
        }
    }

    [TestMethod]
    public void GnomeCheckpoint_AttemptsRemainingKeysAndReportsFailedNativeReadback() {
        var restoring = false;
        var runner = new FakeRunner { Execute = args => {
            if (args[0] == "list-keys") { return "picture-uri\npicture-uri-dark\n"; }
            if (args[0] == "writable") { return "true"; }
            if (args[0] == "get") { return restoring ? "'unrestored'" : "'original'"; }
            if (args[2] == "picture-uri") { throw new InvalidOperationException("Native write failed."); }
            return string.Empty;
        } };
        var service = new PortableDesktopService(Linux(runner));
        var snapshot = service.CaptureWallpaperState();
        Assert.IsTrue(snapshot.Succeeded, snapshot.Message);
        restoring = true;
        var result = service.RestoreWallpaperState(snapshot);
        Assert.AreEqual(DesktopOperationStatus.Failed, result.Status);
        CollectionAssert.AreEqual(new[] { "picture-uri", "picture-uri-dark", "picture-options" },
            runner.Calls.Where(call => call[0] == "set").Select(call => call[2]).ToArray());
        StringAssert.Contains(result.Message, "readback");
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
