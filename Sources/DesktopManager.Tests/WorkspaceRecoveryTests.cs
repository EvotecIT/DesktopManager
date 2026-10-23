using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager.Tests;

[TestClass]
#if NET8_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
public sealed class WorkspaceRecoveryTests {
    [TestMethod]
    public void Capture_DesktopOptInPreservesGuidAndPinState() {
        Guid desktopId = Guid.NewGuid();
        var windows = new FakeWindows(Window("Editor", 10, 1));
        var desktop = new FakeDesktops(desktopId);
        var service = new WorkspaceRecoveryService(windows, () => desktop);
        WindowLayout ordinary = service.Capture();
        Assert.IsNull(ordinary.Windows[0].VirtualDesktopId);
        Assert.AreEqual(0, desktop.CaptureCalls);
        WindowLayout captured = service.Capture(new WorkspaceCaptureOptions { IncludeVirtualDesktops = true });
        Assert.AreEqual(desktopId, captured.Windows[0].VirtualDesktopId);
        Assert.AreEqual(VirtualDesktopPinMode.None, captured.Windows[0].PinMode);
        Assert.AreEqual(desktopId, captured.Desktops.Single().Id);
        Assert.AreEqual(1, desktop.DisposeCalls);
    }

    [TestMethod]
    public void Restore_DeletedDesktopNeverFallsBackToSameOrdinal() {
        WindowPosition live = Window("Editor", 10, 1);
        WindowPosition saved = Window("Editor", 99, 0);
        saved.VirtualDesktopId = Guid.NewGuid(); saved.PinMode = VirtualDesktopPinMode.None;
        var windows = new FakeWindows(live);
        var desktop = new FakeDesktops(Guid.NewGuid());
        WorkspaceRestoreResult result = new WorkspaceRecoveryService(windows, () => desktop).Restore(
            Layout(saved), new WorkspaceRestoreOptions { RestoreVirtualDesktops = true });
        Assert.AreEqual(0, desktop.RestoreCalls);
        Assert.IsFalse(result.Windows[0].VirtualDesktopApplied);
        Assert.IsTrue(result.Windows[0].PositionApplied);
        StringAssert.Contains(result.Windows[0].Error, "no longer exists");
    }

    [TestMethod]
    public void Restore_AmbiguousMatchSkipsDesktopAndPosition() {
        var windows = new FakeWindows(Window("Editor", 10, 1), Window("Editor", 11, 2));
        var desktop = new FakeDesktops(Guid.NewGuid());
        WorkspaceRestoreResult result = new WorkspaceRecoveryService(windows, () => desktop).Restore(
            Layout(Window("Editor", 99, 0)), new WorkspaceRestoreOptions { RestoreVirtualDesktops = true });
        Assert.AreEqual(WindowLayoutMatchStatus.Ambiguous, result.Windows.Single().MatchStatus);
        Assert.AreEqual(0, windows.Applied.Count);
        Assert.AreEqual(0, desktop.RestoreCalls);
    }

    [TestMethod]
    public void Restore_OneToOnePlanDoesNotReuseAnAlreadyAssignedWindow() {
        var windows = new FakeWindows(Window("Editor", 10, 1));
        WorkspaceRestoreResult result = new WorkspaceRecoveryService(windows, () => throw new AssertFailedException("No desktop connection expected."))
            .Restore(Layout(Window("Editor", 10, 0), Window("Editor", 99, 0)));
        Assert.AreEqual(1, windows.Applied.Count);
        Assert.AreEqual(WindowLayoutMatchStatus.Missing, result.Windows[1].MatchStatus);
    }

    [TestMethod]
    public void Restore_ChangedHwndIdentityPreventsEveryMutation() {
        var windows = new FakeWindows(Window("Editor", 10, 1)) { IdentityChanged = true };
        var desktop = new FakeDesktops(Guid.NewGuid());
        WorkspaceRestoreResult result = new WorkspaceRecoveryService(windows, () => desktop).Restore(
            Layout(Window("Editor", 10, 0)), new WorkspaceRestoreOptions { RestoreVirtualDesktops = true });
        Assert.AreEqual(0, windows.Applied.Count);
        Assert.AreEqual(0, desktop.RestoreCalls);
        Assert.AreEqual("identity changed", result.Windows[0].Error);
    }

