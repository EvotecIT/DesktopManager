using System.IO;
using System.Linq;

namespace DesktopManager;

public partial class MonitorService {
    internal DesktopWallpaperBackground ReadNativeWallpaperBackground(out bool enabled) {
        CheckCheckpointResult(_desktopManager.GetStatus(out var state), "GetStatus");
        enabled = state.HasFlag(DesktopSlideshowState.Enabled);
        var color = _desktopManager.GetBackgroundColor();
        var rgb = ((color & 0xff) << 16) | (color & 0xff00) | ((color >> 16) & 0xff);
        return new DesktopWallpaperBackground("#" + rgb.ToString("X6"), DesktopBackgroundShading.Solid);
    }

    /// <summary>Captures strict native state; unlike presentation reads, no fallback value may masquerade as a restorable setting.</summary>
    internal DesktopWallpaperSnapshot CaptureNativeWallpaperCheckpoint(object owner, Func<bool> interactive, Func<IReadOnlyList<MonitorSnapshot>> geometry,
        WindowsWallpaperSelection? selection = null) {
        var mode = selection?.Read();
        if (selection != null && (!mode.HasValue || mode < 0 || mode > 2)) {
            return new DesktopWallpaperSnapshot(DesktopOperationStatus.Unsupported,
                "An absent, unknown or Spotlight Windows background mode cannot be restored through a static image checkpoint.", "windows", owner);
        }
        var manager = _desktopManager;
        var ids = ReadCheckpointMonitorIds(geometry);
        var paths = ids.Select(id => manager.GetWallpaper(id) ?? string.Empty).ToArray();
        if (mode == 1 && paths.Any(path => path.Length > 0)) {
            return new DesktopWallpaperSnapshot(DesktopOperationStatus.Unsupported,
                "The solid-color desktop retains native image assignments. Their visible mode and retained assignments cannot be faithfully restored through this checkpoint.", "windows", owner);
        }
        if (paths.Any(path => path.Length > 0 && !File.Exists(path))) {
            return new DesktopWallpaperSnapshot(DesktopOperationStatus.Unsupported,
                "An assigned Windows monitor image must expose an existing file before a native checkpoint can be restored.", "windows", owner);
        }
        var position = manager.GetPosition();
        var color = manager.GetBackgroundColor();
        CheckCheckpointResult(manager.GetStatus(out var state), "GetStatus");
        CheckCheckpointResult(manager.GetSlideshowOptions(out var options, out var tick), "GetSlideshowOptions");
        var result = manager.GetSlideshow(out var pointer);
        var items = new WallpaperShellItems(pointer);
        try {
            // Windows can return S_FALSE for an inactive slideshow while retaining its item array.
            // A configured slideshow must still supply its original items for faithful restoration.
            if (result != 1) {
                CheckCheckpointResult(result, "GetSlideshow");
            }
            if ((mode == 2 || state.HasFlag(DesktopSlideshowState.Slideshow)) && items.Count == 0) {
                throw new InvalidOperationException("The configured native slideshow did not return its original items.");
            }
            return new DesktopWallpaperSnapshot(DesktopOperationStatus.Success,
                "Captured native Windows images, global fit/color, slideshow items/options and enabled state.", "windows", owner,
                () => RestoreNativeWallpaperCheckpoint(ids, paths, position, color, state, options, tick, items, interactive, geometry, selection, mode), items);
        } catch {
            items.Dispose();
            throw;
        }
    }

