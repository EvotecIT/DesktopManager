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

    [TestMethod]
    public void WindowsCheckpoint_CaptureIsReadOnlyAndRestoresNativeImageFitColorOptionsAndEnabledState() {
        WithImageAndHistory((path, history) => {
            var native = new CheckpointDesktopManager(path) {
                WallpaperPosition = DesktopWallpaperPosition.Fit, BackgroundColor = 0x00224466, SlideshowTick = 3000,
                SlideshowState = DesktopSlideshowState.None
            };
            var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true));
            using var snapshot = service.CaptureWallpaperState();
            Assert.IsTrue(snapshot.Succeeded, snapshot.Message);
            Assert.AreEqual(0, native.SetWallpaperCalls.Count);
            Assert.AreEqual(0, native.SetSlideshowCalls.Count);
            Assert.IsFalse(native.EnableCalled);
            native.WallpaperPosition = DesktopWallpaperPosition.Stretch;
            native.BackgroundColor = 0;
            native.SlideshowTick = 9999;
            native.SlideshowState = DesktopSlideshowState.Enabled;
            var result = service.RestoreWallpaperState(snapshot);
            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(DesktopWallpaperPosition.Fit, native.WallpaperPosition);
            Assert.AreEqual((uint)0x00224466, native.BackgroundColor);
            Assert.AreEqual((uint)3000, native.SlideshowTick);
            Assert.AreEqual(DesktopSlideshowState.None, native.SlideshowState);
            Assert.AreEqual(("display", path), native.SetWallpaperCalls.Single());
            Assert.IsFalse(File.Exists(history), "Checkpoint restoration must not add generated history.");
        });
    }

    [TestMethod]
    public void WindowsCheckpoint_RefusesChangedTopologyMissingSourceAndDisposedCheckpointBeforeWriting() {
        foreach (var rejection in new[] { "topology", "file", "disposed" }) {
            WithImageAndHistory((path, _) => {
                var native = new CheckpointDesktopManager(path);
                var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true));
                using var snapshot = service.CaptureWallpaperState();
                Assert.IsTrue(snapshot.Succeeded, snapshot.Message);
                if (rejection == "topology") { native.DevicePaths[0] = "replacement"; }
                if (rejection == "file") { File.Delete(path); }
                if (rejection == "disposed") { snapshot.Dispose(); }
                Assert.IsFalse(service.RestoreWallpaperState(snapshot).Succeeded, rejection);
                Assert.AreEqual(0, native.SetWallpaperCalls.Count, rejection);
                Assert.AreEqual(0, native.SetSlideshowCalls.Count, rejection);
                Assert.IsFalse(native.EnableCalled, rejection);
            });
        }
    }

    [TestMethod]
    public void WindowsCheckpoint_NativeReadFailureCannotBecomeAFallbackSnapshot() {
        WithImageAndHistory((path, _) => {
            var native = new CheckpointDesktopManager(path) { SlideshowReadResult = 0x80004005 };
            var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true));
            using var snapshot = service.CaptureWallpaperState();
            Assert.AreEqual(DesktopOperationStatus.Failed, snapshot.Status);
            StringAssert.Contains(snapshot.Message, "GetSlideshow");
            Assert.IsFalse(service.RestoreWallpaperState(snapshot).Succeeded);
            Assert.AreEqual(0, native.SetWallpaperCalls.Count);
        });
    }

    [TestMethod]
    public void WindowsCheckpoint_NoAssignedImageIsRestorableAndInactiveAnonymousSlotsAreIgnored() {
        var native = new CheckpointDesktopManager(string.Empty) { DevicePathCount = 2 };
        var geometry = Snapshots("display").ToList();
        geometry.Add(new MonitorSnapshot(1, string.Empty, string.Empty, false, default, null, null));
        var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => geometry, () => true));
        using var snapshot = service.CaptureWallpaperState();
        Assert.IsTrue(snapshot.Succeeded, snapshot.Message);
        var result = service.RestoreWallpaperState(snapshot);
        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreEqual(("display", string.Empty), native.SetWallpaperCalls.Single());
    }

    [TestMethod]
    public void WindowsCheckpoint_SFalseIsAcceptedOnlyWhenNoSlideshowIsConfigured() {
        foreach (var configured in new[] { false, true }) {
            var native = new CheckpointDesktopManager(string.Empty) { SlideshowReadResult = 1,
                SlideshowState = configured ? DesktopSlideshowState.Slideshow : DesktopSlideshowState.Enabled };
            var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true));
            using var snapshot = service.CaptureWallpaperState();
            Assert.AreEqual(!configured, snapshot.Succeeded);
            Assert.AreEqual(0, native.SetWallpaperCalls.Count);
        }
    }

    [TestMethod]
    public void WindowsCheckpoint_FailedImageRestoreStillRestoresGlobalSettingsAndReportsFailure() {
        WithImageAndHistory((path, _) => {
            var native = new CheckpointDesktopManager(path) { WallpaperPosition = DesktopWallpaperPosition.Fit, BackgroundColor = 42 };
            var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true));
            using var snapshot = service.CaptureWallpaperState();
            Assert.IsTrue(snapshot.Succeeded, snapshot.Message);
            native.RejectRestore = true;
            native.WallpaperPosition = DesktopWallpaperPosition.Stretch;
            native.BackgroundColor = 999;
            var result = service.RestoreWallpaperState(snapshot);
            Assert.AreEqual(DesktopOperationStatus.Failed, result.Status);
            Assert.AreEqual(DesktopWallpaperPosition.Fit, native.WallpaperPosition);
            Assert.AreEqual((uint)42, native.BackgroundColor);
            Assert.IsTrue(native.EnableCalled);
        });
    }

    [TestMethod]
    public void SlideshowBuilder_UsesTheRealNativeCollectionWithoutChangingTheDesktop() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("This proof requires the Windows Shell collection boundary.");
        }
        WithImageAndHistory((path, _) => {
            var native = new SlideshowCollectionReader();
            var owner = new MonitorService(native);
            owner.StartWallpaperSlideshow(new[] { path.Replace('\\', '/') });
            Assert.AreEqual((uint)1, native.ItemCount);
            Assert.AreEqual(Path.GetFullPath(path), native.ItemPath);
        });
    }

    [TestMethod]
    public void SlideshowBuilder_InvalidPathDoesNotEnableTheDesktopBeforeValidation() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("This proof requires the Windows Shell collection boundary.");
        }
        WithImageAndHistory((path, _) => {
            var native = new SlideshowCollectionReader();
            var owner = new MonitorService(native);
            Assert.ThrowsExactly<InvalidOperationException>(() => owner.StartWallpaperSlideshow(new[] { path + ".missing" }));
            Assert.IsFalse(native.EnableCalled);
        });
    }

    [TestMethod]
    public void NativeObjectCollection_ReservesInheritedSlotsBeforeAddingShellItems() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("This proof requires the Windows Shell collection boundary.");
        }
        WithImageAndHistory((path, _) => {
            var type = Type.GetTypeFromCLSID(new Guid("2d3468c1-36a7-43b6-ac24-d3f02fd9607a"))!;
            var collection = (MonitorNativeMethods.IObjectCollection)Activator.CreateInstance(type)!;
            var itemId = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");
            Marshal.ThrowExceptionForHR(MonitorNativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, ref itemId, out var pointer));
            var item = Marshal.GetObjectForIUnknown(pointer);
            try {
                collection.AddObject(item);
                Assert.AreEqual((uint)1, collection.GetCount());
                var retrieved = collection.GetAt(0, ref itemId);
                Marshal.ReleaseComObject(retrieved);
                collection.Clear();
                Assert.AreEqual((uint)0, collection.GetCount());
            } finally {
                Marshal.ReleaseComObject(item);
                Marshal.Release(pointer);
                Marshal.ReleaseComObject(collection);
            }
        });
    }

    [TestMethod]
    public void WindowsBackground_ConvertsColorRefAndDoesNotExposeDisabledRetainedImages() {
        foreach (var enabled in new[] { false, true }) {
            var native = new CheckpointDesktopManager("retained.jpg") { BackgroundColor = 0x00332211,
                SlideshowState = enabled ? DesktopSlideshowState.Enabled : DesktopSlideshowState.None };
            var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true));
            var state = service.GetWallpaper("display");
            Assert.IsTrue(state.Succeeded, state.Message);
            Assert.AreEqual(enabled ? "retained.jpg" : null, state.FilePath);
            Assert.AreEqual(enabled ? 1.0 : 0.0, state.ImageOpacity);
            Assert.AreEqual("#112233", state.Background!.PrimaryColor);
            Assert.AreEqual(DesktopBackgroundShading.Solid, state.Background.Shading);
            Assert.AreEqual(0, native.SetWallpaperCalls.Count);
            Assert.IsFalse(native.EnableCalled);
        }
    }

    [TestMethod]
    public void WindowsSelectedSolidColor_HidesRetainedImageButRejectsItsUnqualifiedCheckpointBeforeWrites() {
        var native = new CheckpointDesktopManager("retained-unavailable-image.jpg") { BackgroundColor = 0x00332211 };
        var selection = new RecordingSelection { Mode = 1 };
        var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true, selection));
        var state = service.GetWallpaper("display");
        Assert.IsTrue(state.Succeeded, state.Message);
        Assert.IsNull(state.FilePath);
        Assert.AreEqual(0.0, state.ImageOpacity);
        Assert.AreEqual("#112233", state.Background!.PrimaryColor);
        using var checkpoint = service.CaptureWallpaperState();
        Assert.AreEqual(DesktopOperationStatus.Unsupported, checkpoint.Status);
        Assert.IsFalse(service.RestoreWallpaperState(checkpoint).Succeeded);
        Assert.AreEqual(0, native.SetWallpaperCalls.Count);
        Assert.AreEqual(0, native.SetSlideshowCalls.Count);
        Assert.AreEqual(1, selection.Mode);
        Assert.IsFalse(selection.CheckedWritable);
    }

    [TestMethod]
    public void WindowsSelectedSolidColor_WithoutRetainedImagesRestoresItsEmptyAssignmentsAfterInactiveSources() {
        var native = new CheckpointDesktopManager(string.Empty);
        var selection = new RecordingSelection { Mode = 1 };
        var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true, selection));
        using var checkpoint = service.CaptureWallpaperState();
        Assert.IsTrue(checkpoint.Succeeded, checkpoint.Message);
        selection.Mode = 0;
        var restored = service.RestoreWallpaperState(checkpoint);
        Assert.IsTrue(restored.Succeeded, restored.Message);
        Assert.AreEqual(("display", string.Empty), native.SetWallpaperCalls.Single());
        Assert.AreEqual(1, selection.Mode);
        Assert.AreEqual("items,image", string.Join(",", native.RestoreOrder.TakeLast(2)));
    }

    [TestMethod]
    public void WindowsExplicitImage_UpdatesSelectedModeOnlyAfterSuccessfulAssignmentAndFit() {
        WithImageAndHistory((path, _) => {
            foreach (var fail in new[] { false, true }) {
                var native = new CheckpointDesktopManager(string.Empty) { RejectRestore = fail };
                var selection = new RecordingSelection { Mode = 1 };
                var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true, selection));
                var result = service.ApplyWallpaper(new DesktopWallpaperRequest { FilePath = path, DisplayId = "display", Position = DesktopWallpaperPosition.Fit });
                Assert.AreEqual(!fail, result.Succeeded);
                Assert.AreEqual(fail ? 1 : 0, selection.Mode);
                Assert.IsTrue(selection.CheckedWritable);
            }
        });
    }

    [TestMethod]
    public void WindowsDynamicOrUnknownSelection_DoesNotTreatRetainedImageAsEffectiveOrRestorable() {
        foreach (var mode in new int?[] { null, 3, 7 }) {
            var native = new CheckpointDesktopManager("retained.jpg");
            var service = new PortableDesktopService(new WindowsDesktopBackend(new MonitorService(native), () => Snapshots("display"), () => true,
                new RecordingSelection { Mode = mode }));
            Assert.AreEqual(DesktopOperationStatus.Unsupported, service.GetWallpaper("display").Status);
            using var checkpoint = service.CaptureWallpaperState();
            Assert.AreEqual(DesktopOperationStatus.Unsupported, checkpoint.Status);
            Assert.AreEqual(0, native.SetWallpaperCalls.Count);
            Assert.IsFalse(native.EnableCalled);
        }
    }

    private sealed class RecordingSelection : WindowsWallpaperSelection {
        internal int? Mode;
        internal bool CheckedWritable;
        internal override int? Read() => Mode;
        internal override void EnsureWritable() { CheckedWritable = true; }
        internal override void Set(int mode) { Mode = mode; }
    }

    private sealed class SlideshowCollectionReader : global::DesktopManager.Tests.FakeDesktopManager, IDesktopManager {
        internal uint ItemCount { get; private set; }
        internal string ItemPath { get; private set; } = string.Empty;
        void IDesktopManager.SetSlideshow(IntPtr pointer) {
            var array = (MonitorNativeMethods.IShellItemArray)Marshal.GetObjectForIUnknown(pointer);
            try {
                Marshal.ThrowExceptionForHR(array.GetCount(out var count));
                ItemCount = count;
                Marshal.ThrowExceptionForHR(array.GetItemAt(0, out var item));
                try {
                    Marshal.ThrowExceptionForHR(item.GetDisplayName(MonitorNativeMethods.SIGDN.FileSystemPath, out var name));
                    try { ItemPath = Marshal.PtrToStringUni(name)!; } finally { Marshal.FreeCoTaskMem(name); }
                } finally { Marshal.ReleaseComObject(item); }
            } finally { Marshal.ReleaseComObject(array); }
        }
    }

    private sealed class CheckpointDesktopManager : global::DesktopManager.Tests.FakeDesktopManager, IDesktopManager {
        private readonly Dictionary<string, string> _paths = new Dictionary<string, string>();
        internal bool RejectRestore { get; set; }
        internal uint SlideshowReadResult { get; set; }
        internal List<string> RestoreOrder { get; } = new List<string>();
        internal CheckpointDesktopManager(string path) {
            DevicePaths[0] = "display";
            _paths["display"] = path;
        }
        string IDesktopManager.GetWallpaper(string id) => _paths[id];
        void IDesktopManager.SetWallpaper(string id, string path) {
            RestoreOrder.Add("image");
            SetWallpaperCalls.Add((id, path));
            if (RejectRestore) { throw new COMException("Native image restore failed."); }
            _paths[id] = path;
        }
        uint IDesktopManager.GetSlideshow(out IntPtr pointer) { pointer = IntPtr.Zero; return SlideshowReadResult; }
        void IDesktopManager.SetSlideshow(IntPtr pointer) { SetSlideshowCalls.Add(pointer); RestoreOrder.Add("items"); }
        void IDesktopManager.Enable(bool enable) {
            EnableCalled = true;
            SlideshowState = enable ? SlideshowState | DesktopSlideshowState.Enabled : SlideshowState & ~DesktopSlideshowState.Enabled;
        }
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
