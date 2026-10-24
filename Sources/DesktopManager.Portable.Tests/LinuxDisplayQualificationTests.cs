using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Portable.Tests;

[TestClass]
public sealed class LinuxDisplayQualificationTests {
    // Reduced from the actual GNOME Shell 46 and Plasma 5.27 Xvfb output.
    private const string NativeLayout = """
        Screen 0: minimum 1 x 1, current 1280 x 720, maximum 1280 x 720
        screen connected primary 1280x720+0+0 (0x3a) normal (normal) 0mm x 0mm
            Identifier: 0x3c
            Transform:  1.000000 0.000000 0.000000
                        0.000000 1.000000 0.000000
                        0.000000 0.000000 1.000000
                       filter:
          1280x720 (0x3a) 0.000MHz *current
                h: width 1280 start 0 end 0 total 0 skew 0 clock 0.00KHz
                v: height 720 start 0 end 0 total 0 clock 0.00Hz
        """;

    [TestMethod]
    [DataRow("GNOME")]
    [DataRow("KDE")]
    public void SingleIdentityOutput_ProvidesPhysicalDimensionsToItsWallpaperTarget(string desktop) {
        var runner = new DisplayRunner(NativeLayout);
        var service = Service(runner, desktop);

        var displays = service.GetDisplays();
        Assert.AreEqual(1, displays.Count);
        var display = displays[0];
        Assert.AreEqual(1280, display.Width);
        Assert.AreEqual(720, display.Height);
        Assert.AreEqual(0, display.Left);
        Assert.AreEqual(0, display.Top);
        Assert.IsTrue(service.GetCapabilities().CanEnumerateDisplays);
        var targets = service.GetWallpaperTargets();
        Assert.AreEqual(1, targets.Count);
        var target = targets[0];
        Assert.IsNotNull(target.Display);
        Assert.AreEqual(display.Width, target.Display.Width);
        Assert.AreEqual(display.Height, target.Display.Height);
        Assert.AreEqual(desktop == "GNOME" ? null : "screen:0", target.DisplayId);
        Assert.IsNull(display.Scale, "XRandR does not establish the desktop's UI scaling preference.");
    }

    [TestMethod]
    [DataRow("scaled")]
    [DataRow("rotated")]
    [DataRow("reflected-x")]
    [DataRow("reflected-y")]
    [DataRow("reflected-xy")]
    [DataRow("multiple")]
    [DataRow("missing-transform")]
    [DataRow("mode-mismatch")]
    [DataRow("framebuffer-mismatch")]
    [DataRow("panning")]
    [DataRow("non-desktop")]
    public void UnqualifiedLayouts_KeepWallpaperSupportWithoutInventingGeometry(string variant) {
        var layout = variant switch {
            "scaled" => NativeLayout.Replace("1.000000 0.000000 0.000000", "0.500000 0.000000 0.000000"),
            "rotated" => NativeLayout.Replace("(0x3a) normal", "(0x3a) left"),
            "reflected-x" => NativeLayout.Replace("(0x3a) normal", "(0x3a) normal X axis"),
            "reflected-y" => NativeLayout.Replace("(0x3a) normal", "(0x3a) normal Y axis"),
            "reflected-xy" => NativeLayout.Replace("(0x3a) normal", "(0x3a) normal X and Y axis"),
            "multiple" => NativeLayout + "\nHDMI-1 connected 1920x1080+1280+0 normal\n",
            "missing-transform" => NativeLayout.Replace("Transform:", "UnknownMatrix:"),
            "mode-mismatch" => NativeLayout.Replace("h: width 1280", "h: width 2560"),
            "framebuffer-mismatch" => NativeLayout.Replace("current 1280 x 720", "current 2560 x 720"),
            "panning" => NativeLayout + "\n    Panning: 2560x720+0+0\n",
            "non-desktop" => NativeLayout + "\n    non-desktop: 1\n",
            _ => throw new ArgumentException(variant)
        };
        var service = Service(new DisplayRunner(layout), "GNOME");

        Assert.AreEqual(0, service.GetDisplays().Count);
        Assert.IsFalse(service.GetCapabilities().CanEnumerateDisplays);
        Assert.IsTrue(service.GetCapabilities().CanSetWallpaper);
        Assert.IsNull(service.GetWallpaperTargets().Single().Display);
    }

    [TestMethod]
    public void DisconnectedOutputMetadata_CannotSupplyAnActiveOutputsMissingTransform() {
        var disconnected = NativeLayout.Replace("screen connected primary", "DP-1 disconnected");
        var activeWithoutTransform = NativeLayout.Substring(NativeLayout.IndexOf("screen connected", StringComparison.Ordinal))
            .Replace("Transform:", "UnknownMatrix:");
        var service = Service(new DisplayRunner(disconnected + "\n" + activeWithoutTransform), "GNOME");

        Assert.AreEqual(0, service.GetDisplays().Count);
        Assert.IsFalse(service.GetCapabilities().CanEnumerateDisplays);
    }

    [TestMethod]
    public void WaylandOrMissingXrandr_DoesNotQueryX11OrInventGeometry() {
        foreach (var wayland in new[] { false, true }) {
            var runner = new DisplayRunner(NativeLayout) { HasXrandr = wayland };
            var service = Service(runner, "GNOME", wayland);

            Assert.AreEqual(0, service.GetDisplays().Count);
            Assert.IsFalse(service.GetCapabilities().CanEnumerateDisplays);
            Assert.AreEqual(0, runner.DisplayReads);
        }
    }

    [TestMethod]
    public void FailedNativeDisplayQuery_RemainsAnObservableReadFailure() {
        var runner = new DisplayRunner(NativeLayout) { QueryFailure = new IOException("XRandR could not read this display.") };
        var service = Service(runner, "GNOME");

        var failure = Assert.ThrowsExactly<IOException>(() => service.GetDisplays());
        Assert.AreSame(runner.QueryFailure, failure);
        Assert.IsFalse(service.GetCapabilities().CanEnumerateDisplays);
        Assert.IsTrue(service.GetCapabilities().CanSetWallpaper);
    }

    private static PortableDesktopService Service(DisplayRunner runner, string desktop, bool wayland = false) {
        var environment = new Dictionary<string, string> {
            ["XDG_CURRENT_DESKTOP"] = desktop, ["XDG_SESSION_TYPE"] = wayland ? "wayland" : "x11",
            ["DISPLAY"] = ":101", ["DBUS_SESSION_BUS_ADDRESS"] = "unix:path=/fixture/session-bus"
        };
        return new PortableDesktopService(new LinuxDesktopBackend(runner, name => environment.TryGetValue(name, out var value) ? value : null));
    }

    private sealed class DisplayRunner(string layout) : IDesktopCommandRunner {
        internal bool HasXrandr { get; set; } = true;
        internal int DisplayReads { get; private set; }
        internal IOException? QueryFailure { get; set; }

        public string? FindExecutable(params string[] names) => names[0] == "xrandr" && !HasXrandr ? null : "/platform/" + names[0];

        public string Run(string executable, params string[] arguments) {
            if (executable.EndsWith("/xrandr", StringComparison.Ordinal)) {
                CollectionAssert.AreEqual(new[] { "--current", "--verbose" }, arguments);
                DisplayReads++;
                if (QueryFailure != null) throw QueryFailure;
                return layout;
            }
            if (executable.EndsWith("/qdbus6", StringComparison.Ordinal)) {
                return "[{\"displayId\":\"screen:0\",\"activity\":\"native-scope\"}]";
            }
            throw new InvalidOperationException("Unexpected native read: " + executable);
        }
    }
}