    private DesktopOperationResult RestoreNativeWallpaperCheckpoint(string[] ids, string[] paths, DesktopWallpaperPosition position, uint color,
        DesktopSlideshowState state, DesktopSlideshowOptions options, uint tick, WallpaperShellItems items, Func<bool> interactive,
        Func<IReadOnlyList<MonitorSnapshot>> geometry, WindowsWallpaperSelection? selection, int? mode) {
        if (!interactive() || !ids.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(ReadCheckpointMonitorIds(geometry).OrderBy(id => id, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)) {
            return new DesktopOperationResult(DesktopOperationStatus.Unavailable, "The Windows session or monitor topology changed; no settings were restored.");
        }
        if (paths.Any(path => path.Length > 0 && !File.Exists(path))) {
            return new DesktopOperationResult(DesktopOperationStatus.Unavailable, "An original Windows wallpaper file is unavailable; no settings were restored.");
        }
        if (mode.HasValue) { selection!.EnsureWritable(); }
        var failures = new List<string>();
        void Attempt(string operation, Action action) {
            try { action(); } catch (Exception ex) { failures.Add(operation + ": " + ex.Message); }
        }
        Attempt("Stop temporary slideshow", () => _desktopManager.SetSlideshow(IntPtr.Zero));
        // Restoring an inactive source list can itself select slideshow/image state. Restore
        // that list first, then the visible static image (or empty solid-color assignment).
        var slideshowActive = mode != 1 && state.HasFlag(DesktopSlideshowState.Slideshow);
        if (!slideshowActive) {
            Attempt("Restore inactive slideshow items", () => items.WithApartmentPointer(pointer => _desktopManager.SetSlideshow(pointer)));
        }
        for (var index = 0; index < ids.Length; index++) {
            var current = index;
            Attempt("Restore monitor " + current, () => {
                _desktopManager.SetWallpaper(ids[current], paths[current]);
                UpdateWallpaperCache(ids[current], paths[current]);
            });
        }
        if (slideshowActive) {
            Attempt("Restore slideshow items", () => items.WithApartmentPointer(pointer => _desktopManager.SetSlideshow(pointer)));
        }
        Attempt("Restore slideshow options", () => _desktopManager.SetSlideshowOptions(options, tick));
        Attempt("Restore background color", () => _desktopManager.SetBackgroundColor(color));
        Attempt("Restore fit", () => _desktopManager.SetPosition(position));
        Attempt("Restore enabled state", () => _desktopManager.Enable(state.HasFlag(DesktopSlideshowState.Enabled)));
        if (mode.HasValue) { Attempt("Restore selected background mode", () => selection!.Set(mode.Value)); }
        _desktopWallpaperEnableAttempted = false;
        Attempt("Verify native settings", () => {
            CheckCheckpointResult(_desktopManager.GetStatus(out var restoredState), "GetStatus");
            CheckCheckpointResult(_desktopManager.GetSlideshowOptions(out var restoredOptions, out var restoredTick), "GetSlideshowOptions");
            if (_desktopManager.GetPosition() != position || _desktopManager.GetBackgroundColor() != color
                || restoredState != state || restoredOptions != options || restoredTick != tick
                || mode.HasValue && selection!.Read() != mode) {
                throw new InvalidOperationException("Restored Windows settings did not match native readback.");
            }
            if (!slideshowActive && mode != 1) {
                for (var index = 0; index < ids.Length; index++) {
                    if (!string.Equals(_desktopManager.GetWallpaper(ids[index]), paths[index], StringComparison.OrdinalIgnoreCase)) {
                        throw new InvalidOperationException("A restored monitor image did not match native readback.");
                    }
                }
            }
        });
        return failures.Count == 0 ? new DesktopOperationResult(DesktopOperationStatus.Success, "Native Windows wallpaper state was restored and read back.")
            : new DesktopOperationResult(DesktopOperationStatus.Failed, "Windows wallpaper restoration was incomplete: " + string.Join("; ", failures));
    }

    private string[] ReadCheckpointMonitorIds(Func<IReadOnlyList<MonitorSnapshot>> geometry) {
        if (geometry().Any(display => string.IsNullOrWhiteSpace(display.DeviceId) && display.Width > 0 && display.Height > 0)) {
            throw new InvalidOperationException("An attached Windows monitor has no addressable wallpaper path.");
        }
        var count = _desktopManager.GetMonitorDevicePathCount();
        var ids = new List<string>();
        for (uint index = 0; index < count; index++) {
            var id = _desktopManager.GetMonitorDevicePathAt(index);
            // Windows can retain an empty, zero-geometry COM slot for a disconnected display.
            // The physical snapshot above must first rule out an attached anonymous display.
            if (string.IsNullOrWhiteSpace(id)) { continue; }
            if (ids.Contains(id, StringComparer.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("A Windows monitor has no unique addressable wallpaper path.");
            }
            ids.Add(id);
        }
        if (ids.Count == 0) { throw new InvalidOperationException("No addressable Windows wallpaper monitors are available."); }
        return ids.ToArray();
    }

    private static void CheckCheckpointResult(uint result, string operation) {
        if (result != 0) {
            throw new InvalidOperationException(operation + " could not capture native state (HRESULT 0x" + result.ToString("X8") + ").");
        }
    }

    /// <summary>Retains the shell array through an agile COM reference, resolving and releasing ordinary pointers only in their calling apartment.</summary>
    private sealed class WallpaperShellItems : IDisposable {
        private static readonly Guid ShellItemArrayId = new Guid("b63ea76d-1f85-456f-a19c-48159efa858b");
        private IAgileWallpaperReference? _reference;

        internal WallpaperShellItems(IntPtr pointer) {
            if (pointer == IntPtr.Zero) { return; }
            try {
                var id = ShellItemArrayId;
                Marshal.ThrowExceptionForHR(RoGetAgileReference(0, ref id, pointer, out _reference));
            } finally {
                // GetSlideshow's original reference belongs to this capture apartment.
                Marshal.Release(pointer);
            }
        }

        internal uint Count {
            get {
                uint count = 0;
                WithApartmentPointer(pointer => {
                    if (pointer == IntPtr.Zero) { return; }
                    var array = (MonitorNativeMethods.IShellItemArray)Marshal.GetObjectForIUnknown(pointer);
                    try { Marshal.ThrowExceptionForHR(array.GetCount(out count)); }
                    finally { Marshal.ReleaseComObject(array); }
                });
                return count;
            }
        }

        internal void WithApartmentPointer(Action<IntPtr> action) {
            if (_reference == null) { action(IntPtr.Zero); return; }
            var id = ShellItemArrayId;
            _reference.Resolve(ref id, out var pointer);
            try { action(pointer); } finally { Marshal.Release(pointer); }
        }

        public void Dispose() {
            var reference = _reference;
            _reference = null;
            if (reference != null) {
                // RoGetAgileReference creates an exclusively owned agile object. Its RCW also
                // provides COM-aware finalization if a caller forgets to dispose the checkpoint.
                Marshal.ReleaseComObject(reference);
            }
        }

        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern int RoGetAgileReference(uint options, ref Guid interfaceId, IntPtr pointer,
            [MarshalAs(UnmanagedType.Interface)] out IAgileWallpaperReference? reference);

        [ComImport, Guid("C03F6A43-65A4-9818-987E-E0B810D2A6F2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAgileWallpaperReference {
            void Resolve(ref Guid interfaceId, out IntPtr pointer);
        }
    }
}
