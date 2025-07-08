using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Linq;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Provides a simple mechanism to keep windows awake by periodically sending
/// a harmless input message. Messages are skipped when the window is
/// currently active to avoid interrupting the user.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowKeepAlive : IDisposable {
    private static readonly Lazy<WindowKeepAlive> _instance = new(() => new WindowKeepAlive());

    /// <summary>
    /// Gets the global instance of the <see cref="WindowKeepAlive"/> service.
    /// </summary>
    public static WindowKeepAlive Instance => _instance.Value;

    private readonly ConcurrentDictionary<IntPtr, Timer> _timers = new();
    private readonly object _syncRoot = new();

    private WindowKeepAlive() {
    }

    /// <summary>
    /// Starts sending keep alive messages to the specified window.
    /// </summary>
    /// <param name="window">Window to keep alive.</param>
    /// <param name="interval">Interval between messages.</param>
    public void Start(WindowInfo window, TimeSpan interval) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }
        Start(window.Handle, interval);
    }

    /// <summary>
    /// Starts sending keep alive messages to the specified window handle.
    /// </summary>
    /// <param name="handle">Handle of the window.</param>
    /// <param name="interval">Interval between messages.</param>
    public void Start(IntPtr handle, TimeSpan interval) {
        if (interval <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        if (_timers.ContainsKey(handle)) {
            return;
        }

        var timer = new Timer(KeepAliveCallback, handle, interval, interval);
        _timers[handle] = timer;
    }

    /// <summary>
    /// Stops sending keep alive messages for the specified window.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    public void Stop(IntPtr handle) {
        if (_timers.TryRemove(handle, out var timer)) {
            timer.Dispose();
        }
    }

    /// <summary>
    /// Stops all keep alive sessions.
    /// </summary>
    public void StopAll() {
        var handles = _timers.Keys.ToList();
        foreach (var handle in handles) {
            Stop(handle);
        }
    }

    /// <summary>
    /// Checks if keep alive is active for the specified window handle.
    /// </summary>
    public bool IsActive(IntPtr handle) {
        lock (_syncRoot) {
            return _timers.ContainsKey(handle);
        }
    }

    /// <summary>
    /// Gets handles currently under keep alive.
    /// </summary>
    public IEnumerable<IntPtr> ActiveHandles => _timers.Keys;

    private void KeepAliveCallback(object? state) {
        if (state is not IntPtr handle) {
            return;
        }

        if (MonitorNativeMethods.GetForegroundWindow() == handle) {
            return;
        }

        MonitorNativeMethods.SendMessage(handle, WM_MOUSEMOVE, 0, 0);
    }

    /// <inheritdoc/>
    public void Dispose() {
        foreach (var timer in _timers.Values) {
            timer.Dispose();
        }
        _timers.Clear();
    }

    private const uint WM_MOUSEMOVE = 0x0200;
}
