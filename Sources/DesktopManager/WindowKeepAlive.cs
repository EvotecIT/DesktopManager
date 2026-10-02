using System;
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

    private readonly object _sync = new();
    private readonly Dictionary<IntPtr, Session> _sessions = new();

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
        if (handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle", nameof(handle));
        }
        if (interval <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        lock (_sync) {
            if (_sessions.ContainsKey(handle)) {
                return;
            }
            uint threadId = MonitorNativeMethods.GetWindowThreadProcessId(handle, out uint processId);
            var session = new Session(handle, processId, threadId);
            session.Timer = new Timer(KeepAliveCallback, session, Timeout.Infinite, Timeout.Infinite);
            try {
                session.Timer.Change(interval, interval);
                _sessions.Add(handle, session);
            } catch {
                session.Timer.Dispose();
                throw;
            }
        }
    }

    /// <summary>
    /// Stops sending keep alive messages for the specified window.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    public void Stop(IntPtr handle) {
        lock (_sync) {
            if (_sessions.TryGetValue(handle, out Session? session)) {
                _sessions.Remove(handle);
                session.Timer?.Dispose();
            }
        }
    }

    /// <summary>
    /// Stops all keep alive sessions.
    /// </summary>
    public void StopAll() {
        lock (_sync) {
            foreach (Session session in _sessions.Values) {
                session.Timer?.Dispose();
            }
            _sessions.Clear();
        }
    }

    /// <summary>
    /// Checks if keep alive is active for the specified window handle.
    /// </summary>
    public bool IsActive(IntPtr handle) {
        lock (_sync) {
            return _sessions.ContainsKey(handle);
        }
    }

    /// <summary>
    /// Gets handles currently under keep alive.
    /// </summary>
    public IEnumerable<IntPtr> ActiveHandles {
        get {
            lock (_sync) {
                return _sessions.Keys.ToArray();
            }
        }
    }

    private void KeepAliveCallback(object? state) {
        if (state is not Session session || Interlocked.Exchange(ref session.Running, 1) != 0) {
            return;
        }

        try {
            lock (_sync) {
                if (!_sessions.TryGetValue(session.Handle, out Session? current) || current != session) {
                    return;
                }
                uint threadId = MonitorNativeMethods.GetWindowThreadProcessId(session.Handle, out uint processId);
                if (threadId == 0 || session.ProcessId == 0 ||
                        processId != session.ProcessId || threadId != session.ThreadId) {
                    Stop(session.Handle);
                    return;
                }
                if (MonitorNativeMethods.GetForegroundWindow() != session.Handle) {
                    MonitorNativeMethods.SendMessageTimeout(session.Handle, WM_MOUSEMOVE, IntPtr.Zero, IntPtr.Zero,
                        MonitorNativeMethods.SMTO_ABORTIFHUNG, 200, out _);
                }
            }
        } finally {
            Volatile.Write(ref session.Running, 0);
        }
    }

    /// <summary>Stops all current sessions. The shared service can subsequently start new sessions.</summary>
    public void Dispose() {
        StopAll();
    }

    private sealed class Session {
        internal Session(IntPtr handle, uint processId, uint threadId) {
            Handle = handle;
            ProcessId = processId;
            ThreadId = threadId;
        }
        internal readonly IntPtr Handle;
        internal readonly uint ProcessId;
        internal readonly uint ThreadId;
        internal Timer? Timer;
        internal int Running;
    }

    private const uint WM_MOUSEMOVE = 0x0200;
}
