using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Reports window moves, resizes, shows, destroys and foreground changes across the desktop through
/// WinEvent hooks on a dedicated message thread.
/// </summary>
/// <remarks>
/// <para>Only events for a window itself are reported, not for its caret, cursor or accessible
/// children. Child windows are included; compare the handle with its root ancestor to keep
/// top-level windows only.</para>
/// <para><see cref="WindowChanged"/> is raised on the watcher's hook thread. Keep handlers short
/// and hand longer work to another thread, or later events queue behind it.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowEventWatcher : IDisposable {
    private readonly Thread _thread;
    private readonly ThreadStartup _ready = new();
    private readonly MonitorNativeMethods.WinEventDelegate _callback;
    private readonly List<IntPtr> _hooks = new();
    private readonly uint _flags;
    private uint _threadId;
    private volatile bool _disposed;

    /// <summary>
    /// Starts watching for the chosen window events.
    /// </summary>
    /// <param name="kinds">The events to watch; each selected kind installs one hook.</param>
    /// <param name="skipOwnProcess">True to ignore windows that belong to the current process.</param>
    /// <exception cref="ArgumentException"><paramref name="kinds"/> selects no events.</exception>
    /// <exception cref="InvalidOperationException">Windows refused a hook.</exception>
    /// <exception cref="TimeoutException">The hook thread did not report readiness within five seconds.
    /// Stop is requested, with up to two more seconds for the thread to exit.</exception>
    public WindowEventWatcher(WindowEventKinds kinds = WindowEventKinds.All, bool skipOwnProcess = false) {
        kinds &= WindowEventKinds.All;
        if (kinds == WindowEventKinds.None) {
            throw new ArgumentException("Select at least one window event to watch.", nameof(kinds));
        }

        Kinds = kinds;
        _flags = MonitorNativeMethods.WINEVENT_OUTOFCONTEXT | (skipOwnProcess ? MonitorNativeMethods.WINEVENT_SKIPOWNPROCESS : 0);
        _callback = OnWinEvent;
        _thread = new Thread(Run) { IsBackground = true, Name = "DesktopManager window events" };
        try {
            _ready.Start(_thread, "window event thread");
        } catch {
            _disposed = true;
            MonitorNativeMethods.PostThreadMessage(_threadId, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            if (_thread.IsAlive) {
                _thread.Join(TimeSpan.FromSeconds(2));
            }
            throw;
        }
    }

    /// <summary>Occurs on the hook thread when a watched window event happens.</summary>
    public event EventHandler<WindowChangedEventArgs>? WindowChanged;

    /// <summary>Gets the events this watcher reports.</summary>
    public WindowEventKinds Kinds { get; }

    private void Run() {
        try {
            Exception? startError = null;
            try {
                _threadId = MonitorNativeMethods.GetCurrentThreadId();
                // Create the queue before readiness; late startup also observes _disposed below.
                MonitorNativeMethods.PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
                foreach (WindowEventKinds kind in new[] {
                    WindowEventKinds.MoveSizeStarted, WindowEventKinds.MoveSizeEnded, WindowEventKinds.LocationChanged,
                    WindowEventKinds.Shown, WindowEventKinds.Destroyed, WindowEventKinds.Foreground }) {
                    if (_disposed || (Kinds & kind) == 0) {
                        continue;
                    }

                    uint native = ToNative(kind);
                    IntPtr hook = MonitorNativeMethods.SetWinEventHook(native, native, IntPtr.Zero, _callback, 0, 0, _flags);
                    if (hook == IntPtr.Zero) {
                        throw new InvalidOperationException($"Windows refused the {kind} window event hook.");
                    }

                    _hooks.Add(hook);
                }
            } catch (Exception exception) {
                startError = exception;
            }

            _ready.Signal(startError);
            if (startError != null) {
                return;
            }

            while (!_disposed) {
                if (!MonitorNativeMethods.PeekMessage(out MonitorNativeMethods.MSG message, IntPtr.Zero, 0, 0, 1)) {
                    MonitorNativeMethods.MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, 100, 0x04FF, 4);
                    continue;
                }
                if (message.message == MonitorNativeMethods.WM_QUIT) {
                    break;
                }
                MonitorNativeMethods.TranslateMessage(ref message);
                MonitorNativeMethods.DispatchMessage(ref message);
            }
        } finally {
            foreach (IntPtr hook in _hooks) {
                MonitorNativeMethods.UnhookWinEvent(hook);
            }

            _hooks.Clear();
        }
    }

    private void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint eventThread, uint eventTime) {
        if (_disposed || hwnd == IntPtr.Zero || idObject != MonitorNativeMethods.OBJID_WINDOW || idChild != MonitorNativeMethods.CHILDID_SELF) {
            return;
        }

        WindowEventKinds kind = FromNative(eventType);
        EventHandler<WindowChangedEventArgs>? handler = WindowChanged;
        if (kind == WindowEventKinds.None || handler == null) {
            return;
        }

        try {
            handler(this, new WindowChangedEventArgs(kind, hwnd));
        } catch (Exception ex) {
            // An exception must not unwind into the native WinEvent dispatcher.
            DesktopManagerDiagnostics.Report($"Window event handler failed: {ex.Message}");
        }
    }

    private static uint ToNative(WindowEventKinds kind) => kind switch {
        WindowEventKinds.MoveSizeStarted => MonitorNativeMethods.EVENT_SYSTEM_MOVESIZESTART,
        WindowEventKinds.MoveSizeEnded => MonitorNativeMethods.EVENT_SYSTEM_MOVESIZEEND,
        WindowEventKinds.LocationChanged => MonitorNativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
        WindowEventKinds.Shown => MonitorNativeMethods.EVENT_OBJECT_SHOW,
        WindowEventKinds.Destroyed => MonitorNativeMethods.EVENT_OBJECT_DESTROY,
        _ => MonitorNativeMethods.EVENT_SYSTEM_FOREGROUND,
    };

    private static WindowEventKinds FromNative(uint eventType) => eventType switch {
        MonitorNativeMethods.EVENT_SYSTEM_MOVESIZESTART => WindowEventKinds.MoveSizeStarted,
        MonitorNativeMethods.EVENT_SYSTEM_MOVESIZEEND => WindowEventKinds.MoveSizeEnded,
        MonitorNativeMethods.EVENT_OBJECT_LOCATIONCHANGE => WindowEventKinds.LocationChanged,
        MonitorNativeMethods.EVENT_OBJECT_SHOW => WindowEventKinds.Shown,
        MonitorNativeMethods.EVENT_OBJECT_DESTROY => WindowEventKinds.Destroyed,
        MonitorNativeMethods.EVENT_SYSTEM_FOREGROUND => WindowEventKinds.Foreground,
        _ => WindowEventKinds.None,
    };

    /// <summary>Stops watching and removes the hooks.</summary>
    /// <exception cref="TimeoutException">The hook thread did not stop within two seconds.</exception>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        WindowChanged = null;
        MonitorNativeMethods.PostThreadMessage(_threadId, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        if (Thread.CurrentThread == _thread) {
            // Disposed from a handler: the loop ends and removes the hooks once the handler returns.
            return;
        }

        if (!_thread.Join(TimeSpan.FromSeconds(2))) {
            throw new TimeoutException("The window event thread did not stop.");
        }
    }
}
