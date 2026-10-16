using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public sealed class MonitorSnapshotTests {
    [TestMethod]
    public void GetMonitorSnapshots_ComOrder_PreservesWallpaperIndicesAndPhysicalGeometry() {
        var desktop = new ReadOnlyDesktopManager { Paths = new[] { "second", "first" } };
        var native = new SnapshotNative();
        native.Paths.Add(Path("first", @"\\.\DISPLAY1", 0, 0, 1920, 1080, primary: true));
        native.Paths.Add(Path("second", @"\\.\DISPLAY2", -2560, -200, 0, 1240));
        var service = new MonitorService(desktop);

        IReadOnlyList<MonitorSnapshot> snapshots = service.GetMonitorSnapshots(native);
        IReadOnlyList<MonitorSnapshot> repeated = service.GetMonitorSnapshots(native);

        Assert.AreEqual(2, snapshots.Count);
        Assert.AreEqual(0, snapshots[0].Index);
        Assert.AreEqual("second", snapshots[0].DeviceId);
        Assert.AreEqual(@"\\.\DISPLAY2", snapshots[0].DeviceName);
        Assert.IsFalse(snapshots[0].IsPrimary);
        Assert.AreEqual(-2560, snapshots[0].Left);
        Assert.AreEqual(-200, snapshots[0].Top);
        Assert.AreEqual(0, snapshots[0].Right);
        Assert.AreEqual(1240, snapshots[0].Bottom);
        Assert.AreEqual(2560, snapshots[0].Width);
        Assert.AreEqual(1440, snapshots[0].Height);
        Assert.AreEqual((uint?)144, snapshots[0].DpiX);
        Assert.AreEqual((uint?)144, snapshots[0].DpiY);
        Assert.AreEqual((double?)1.5, snapshots[0].Scale);
        Assert.AreEqual(1, snapshots[1].Index);
        Assert.IsTrue(snapshots[1].IsPrimary);
        Assert.AreEqual((uint?)120, snapshots[1].DpiX);
        Assert.AreEqual((double?)1.25, snapshots[1].Scale);
        CollectionAssert.AreEqual(snapshots.Select(item => item.DeviceName).ToArray(), repeated.Select(item => item.DeviceName).ToArray());
        CollectionAssert.AreEqual(snapshots.Select(item => item.Index).ToArray(), repeated.Select(item => item.Index).ToArray());
        Assert.AreEqual(new IntPtr(-1), native.Context);

        // A consumer can edit its copy of RECT without changing a captured snapshot.
        RECT copy = snapshots[0].Bounds;
        copy.Left = 100;
        native.Paths[1].AdapterBounds = default(RECT);
        Assert.AreEqual(-2560, snapshots[0].Bounds.Left);
    }

    [TestMethod]
    public void GetMonitorSnapshots_MissingDevicePath_RetainsEntryWithoutDeviceScopedComCalls() {
        var desktop = new ReadOnlyDesktopManager { Paths = new[] { (string)null!, "" } };
        var native = new SnapshotNative();
        native.Paths.Add(Path("", @"\\.\DISPLAY1", 0, 0, 1920, 1080, primary: true));
        native.Paths.Add(Path("", @"\\.\DISPLAY2", -2560, 0, 0, 1440));

        IReadOnlyList<MonitorSnapshot> snapshots = new MonitorService(desktop).GetMonitorSnapshots(native);

        Assert.AreEqual(2, snapshots.Count);
        Assert.AreEqual(string.Empty, snapshots[0].DeviceId);
        Assert.AreEqual(string.Empty, snapshots[1].DeviceId);
        Assert.AreEqual(0, snapshots[0].Index);
        Assert.AreEqual(1, snapshots[1].Index);
        Assert.IsTrue(snapshots[0].IsPrimary);
        Assert.AreEqual(@"\\.\DISPLAY2", snapshots[1].DeviceName);
        Assert.AreEqual(2560, snapshots[1].Width);
        Assert.AreEqual(0, desktop.BoundsCalls);
    }

    [TestMethod]
    public void GetMonitorSnapshots_UnmatchedComPath_DoesNotBorrowGeometryByIndex() {
        var desktop = new ReadOnlyDesktopManager { Paths = new[] { "com-only" } };
        var native = new SnapshotNative();
        native.Paths.Add(Path("native-only", @"\\.\DISPLAY2", -2560, 0, 0, 1440));

        MonitorSnapshot snapshot = new MonitorService(desktop).GetMonitorSnapshots(native)[0];

        Assert.AreEqual("com-only", snapshot.DeviceId);
        Assert.AreEqual(string.Empty, snapshot.DeviceName);
        Assert.AreEqual(0, snapshot.Left);
        Assert.AreEqual(10, snapshot.Width);
        Assert.AreEqual(10, snapshot.Height);
    }

    [TestMethod]
    public void GetMonitorSnapshots_MissingPaths_IgnoreInactiveAndDuplicateChildrenAndUseNativeOrder() {
        var desktop = new ReadOnlyDesktopManager { Paths = new[] { "", "" } };
        var native = new SnapshotNative();
        var inactive = Path("stale", "inactive", 0, 0, 800, 600);
        inactive.AdapterStateFlags = 0;
        var second = Path("second", @"\\.\DISPLAY2", -2560, 0, 0, 1440);
        second.AdapterMonitorIndex = 1;
        var first = Path("first", @"\\.\DISPLAY1", 0, 0, 1920, 1080, primary: true);
        first.AdapterMonitorIndex = 0;
        native.Paths.AddRange(new[] { inactive, second, second, first });

        IReadOnlyList<MonitorSnapshot> snapshots = new MonitorService(desktop).GetMonitorSnapshots(native);

        Assert.AreEqual(2, snapshots.Count);
        Assert.AreEqual(0, snapshots[0].Index);
        Assert.AreEqual(@"\\.\DISPLAY1", snapshots[0].DeviceName);
        Assert.AreEqual(1920, snapshots[0].Width);
        Assert.IsTrue(snapshots[0].IsPrimary);
        Assert.AreEqual(1, snapshots[1].Index);
        Assert.AreEqual(@"\\.\DISPLAY2", snapshots[1].DeviceName);
        Assert.AreEqual(2560, snapshots[1].Width);
        Assert.AreEqual((double?)1.5, snapshots[1].Scale);
        Assert.AreEqual(0, desktop.BoundsCalls);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GetMonitorSnapshots_MixedKnownAndMissingPaths_ReserveKnownDisplayRegardlessOfComOrder(bool missingFirst) {
        var desktop = new ReadOnlyDesktopManager { Paths = missingFirst ? new[] { "", "second" } : new[] { "second", "" } };
        var native = new SnapshotNative();
        native.Paths.Add(Path("first", @"\\.\DISPLAY1", 0, 0, 1920, 1080, primary: true));
        native.Paths.Add(Path("second", @"\\.\DISPLAY2", -2560, 0, 0, 1440));

        IReadOnlyList<MonitorSnapshot> snapshots = new MonitorService(desktop).GetMonitorSnapshots(native);
        MonitorSnapshot anonymous = snapshots[missingFirst ? 0 : 1];
        MonitorSnapshot known = snapshots[missingFirst ? 1 : 0];

        Assert.AreEqual(@"\\.\DISPLAY1", anonymous.DeviceName);
        Assert.AreEqual(string.Empty, anonymous.DeviceId);
        Assert.AreEqual(1920, anonymous.Width);
        Assert.AreEqual(@"\\.\DISPLAY2", known.DeviceName);
        Assert.AreEqual("second", known.DeviceId);
        Assert.AreEqual(2560, known.Width);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GetMonitorSnapshots_EmptyOrUnavailableCom_UsesNativeMonitorOrderAndRetainsMissingPaths(bool failCom) {
        var desktop = new ReadOnlyDesktopManager { Paths = Array.Empty<string>(), FailEnumeration = failCom };
        var native = new SnapshotNative();
        var second = Path("", @"\\.\DISPLAY2", -2560, 0, 0, 1440);
        second.AdapterMonitorIndex = 1;
        var first = Path("first", @"\\.\DISPLAY1", 0, 0, 1920, 1080, primary: true);
        first.AdapterMonitorIndex = 0;
        var disconnected = Path("old", "disconnected", 0, 0, 1, 1);
        disconnected.AdapterStateFlags = 0;
        native.Paths.AddRange(new[] { second, disconnected, first, first });

        IReadOnlyList<MonitorSnapshot> snapshots = new MonitorService(desktop).GetMonitorSnapshots(native);

        Assert.AreEqual(2, snapshots.Count);
        Assert.AreEqual(0, snapshots[0].Index);
        Assert.AreEqual(@"\\.\DISPLAY1", snapshots[0].DeviceName);
        Assert.AreEqual(string.Empty, snapshots[0].DeviceId, "A native interface ID must not be presented as a confirmed wallpaper COM path.");
        Assert.AreEqual(1, snapshots[1].Index);
        Assert.AreEqual(@"\\.\DISPLAY2", snapshots[1].DeviceName);
        Assert.AreEqual(string.Empty, snapshots[1].DeviceId);
        Assert.AreEqual(2560, snapshots[1].Width);
    }

    [TestMethod]
    public void GetMonitorSnapshots_DpiUnavailable_ReturnsNullWithoutDroppingMonitor() {
        var desktop = new ReadOnlyDesktopManager { Paths = new[] { "first" } };
        var native = new SnapshotNative { HasDpi = false };
        native.Paths.Add(Path("first", @"\\.\DISPLAY1", 0, 0, 1920, 1080));

        MonitorSnapshot snapshot = new MonitorService(desktop).GetMonitorSnapshots(native)[0];

        Assert.AreEqual(1920, snapshot.Width);
        Assert.IsNull(snapshot.DpiX);
        Assert.IsNull(snapshot.DpiY);
        Assert.IsNull(snapshot.Scale);
        Assert.AreEqual(new IntPtr(-1), native.Context);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GetMonitorSnapshots_NativeReadThrows_RestoresCallerContext(bool failDpi) {
        var native = new SnapshotNative { FailEnumeration = !failDpi, FailDpi = failDpi };
        native.Paths.Add(Path("first", @"\\.\DISPLAY1", 0, 0, 1920, 1080));

        Assert.ThrowsExactly<InvalidOperationException>(() => new MonitorService(new ReadOnlyDesktopManager()).GetMonitorSnapshots(native));

        Assert.AreEqual(new IntPtr(-1), native.Context);
    }

    [TestMethod]
    public void GetMonitorSnapshots_ContextChangeFails_DoesNotEnumerateVirtualizedData() {
        var native = new SnapshotNative { RejectContext = true, FailEnumeration = true };

        Assert.ThrowsExactly<Win32Exception>(() => new MonitorService(new ReadOnlyDesktopManager()).GetMonitorSnapshots(native));

        Assert.AreEqual(new IntPtr(-1), native.Context);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(-2)]
    [DataRow(-3)]
    public void GetMonitorSnapshots_WindowsCallerAwareness_PreservesPhysicalGeometryDpiAndContext(int callerContext) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Requires Windows with thread DPI contexts.");
        }

        var native = new MonitorSnapshotNative();
        IntPtr original;
        try {
            original = native.SetThreadDpiAwarenessContext(new IntPtr(-3));
        } catch (PlatformNotSupportedException) {
            Assert.Inconclusive("Thread DPI contexts are unavailable.");
            return;
        }
        if (original == IntPtr.Zero) {
            Assert.Inconclusive("Cannot enter a per-monitor-aware context.");
        }

        try {
            // All COM settings operations throw; only monitor enumeration is permitted in this test.
            var monitors = new Monitors(() => new ReadOnlyDesktopManager { FailEnumeration = true });
            IReadOnlyList<MonitorSnapshot> expected = monitors.GetMonitorSnapshots();
            if (expected.Count == 0) {
                Assert.Inconclusive("No attached monitors available.");
            }
            Assert.AreNotEqual(IntPtr.Zero, native.SetThreadDpiAwarenessContext(new IntPtr(callerContext)));

            IReadOnlyList<MonitorSnapshot> actual = monitors.GetMonitorSnapshots();

            Assert.IsTrue(AreDpiAwarenessContextsEqual(new IntPtr(callerContext), GetThreadDpiAwarenessContext()));
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++) {
                Assert.AreEqual(expected[i].Index, actual[i].Index);
                Assert.AreEqual(expected[i].DeviceName, actual[i].DeviceName);
                Assert.AreEqual(expected[i].Bounds, actual[i].Bounds);
                Assert.AreEqual(expected[i].DpiX, actual[i].DpiX);
                Assert.AreEqual(expected[i].DpiY, actual[i].DpiY);
                Assert.AreEqual(expected[i].Scale, actual[i].Scale);
            }
            if (!expected.Any(snapshot => snapshot.DpiX.HasValue)) {
                Assert.Inconclusive("Geometry and context checks passed, but effective DPI is unavailable on this desktop.");
            }
        } finally {
            Assert.AreNotEqual(IntPtr.Zero, native.SetThreadDpiAwarenessContext(original));
        }
    }

    private static MonitorService.DisplayPathSnapshot Path(string id, string name, int left, int top, int right, int bottom, bool primary = false) {
        return new MonitorService.DisplayPathSnapshot {
            MonitorDeviceId = id,
            AdapterDeviceName = name,
            AdapterStateFlags = DisplayDeviceStateFlags.AttachedToDesktop | (primary ? DisplayDeviceStateFlags.PrimaryDevice : 0),
            AdapterBounds = new RECT { Left = left, Top = top, Right = right, Bottom = bottom }
        };
    }

    private sealed class SnapshotNative : MonitorSnapshotNative {
        internal IntPtr Context = new IntPtr(-1);
        internal readonly List<MonitorService.DisplayPathSnapshot> Paths = new();
        internal bool HasDpi = true;
        internal bool FailEnumeration;
        internal bool FailDpi;
        internal bool RejectContext;

        internal override IntPtr SetThreadDpiAwarenessContext(IntPtr context) {
            if (RejectContext) {
                return IntPtr.Zero;
            }
            IntPtr previous = Context;
            Context = context;
            return previous;
        }

        internal override List<MonitorService.DisplayPathSnapshot> EnumerateDisplayPaths() {
            Assert.AreEqual(new IntPtr(-3), Context, "Geometry must be queried in a per-monitor-aware context.");
            if (FailEnumeration) {
                throw new InvalidOperationException("Native enumeration failed.");
            }
            return Paths;
        }

        internal override bool TryGetDpi(RECT bounds, out uint dpiX, out uint dpiY) {
            Assert.AreEqual(new IntPtr(-3), Context, "DPI must be queried in a per-monitor-aware context.");
            if (FailDpi) {
                throw new InvalidOperationException("Native DPI query failed.");
            }
            dpiX = dpiY = bounds.Left < 0 ? 144u : 120u;
            return HasDpi;
        }
    }

    // Strict boundary: any wallpaper read or settings operation makes the snapshot tests fail.
    private sealed class ReadOnlyDesktopManager : IDesktopManager {
        internal string[] Paths = Array.Empty<string>();
        internal bool FailEnumeration;
        internal int BoundsCalls;
        public uint GetMonitorDevicePathCount() => FailEnumeration ? throw new COMException("No wallpaper service") : (uint)Paths.Length;
        public string GetMonitorDevicePathAt(uint monitorIndex) => Paths[monitorIndex];
        public RECT GetMonitorBounds(string monitorId) {
            Assert.IsFalse(string.IsNullOrWhiteSpace(monitorId));
            BoundsCalls++;
            return new RECT { Right = 10, Bottom = 10 };
        }
        public void SetWallpaper(string monitorId, string wallpaper) => throw new InvalidOperationException();
        public string GetWallpaper(string monitorId) => throw new InvalidOperationException();
        public void SetBackgroundColor(uint color) => throw new InvalidOperationException();
        public uint GetBackgroundColor() => throw new InvalidOperationException();
        public void SetPosition(DesktopWallpaperPosition position) => throw new InvalidOperationException();
        public DesktopWallpaperPosition GetPosition() => throw new InvalidOperationException();
        public void SetSlideshow(IntPtr items) => throw new InvalidOperationException();
        public uint GetSlideshow(out IntPtr items) => throw new InvalidOperationException();
        public void SetSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick) => throw new InvalidOperationException();
        public uint GetSlideshowOptions(out DesktopSlideshowOptions options, out uint slideshowTick) => throw new InvalidOperationException();
        public void AdvanceSlideshow(string? monitorId, DesktopSlideshowDirection direction) => throw new InvalidOperationException();
        public uint GetStatus(out DesktopSlideshowState state) => throw new InvalidOperationException();
        public void Enable(bool enable) => throw new InvalidOperationException();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetThreadDpiAwarenessContext();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);
}
