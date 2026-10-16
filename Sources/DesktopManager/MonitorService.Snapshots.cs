using System.ComponentModel;
using System.Linq;

namespace DesktopManager;

public partial class MonitorService {
    /// <summary>Captures monitor identity, physical bounds and effective per-monitor DPI without changing desktop settings.</summary>
    /// <returns>A fresh read-only collection ordered by the existing wallpaper monitor index, including missing-device-path entries.</returns>
    /// <remarks>
    /// Requires Windows 10 version 1607 or Windows Server 2016 or later. Queries run in a temporary
    /// per-monitor-aware context on the calling thread, which is restored before returning or throwing.
    /// The process DPI awareness is unchanged. DPI and scale are null when Windows cannot read them.
    /// Indices describe the current topology, not permanent hardware identities. Reacquire snapshots after display changes.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">Thread DPI contexts are unavailable.</exception>
    /// <exception cref="Win32Exception">Windows could not change or restore the thread DPI context.</exception>
    public IReadOnlyList<MonitorSnapshot> GetMonitorSnapshots() {
        return GetMonitorSnapshots(new MonitorSnapshotNative());
    }

    internal IReadOnlyList<MonitorSnapshot> GetMonitorSnapshots(MonitorSnapshotNative native) {
        // PER_MONITOR_AWARE is supported on Server 2016, including builds without the V2 context.
        IntPtr previous = native.SetThreadDpiAwarenessContext(new IntPtr(-3));
        if (previous == IntPtr.Zero) {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enter a per-monitor DPI context.");
        }

        try {
            List<DisplayPathSnapshot> displayPaths = native.EnumerateDisplayPaths();
            List<DisplayPathSnapshot> attachedPaths = GetAttachedSnapshotPaths(displayPaths);
            try {
                uint count = Execute(() => _desktopManager.GetMonitorDevicePathCount(), nameof(IDesktopManager.GetMonitorDevicePathCount));
                if (count > 0) {
                    var slots = new List<(string DeviceId, RECT Bounds, DisplayPathSnapshot? Path)>();
                    var assignedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (uint i = 0; i < count; i++) {
                        string deviceId = Execute(() => _desktopManager.GetMonitorDevicePathAt(i), nameof(IDesktopManager.GetMonitorDevicePathAt)) ?? string.Empty;
                        RECT bounds = string.IsNullOrWhiteSpace(deviceId) ? default :
                            Execute(() => _desktopManager.GetMonitorBounds(deviceId), nameof(IDesktopManager.GetMonitorBounds));
                        // Reserve exact/geometry matches before assigning anonymous RDS slots. COM and
                        // native monitor order can differ, so a later known slot must not be borrowed.
                        DisplayPathSnapshot? path = string.IsNullOrWhiteSpace(deviceId) ? null :
                            ResolveDisplayPathForMonitor(attachedPaths, deviceId, bounds, -1);
                        if (path != null) {
                            assignedSources.Add(path.AdapterDeviceName);
                        }
                        slots.Add((deviceId, bounds, path));
                    }
                    List<MonitorSnapshot> snapshots = new List<MonitorSnapshot>();
                    for (int i = 0; i < slots.Count; i++) {
                        var slot = slots[i];
                        DisplayPathSnapshot? path = slot.Path;
                        if (string.IsNullOrWhiteSpace(slot.DeviceId)) {
                            path = attachedPaths.FirstOrDefault(candidate => !assignedSources.Contains(candidate.AdapterDeviceName));
                            if (path != null) {
                                assignedSources.Add(path.AdapterDeviceName);
                            }
                        }
                        snapshots.Add(CreateMonitorSnapshot(native, i, slot.DeviceId, path, path?.AdapterBounds ?? slot.Bounds));
                    }
                    return snapshots.AsReadOnly();
                }
            } catch (DesktopManagerException) {
                // Session hosts can expose display geometry without the wallpaper COM service.
            } catch (COMException) {
                // COM activation itself can fail before Execute translates an error.
            }

            List<MonitorSnapshot> fallback = new List<MonitorSnapshot>();
            foreach (DisplayPathSnapshot path in attachedPaths) {
                // Native display-interface identifiers have not been confirmed by the wallpaper COM service.
                fallback.Add(CreateMonitorSnapshot(native, path.AdapterMonitorIndex ?? fallback.Count, string.Empty, path, path.AdapterBounds!.Value));
            }
            return fallback.AsReadOnly();
        } finally {
            if (native.SetThreadDpiAwarenessContext(previous) == IntPtr.Zero) {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not restore the calling thread's DPI context.");
            }
        }
    }

    private static List<DisplayPathSnapshot> GetAttachedSnapshotPaths(List<DisplayPathSnapshot> displayPaths) {
        var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return displayPaths
            .Where(path => (path.AdapterStateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0 &&
                path.AdapterBounds.HasValue && path.AdapterBounds.Value.Right > path.AdapterBounds.Value.Left &&
                path.AdapterBounds.Value.Bottom > path.AdapterBounds.Value.Top)
            .OrderBy(path => path.AdapterMonitorIndex ?? int.MaxValue)
            .Where(path => sources.Add(path.AdapterDeviceName))
            .ToList();
    }

    private static MonitorSnapshot CreateMonitorSnapshot(MonitorSnapshotNative native, int index, string deviceId, DisplayPathSnapshot? path, RECT bounds) {
        bool hasDpi = native.TryGetDpi(bounds, out uint dpiX, out uint dpiY) && dpiX > 0 && dpiY > 0;
        return new MonitorSnapshot(index, deviceId, path?.AdapterDeviceName ?? string.Empty,
            path != null && (path.AdapterStateFlags & DisplayDeviceStateFlags.PrimaryDevice) != 0,
            bounds, hasDpi ? dpiX : (uint?)null, hasDpi ? dpiY : (uint?)null);
    }
}