    [TestMethod]
    public void Restore_WallpaperDuplicatesAreExplicitlyPreserved() {
        Guid first = Guid.NewGuid(), second = Guid.NewGuid();
        var desktop = new FakeDesktops(first, second);
        var layout = Layout();
        layout.Desktops.Add(new VirtualDesktopInfo { Id = first, WallpaperPath = @"C:\Images\shared.jpg" });
        layout.Desktops.Add(new VirtualDesktopInfo { Id = second, WallpaperPath = @"C:\Images\shared.jpg" });
        var service = new WorkspaceRecoveryService(new FakeWindows(), () => desktop);
        Assert.AreEqual(0, service.Restore(layout).Wallpapers.Count);
        WorkspaceRestoreResult restored = service.Restore(layout, new WorkspaceRestoreOptions { RestoreWallpapers = true });
        Assert.AreEqual(2, restored.Wallpapers.Count);
        Assert.IsTrue(restored.Wallpapers.All(item => item.Applied));
        CollectionAssert.AreEqual(new[] { first, second }, desktop.Wallpapers.ToArray());
    }

    [TestMethod]
    public void Restore_ApplicationPinPermissionRequiresDesktopRecovery() {
        var service = new WorkspaceRecoveryService(new FakeWindows(), () => throw new AssertFailedException("No native call expected."));
        Assert.ThrowsExactly<ArgumentException>(() => service.Restore(Layout(), new WorkspaceRestoreOptions { RestoreApplicationPins = true }));
    }

    [TestMethod]
    public void Plan_ExplorerFolderIdentityDistinguishesEqualTitles() {
        WindowPosition first = Window("Files", 10, 1), second = Window("Files", 10, 2), saved = Window("Files", 99, 0);
        first.ExplorerFolderPath = @"C:\First"; second.ExplorerFolderPath = @"C:\Second"; saved.ExplorerFolderPath = @"C:\Second";
        WindowLayoutPlan plan = new WorkspaceRecoveryService(new FakeWindows(first, second), () => throw new AssertFailedException()).Plan(Layout(saved));
        Assert.AreEqual(second.Handle, plan.Windows.Single().Window!.Handle);
    }

    [TestMethod]
    public void AutomaticCapture_PreservesPreviousBytesWhenWindowsAreMissingOrDesktopCaptureFails() {
        string path = Path.GetTempFileName();
        try {
            WorkspaceRecoveryService.Write(path, Layout(Window("One", 10, 0), Window("Two", 11, 0)));
            string before = File.ReadAllText(path);
            var service = new WorkspaceRecoveryService(new FakeWindows(Window("One", 10, 1)), () => throw new PlatformNotSupportedException("unsupported build"));
            Assert.IsFalse(service.TrySaveAutomatic(path));
            Assert.AreEqual(before, File.ReadAllText(path));
            Assert.IsFalse(service.TrySaveAutomatic(path, new WorkspaceCaptureOptions { IncludeVirtualDesktops = true }));
            Assert.AreEqual(before, File.ReadAllText(path));
        } finally { File.Delete(path); }
    }

    [TestMethod]
    public async Task StartupRecovery_RetriesOnlyMissingWindows() {
        var windows = new FakeWindows(Window("One", 10, 1));
        windows.NextCapture = () => new[] { Window("One", 10, 1), Window("Two", 11, 2) };
        var service = new WorkspaceRecoveryService(windows, () => throw new AssertFailedException());
        WorkspaceRestoreResult result = await service.RestoreAsync(Layout(Window("One", 10, 0), Window("Two", 11, 0)), maximumAttempts: 2, retryDelaySeconds: 1);
        CollectionAssert.AreEqual(new[] { "One", "Two" }, windows.Applied.ToArray());
        Assert.IsTrue(result.Windows.All(item => item.PositionApplied));
    }

