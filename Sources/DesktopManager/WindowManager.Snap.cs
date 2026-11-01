using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager;

public partial class WindowManager {
    private MonitorNativeMethods.WinEventDelegate? _moveDelegate;
    private IntPtr _moveHook;

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
    /// Starts automatic snapping when windows are moved near edges.
    /// </summary>
    public void StartAutoSnap() {
        if (_moveHook != IntPtr.Zero) {
            return;
        }

        _moveDelegate = OnMoveEnd;
        _moveHook = MonitorNativeMethods.SetWinEventHook(
            MonitorNativeMethods.EVENT_SYSTEM_MOVESIZEEND,
            MonitorNativeMethods.EVENT_SYSTEM_MOVESIZEEND,
            IntPtr.Zero,
            _moveDelegate,
            0,
            0,
            MonitorNativeMethods.WINEVENT_OUTOFCONTEXT);
    }

    /// <summary>
    /// Stops automatic snapping.
    /// </summary>
    public void StopAutoSnap() {
        if (_moveHook != IntPtr.Zero) {
            MonitorNativeMethods.UnhookWinEvent(_moveHook);
            _moveHook = IntPtr.Zero;
        }
    }

    private void OnMoveEnd(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
        if (hwnd == IntPtr.Zero) {
            return;
        }

        var window = new WindowInfo { Handle = hwnd };
        var position = GetWindowPosition(window);
        var monitor = _monitors.GetMonitors().FirstOrDefault(m => {
            var b = m.GetMonitorBounds();
            return position.Left >= b.Left && position.Left < b.Right && position.Top >= b.Top && position.Top < b.Bottom;
        });
        if (monitor == null) {
            return;
        }

        var bounds = monitor.GetMonitorBounds();
        var snap = GetSnapPosition(position, bounds, SnapOptions.SnapThreshold);
        if (snap.HasValue) {
            SnapWindow(window, snap.Value);
        }
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
