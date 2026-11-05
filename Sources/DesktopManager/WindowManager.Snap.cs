using System;
using System.Collections.Generic;

namespace DesktopManager;

public partial class WindowManager {
    private readonly object _autoSnapLock = new();
    private WindowEventWatcher? _autoSnapWatcher;

    /// <summary>
    /// Gets options for window snapping.
    /// </summary>
    public WindowSnapOptions SnapOptions { get; }

    /// <summary>
    /// Initializes snapping options.
    /// </summary>
    /// <param name="options">Snap options instance.</param>
    public WindowManager(WindowSnapOptions? options) : this() {
        SnapOptions = options ?? new WindowSnapOptions();
    }

    /// <summary>
    /// Starts automatic snapping when the user finishes dragging a window near an edge of its monitor's work area.
    /// </summary>
    /// <remarks>
    /// Events arrive on a dedicated hook thread, so the calling thread needs no message loop.
    /// The window snaps on the monitor it was dropped on.
    /// </remarks>
    public void StartAutoSnap() {
        lock (_autoSnapLock) {
            if (_autoSnapWatcher != null) {
                return;
            }

            var watcher = new WindowEventWatcher(WindowEventKinds.MoveSizeEnded);
            watcher.WindowChanged += OnMoveSizeEnded;
            _autoSnapWatcher = watcher;
        }
    }

    /// <summary>
    /// Stops automatic snapping.
    /// </summary>
    public void StopAutoSnap() {
        WindowEventWatcher? watcher;
        lock (_autoSnapLock) {
            watcher = _autoSnapWatcher;
            _autoSnapWatcher = null;
        }

        watcher?.Dispose();
    }

    private void OnMoveSizeEnded(object? sender, WindowChangedEventArgs e) {
        IntPtr handle = e.Handle;
        // Child windows report their own move ends; only a top-level window the user dropped can snap.
        if (GetRootWindowHandle(handle) != handle || MonitorNativeMethods.IsIconic(handle) || IsZoomed(handle)) {
            return;
        }

        WindowDpiContext.Execute(() => {
            RECT visible = GetVisibleBounds(handle);
            var window = new WindowInfo { Handle = handle, Left = visible.Left, Top = visible.Top, Right = visible.Right, Bottom = visible.Bottom };
            List<Monitor> monitors = _monitors.GetMonitors(connectedOnly: true, refresh: true);
            Monitor? monitor = WindowMonitorResolver.Resolve(window, monitors);
            if (monitor == null) {
                return;
            }

            var position = new WindowPosition { Left = visible.Left, Top = visible.Top, Right = visible.Right, Bottom = visible.Bottom };
            SnapPosition? snap = GetSnapPosition(position, monitor.GetMonitorWorkArea(), SnapOptions.SnapThreshold);
            if (snap.HasValue) {
                SnapWindow(window, monitor, snap.Value);
            }
        });
    }

    internal static SnapPosition? GetSnapPosition(WindowPosition pos, RECT bounds, int threshold) {
        bool left = Math.Abs(pos.Left - bounds.Left) <= threshold;
        bool right = Math.Abs(pos.Right - bounds.Right) <= threshold;
        bool top = Math.Abs(pos.Top - bounds.Top) <= threshold;
        bool bottom = Math.Abs(pos.Bottom - bounds.Bottom) <= threshold;

        if (left && top) {
            return SnapPosition.TopLeft;
        }
        if (left && bottom) {
            return SnapPosition.BottomLeft;
        }
        if (right && top) {
            return SnapPosition.TopRight;
        }
        if (right && bottom) {
            return SnapPosition.BottomRight;
        }
        if (left) {
            return SnapPosition.Left;
        }
        if (right) {
            return SnapPosition.Right;
        }
        return null;
    }
}