    [TestMethod]
    public async Task StartupRecovery_CancellationPreventsNativeActions() {
        var windows = new FakeWindows(Window("Editor", 10, 1));
        var service = new WorkspaceRecoveryService(windows, () => throw new AssertFailedException());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => service.RestoreAsync(Layout(Window("Editor", 10, 0)), cancellationToken: cancellation.Token));
        Assert.AreEqual(0, windows.Applied.Count);
    }

    [TestMethod]
    public void LayoutJson_LegacyAndDesktopStateRoundTripWithoutLiveHandles() {
        WindowLayout legacy = JsonSerializer.Deserialize<WindowLayout>("{\"Windows\":[{\"Title\":\"Editor\",\"ProcessId\":10,\"Left\":1,\"Top\":2,\"Right\":101,\"Bottom\":102}]}")!;
        Assert.IsNull(legacy.Windows[0].VirtualDesktopId);
        WindowPosition position = Window("Editor", 10, 12345);
        position.VirtualDesktopId = Guid.NewGuid(); position.PinMode = VirtualDesktopPinMode.Window;
        WindowLayout restored = JsonSerializer.Deserialize<WindowLayout>(JsonSerializer.Serialize(Layout(position)))!;
        Assert.AreEqual(position.VirtualDesktopId, restored.Windows[0].VirtualDesktopId);
        Assert.AreEqual(position.PinMode, restored.Windows[0].PinMode);
        Assert.AreEqual(IntPtr.Zero, restored.Windows[0].Handle);
    }

    [TestMethod]
    public void Settings_RoundTripIsIsolatedAndAllRecoveryDefaultsAreDisabled() {
        string directory = Path.Combine(Path.GetTempPath(), "workspace-settings-" + Guid.NewGuid().ToString("N"));
        try {
            WorkspaceRecoverySettings defaults = WorkspaceRecoverySettings.Load(directory);
            Assert.IsFalse(defaults.RestoreOnStartup || defaults.AutoCapture || defaults.IncludeVirtualDesktops || defaults.RestoreApplicationPins);
            Assert.IsFalse(Directory.Exists(directory));
            new WorkspaceRecoverySettings { RestoreOnStartup = true, IncludeVirtualDesktops = true, AutoCapture = true }.Save(directory);
            WorkspaceRecoverySettings restored = WorkspaceRecoverySettings.Load(directory);
            Assert.IsTrue(restored.RestoreOnStartup && restored.IncludeVirtualDesktops && restored.AutoCapture);
        } finally { if (Directory.Exists(directory)) { Directory.Delete(directory, true); } }
    }

    private static WindowPosition Window(string title, uint processId, long handle) => new() {
        Title = title, ProcessId = processId, ProcessName = "editor", ClassName = "EditorWindow", Handle = new IntPtr(handle),
        Left = 10, Top = 20, Right = 410, Bottom = 320, State = WindowState.Normal
    };
    private static WindowLayout Layout(params WindowPosition[] windows) => new() { Windows = windows.ToList() };

    private sealed class FakeWindows : IWorkspaceWindowApi {
        private IReadOnlyList<WindowPosition> _current;
        internal FakeWindows(params WindowPosition[] windows) { _current = windows; }
        internal List<string> Applied { get; } = new();
        internal Func<IReadOnlyList<WindowPosition>>? NextCapture { get; set; }
        internal bool IdentityChanged { get; set; }
        public IReadOnlyList<WindowPosition> Capture(bool includeExplorerFolders) {
            IReadOnlyList<WindowPosition> result = _current;
            if (NextCapture != null) { _current = NextCapture(); NextCapture = null; }
            return result;
        }
        public void VerifyIdentity(WindowPosition window) { if (IdentityChanged) { throw new InvalidOperationException("identity changed"); } }
        public WindowLayoutApplyResult Apply(WindowLayoutMatch match) { Applied.Add(match.SavedWindow.Title); return new WindowLayoutApplyResult(match, true, null); }
        public void OpenExplorer(string path) => throw new DirectoryNotFoundException("missing folder");
    }

    private sealed class FakeDesktops : IWorkspaceDesktopApi {
        private readonly Guid[] _ids;
        internal FakeDesktops(params Guid[] ids) { _ids = ids; }
        internal int CaptureCalls { get; private set; }
        internal int RestoreCalls { get; private set; }
        internal int DisposeCalls { get; private set; }
        internal List<Guid> Wallpapers { get; } = new();
        public IReadOnlyList<VirtualDesktopInfo> GetDesktops() => _ids.Select((id, index) => new VirtualDesktopInfo { Id = id, Index = index + 1 }).ToArray();
        public void CaptureWindow(WindowPosition window) { CaptureCalls++; window.VirtualDesktopId = _ids[0]; window.PinMode = VirtualDesktopPinMode.None; }
        public void RestoreWindow(WindowPosition saved, IntPtr handle, bool restoreApplicationPins) { RestoreCalls++; }
        public void SetWallpaper(Guid desktopId, string wallpaperPath) { Wallpapers.Add(desktopId); }
        public void Dispose() { DisposeCalls++; }
    }
}
