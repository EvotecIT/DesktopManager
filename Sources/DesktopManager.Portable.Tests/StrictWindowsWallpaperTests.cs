using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager.Portable.Tests;

// These tests use the real Windows owner with a fake COM boundary and a recording
// session-wide fallback. No native desktop write can run. The desktop-wide case
// can perform read-only Windows display enumeration before reaching the fake setter.
[TestClass]
[SupportedOSPlatform("windows")]
public sealed class StrictWindowsWallpaperTests {
    [TestMethod]
    public void NamedTargetFailure_DoesNotWidenToEveryDisplay() {
        WithImageAndHistory((path, history) => {
            var native = new RejectingDesktopManager();
            var owner = new RecordingMonitorService(native);
            var backend = new WindowsDesktopBackend(owner, () => Snapshots("named-display"), () => true);
            var result = new PortableDesktopService(backend).ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path, DisplayId = "named-display" });
            Assert.AreEqual(DesktopOperationStatus.Failed, result.Status);
            Assert.AreEqual(0, owner.SessionWallpaperCalls.Count);
            Assert.AreEqual(("named-display", path), native.SetWallpaperCalls.Single());
            Assert.AreEqual(DesktopWallpaperPosition.Center, native.WallpaperPosition);
            Assert.IsFalse(File.Exists(history));
        });
    }

    [TestMethod]
    public void LegacyTargetFailure_RetainsItsExistingSessionFallback() {
        WithImageAndHistory((path, _) => {
            var owner = new RecordingMonitorService(new RejectingDesktopManager());
            owner.SetWallpaper("named-display", path);
            CollectionAssert.AreEqual(new[] { path }, owner.SessionWallpaperCalls);
        });
    }

    [TestMethod]
    public void DesktopWideImageFailure_DoesNotChangeTheGlobalFitSetting() {
        WithImageAndHistory((path, history) => {
            var native = new RejectingDesktopManager();
            var owner = new RecordingMonitorService(native) { RejectSessionWallpaper = true };
            var backend = new WindowsDesktopBackend(owner, () => Snapshots("named-display"), () => true);
            var result = new PortableDesktopService(backend).ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path });
            Assert.AreEqual(DesktopOperationStatus.Failed, result.Status);
            Assert.AreEqual(DesktopWallpaperPosition.Center, native.WallpaperPosition);
            Assert.IsFalse(File.Exists(history));
        });
    }

    [TestMethod]
    public void AnonymousTarget_IsUnsupportedEvenWhenAnotherDisplayIsAddressable() {
        WithImageAndHistory((path, _) => {
            foreach (var namedDisplayAvailable in new[] { false, true }) {
                var native = new RejectingDesktopManager();
                var owner = new RecordingMonitorService(native);
                var snapshots = namedDisplayAvailable ? Snapshots(string.Empty, "named-display") : Snapshots(string.Empty);
                var backend = new WindowsDesktopBackend(owner, () => snapshots, () => true);
                Assert.AreEqual(namedDisplayAvailable, backend.GetCapabilities().SupportsPerDisplayWallpaper);
                foreach (var anonymousId in new[] { "index:0", "INDEX:0" }) {
                    var result = new PortableDesktopService(backend).ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path, DisplayId = anonymousId });
                    Assert.AreEqual(DesktopOperationStatus.Unsupported, result.Status);
                    Assert.AreEqual(0, native.SetWallpaperCalls.Count);
                    Assert.AreEqual(0, owner.SessionWallpaperCalls.Count);
                    Assert.IsFalse(native.EnableCalled);
                    Assert.AreEqual(DesktopWallpaperPosition.Center, native.WallpaperPosition);
                }
            }
        });
    }

    [TestMethod]
    public void NamedReadFailure_DoesNotSubstituteASessionImageOrCachedValue() {
        var native = new RejectingDesktopManager();
        var owner = new RecordingMonitorService(native);
        var backend = new WindowsDesktopBackend(owner, () => Snapshots("named-display"), () => true);
        var result = new PortableDesktopService(backend).GetWallpaper("named-display");
        Assert.AreEqual(DesktopOperationStatus.Failed, result.Status);
        Assert.IsNull(result.FilePath);
        StringAssert.Contains(result.Message, "GetWallpaper");
    }

    [TestMethod]
    public void SnapshotFailure_DisablesTargetedCapabilityWithoutDisablingDesktopWideSupport() {
        var native = new RejectingDesktopManager();
        var backend = new WindowsDesktopBackend(new RecordingMonitorService(native),
            () => throw new PlatformNotSupportedException("Physical snapshots are unavailable."), () => true);
        var capabilities = backend.GetCapabilities();
        Assert.IsTrue(capabilities.CanSetWallpaper);
        Assert.IsFalse(capabilities.SupportsPerDisplayWallpaper);
        Assert.IsFalse(capabilities.CanEnumerateDisplays);
        Assert.IsFalse(native.EnableCalled);
    }

    private static IReadOnlyList<MonitorSnapshot> Snapshots(params string[] identifiers) => identifiers.Select((id, index) =>
        new MonitorSnapshot(index, id, "display" + index, index == 0, new RECT { Right = 1920, Bottom = 1080 }, 96, 96)).ToArray();

    private static void WithImageAndHistory(Action<string, string> action) {
        var root = Path.Combine(Path.GetTempPath(), "desktop-strict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var image = Path.Combine(root, "image.png");
        var history = Path.Combine(root, "history.json");
        var previousHistory = Environment.GetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH");
        try {
            File.WriteAllBytes(image, new byte[] { 1 });
            Environment.SetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH", history);
            action(image, history);
        } finally {
            Environment.SetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH", previousHistory);
            Directory.Delete(root, true);
        }
    }

    private sealed class RecordingMonitorService : MonitorService {
        internal RecordingMonitorService(IDesktopManager native) : base(native) { }
        internal List<string> SessionWallpaperCalls { get; } = new List<string>();
        internal bool RejectSessionWallpaper { get; set; }
        internal override void SetSystemWallpaper(string path) {
            SessionWallpaperCalls.Add(path);
            if (RejectSessionWallpaper) {
                throw new IOException("The native wallpaper setter rejected the image.");
            }
        }
    }

    private sealed class RejectingDesktopManager : global::DesktopManager.Tests.FakeDesktopManager, IDesktopManager {
        void IDesktopManager.SetWallpaper(string monitorId, string wallpaper) {
            SetWallpaperCalls.Add((monitorId, wallpaper));
            throw new COMException("A display disappeared during the request.");
        }
        string IDesktopManager.GetWallpaper(string monitorId) => throw new COMException("A display disappeared during readback.");
    }
}
