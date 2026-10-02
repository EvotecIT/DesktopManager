using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Manages global hotkey registrations.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class HotkeyService : IDisposable {
    private static readonly Lazy<HotkeyService> _instance = new(() => new HotkeyService());

    /// <summary>Gets the shared instance.</summary>
    public static HotkeyService Instance => _instance.Value;

    private readonly Dictionary<int, Action> _callbacks = new();
    private int _nextId;
    private IntPtr _hwnd;
    private Thread? _thread;
    private MonitorNativeMethods.WndProc? _wndProc;
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly Queue<Action> _actions = new();
    private int _disposed;
    private Exception? _startupFailure;
    private const uint WM_RUN = MonitorNativeMethods.WM_APP + 1;

    /// <summary>Creates an independently owned hotkey service. Dispose it to release its registrations.</summary>
    public HotkeyService() {
        _thread = new Thread(MessageLoop) { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!_ready.Wait(10000)) {
            Interlocked.Exchange(ref _disposed, 1);
            throw new TimeoutException("The hotkey message window did not start within 10 seconds.");
        }
        if (_startupFailure != null) {
            ExceptionDispatchInfo.Capture(_startupFailure).Throw();
        }
    }

    private void Invoke(Action action) {
        if (_thread == null || Volatile.Read(ref _disposed) != 0) {
            throw new ObjectDisposedException(nameof(HotkeyService));
        }

        if (Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId) {
            action();
            return;
        }

        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? ex = null;
        int state = 0;
        lock (_actions) {
            if (_disposed != 0) {
                throw new ObjectDisposedException(nameof(HotkeyService));
            }
            if (_actions.Count >= 64) {
                throw new TimeoutException("The hotkey message queue is full; the operation was not started.");
            }
            _actions.Enqueue(() => {
                if (Interlocked.CompareExchange(ref state, 1, 0) != 0) {
                    return;
                }
                try {
                    if (Volatile.Read(ref _disposed) != 0) {
                        throw new ObjectDisposedException(nameof(HotkeyService));
                    }
                    action();
                } catch (Exception e) {
                    ex = e;
                } finally {
                    done.TrySetResult(true);
                }
            });
            if (!MonitorNativeMethods.PostMessage(_hwnd, WM_RUN, IntPtr.Zero, IntPtr.Zero)) {
                Interlocked.CompareExchange(ref state, 2, 0);
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        if (!done.Task.Wait(5000)) {
            if (Interlocked.CompareExchange(ref state, 2, 0) == 0) {
                throw new TimeoutException("The hotkey operation was canceled before it started.");
            }
            throw new NativeOperationOutcomeUnknownException("Hotkey registration", 5000);
        }
        if (ex != null) {
            ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }

    /// <summary>Window handle used for hotkey messages.</summary>
    internal IntPtr WindowHandle => _hwnd;

    /// <summary>
    /// Registers a hotkey and associates a callback.
    /// </summary>
    /// <param name="modifiers">Modifier keys.</param>
    /// <param name="key">Virtual key.</param>
    /// <param name="callback">Callback invoked when the hotkey fires.</param>
    /// <returns>Identifier of the registration.</returns>
    public int RegisterHotkey(HotkeyModifiers modifiers, VirtualKey key, Action callback) {
        if (callback == null) {
            throw new ArgumentNullException(nameof(callback));
        }

        int id = 0;
        Invoke(() => {
            id = ++_nextId;
            _callbacks[id] = callback;

            if (!MonitorNativeMethods.RegisterHotKey(_hwnd, id, (uint)modifiers, (uint)key)) {
                _callbacks.Remove(id);
                int error = Marshal.GetLastWin32Error();
                var ex = new System.ComponentModel.Win32Exception(error);
                throw new DesktopManagerException("RegisterHotKey", ex);
            }
        });

        return id;
    }

    /// <summary>
    /// Unregisters a previously registered hotkey.
    /// </summary>
    /// <param name="id">Identifier returned from <see cref="RegisterHotkey"/>.</param>
    public void UnregisterHotkey(int id) {
        Invoke(() => {
            MonitorNativeMethods.UnregisterHotKey(_hwnd, id);
            _callbacks.Remove(id);
        });
    }

    private void MessageLoop() {
        _wndProc = WndProc;
        _hwnd = MonitorNativeMethods.CreateWindowExW(0, "Message", string.Empty, 0, 0, 0, 0, 0,
            MonitorNativeMethods.HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero) {
            _startupFailure = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            _ready.Set();
            return;
        }
        MonitorNativeMethods.SetWindowLongPtr(_hwnd, MonitorNativeMethods.GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_wndProc));
        _ready.Set();

        MonitorNativeMethods.MSG msg;
        while (Volatile.Read(ref _disposed) == 0 && MonitorNativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0) > 0) {
            MonitorNativeMethods.TranslateMessage(ref msg);
            MonitorNativeMethods.DispatchMessage(ref msg);
        }

        if (_hwnd != IntPtr.Zero) {
            MonitorNativeMethods.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
        lock (_actions) {
            while (_actions.Count > 0) {
                _actions.Dequeue()();
            }
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
        if (msg == MonitorNativeMethods.WM_HOTKEY) {
            int id = wParam.ToInt32();
            Action? callback;
            lock (_callbacks) {
                _callbacks.TryGetValue(id, out callback);
            }
            if (Volatile.Read(ref _disposed) == 0) {
                try {
                    callback?.Invoke();
                } catch (Exception ex) {
                    DesktopManagerDiagnostics.Report($"Hotkey callback failed: {ex.Message}");
                }
            }
            return IntPtr.Zero;
        }

        if (msg == WM_RUN) {
            while (true) {
                Action? next = null;
                lock (_actions) {
                    if (_actions.Count > 0) {
                        next = _actions.Dequeue();
                    }
                }
                if (next == null) {
                    break;
                }
                next();
            }
            return IntPtr.Zero;
        }

        return MonitorNativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }
        if (_hwnd != IntPtr.Zero) {
            MonitorNativeMethods.PostMessage(_hwnd, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            if (_thread != null && Thread.CurrentThread != _thread && !_thread.Join(5000)) {
                DesktopManagerDiagnostics.Report("Hotkey shutdown is still pending on a callback.");
            }
        }
        _ready.Dispose();
    }
}
