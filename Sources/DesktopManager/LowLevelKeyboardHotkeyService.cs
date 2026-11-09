using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Manages process-local global hotkeys through a low-level keyboard hook.
/// </summary>
/// <remarks>
/// This backend is intended for applications such as remote desktop clients that can
/// forward keyboard chords before the normal <see cref="HotkeyService"/> message
/// based registration sees them. Matched chords are consumed.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class LowLevelKeyboardHotkeyService : IDisposable {
    private static readonly WorkerGate InstanceGate = new();
    private static LowLevelKeyboardHotkeyService? _instance;

    /// <summary>Gets the shared instance.</summary>
    /// <exception cref="TimeoutException">The hook thread did not report readiness within five seconds.</exception>
    public static LowLevelKeyboardHotkeyService Instance {
        get {
            var instance = Volatile.Read(ref _instance);
            if (instance != null) { return instance; }
            InstanceGate.Enter("Hotkey service startup");
            try {
                instance = _instance;
                if (instance == null) { Volatile.Write(ref _instance, instance = new LowLevelKeyboardHotkeyService()); }
                return instance;
            } finally { InstanceGate.Exit(); }
        }
    }

    private readonly Dictionary<int, LowLevelKeyboardHotkeyRegistration> _registrations = new();
    private readonly HashSet<int> _pressedRegistrations = new();
    private readonly HashSet<VirtualKey> _pressedKeys = new();
    private readonly ThreadStartup _ready = new();
    private readonly Queue<Action> _actions = new();
    private readonly object _syncRoot = new();
    private int _nextId;
    private IntPtr _hookHandle;
    private uint _threadId;
    private Thread? _thread;
    private MonitorNativeMethods.LowLevelKeyboardProc? _hookProc;
    private Exception? _startupException;
    private volatile bool _stopping;
    private const uint WM_RUN = MonitorNativeMethods.WM_APP + 2;

    private LowLevelKeyboardHotkeyService() {
        _thread = new Thread(MessageLoop) { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA);
        try {
            _ready.Start(_thread, "low-level keyboard hook thread");
        } catch {
            _stopping = true;
            MonitorNativeMethods.PostThreadMessage(_threadId, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            throw;
        }
    }

    /// <summary>
    /// Registers a low-level hook hotkey and consumes the matching chord.
    /// </summary>
    /// <param name="modifiers">Required modifier keys.</param>
    /// <param name="key">Required trigger key.</param>
    /// <param name="callback">Callback invoked when the hotkey fires.</param>
    /// <returns>Identifier of the registration.</returns>
    public int RegisterHotkey(HotkeyModifiers modifiers, VirtualKey key, Action callback) {
        if (callback == null) {
            throw new ArgumentNullException(nameof(callback));
        }

        return RegisterHotkey(modifiers, key, _ => callback());
    }

    /// <summary>
    /// Registers a low-level hook hotkey and consumes the matching chord.
    /// </summary>
    /// <param name="modifiers">Required modifier keys.</param>
    /// <param name="key">Required trigger key.</param>
    /// <param name="callback">Callback invoked with the foreground window captured by the hook.</param>
    /// <returns>Identifier of the registration.</returns>
    public int RegisterHotkey(HotkeyModifiers modifiers, VirtualKey key, Action<IntPtr> callback) {
        return RegisterHotkey(modifiers, key, options: null, callback);
    }

    /// <summary>
    /// Registers a low-level hook hotkey and consumes the matching chord.
    /// </summary>
    /// <param name="modifiers">Required modifier keys.</param>
    /// <param name="key">Required trigger key.</param>
    /// <param name="options">Hook capture options.</param>
    /// <param name="callback">Callback invoked with the foreground window captured by the hook.</param>
    /// <returns>Identifier of the registration.</returns>
    public int RegisterHotkey(HotkeyModifiers modifiers, VirtualKey key, LowLevelKeyboardHotkeyOptions? options, Action<IntPtr> callback) {
        if (callback == null) {
            throw new ArgumentNullException(nameof(callback));
        }

        int id = 0;
        Invoke(() => {
            id = ++_nextId;
            _registrations[id] = new LowLevelKeyboardHotkeyRegistration(id, modifiers, key, callback, options);
        });

        return id;
    }

    /// <summary>
    /// Unregisters a previously registered low-level hook hotkey.
    /// </summary>
    /// <param name="id">Identifier returned from a registration call.</param>
    public void UnregisterHotkey(int id) {
        Invoke(() => {
            lock (_syncRoot) {
                _registrations.Remove(id);
                _pressedRegistrations.Remove(id);
                if (_registrations.Count == 0) {
                    _pressedKeys.Clear();
                }
            }
        });
    }

    private void Invoke(Action action) {
        Thread? thread = _thread;
        if (thread == null || _stopping) {
            throw new ObjectDisposedException(nameof(LowLevelKeyboardHotkeyService));
        }

        if (Thread.CurrentThread == thread) {
            action();
            return;
        }

        using var invocation = new WorkerInvocation<bool>();
        bool admitted = false;
        try {
            lock (_actions) {
                if (_stopping) { throw new ObjectDisposedException(nameof(LowLevelKeyboardHotkeyService)); }
                if (_actions.Count >= 64) { throw new TimeoutException("The hotkey message queue is full; the operation was not started."); }
                _actions.Enqueue(() => invocation.Execute(() => {
                    if (_stopping) { throw new ObjectDisposedException(nameof(LowLevelKeyboardHotkeyService)); }
                    action();
                    return true;
                }));
                admitted = true;
                if (!MonitorNativeMethods.PostThreadMessage(_threadId, WM_RUN, IntPtr.Zero, IntPtr.Zero)) {
                    invocation.CancelPending();
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        } catch {
            if (!admitted) { invocation.Reject(); }
            throw;
        }
        invocation.GetResult("Hotkey registration", 5000);
    }

    private void MessageLoop() {
        try {
            try {
                _threadId = MonitorNativeMethods.GetCurrentThreadId();
                MonitorNativeMethods.PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
                _hookProc = HookCallback;
                _hookHandle = MonitorNativeMethods.SetWindowsHookEx(
                    MonitorNativeMethods.WH_KEYBOARD_LL,
                    _hookProc,
                    MonitorNativeMethods.GetModuleHandle(null),
                    0);

                if (_hookHandle == IntPtr.Zero) {
                    int error = Marshal.GetLastWin32Error();
                    _startupException = new DesktopManagerException(
                        "SetWindowsHookEx",
                        new System.ComponentModel.Win32Exception(error));
                }
            } catch (Exception exception) {
                _startupException = exception;
            }
            _ready.Signal(_startupException);
            if (_startupException != null) {
                return;
            }
            while (!_stopping) {
                if (!MonitorNativeMethods.PeekMessage(out MonitorNativeMethods.MSG msg, IntPtr.Zero, 0, 0, 1)) {
                    MonitorNativeMethods.MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, 100, 0x04FF, 4);
                    continue;
                }
                if (msg.message == MonitorNativeMethods.WM_QUIT) {
                    break;
                }
                if (msg.message == WM_RUN) {
                    RunQueuedActions();
                    continue;
                }

                MonitorNativeMethods.TranslateMessage(ref msg);
                MonitorNativeMethods.DispatchMessage(ref msg);
            }
        } finally {
            // Stop can bypass queued WM_RUN messages. Complete their waiters with disposal errors.
            RunQueuedActions();
            if (_hookHandle != IntPtr.Zero) {
                MonitorNativeMethods.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }

    private void RunQueuedActions() {
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
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
        if (nCode < 0 || _stopping) {
            return MonitorNativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var keyboard = Marshal.PtrToStructure<MonitorNativeMethods.KBDLLHOOKSTRUCT>(lParam);
        var key = (VirtualKey)keyboard.vkCode;
        int message = wParam.ToInt32();

        if (message == MonitorNativeMethods.WM_KEYUP_HOOK || message == MonitorNativeMethods.WM_SYSKEYUP_HOOK) {
            if (ReleasePressedRegistrations(key)) {
                TrackKeyUp(key);
                return new IntPtr(1);
            }

            TrackKeyUp(key);

            return MonitorNativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        if (message != MonitorNativeMethods.WM_KEYDOWN_HOOK && message != MonitorNativeMethods.WM_SYSKEYDOWN_HOOK) {
            return MonitorNativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        TrackKeyDown(key);
        if (TryHandleKeyDown(key)) {
            return new IntPtr(1);
        }

        return MonitorNativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private bool TryHandleKeyDown(VirtualKey key) {
        List<Action<IntPtr>> callbacks = new();
        IntPtr foregroundHandle = MonitorNativeMethods.GetForegroundWindow();
        lock (_syncRoot) {
            foreach (LowLevelKeyboardHotkeyRegistration registration in _registrations.Values) {
                if (!CanCaptureRegistration(registration, key, IsTrackedKeyDown, foregroundHandle)) {
                    continue;
                }

                if (_pressedRegistrations.Contains(registration.Id)) {
                    return true;
                }

                _pressedRegistrations.Add(registration.Id);
                callbacks.Add(registration.Callback);
            }
        }

        if (callbacks.Count == 0) {
            return false;
        }

        foreach (Action<IntPtr> callback in callbacks) {
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    callback(foregroundHandle);
                } catch (Exception ex) {
                    DesktopManagerDiagnostics.Report($"Low-level hotkey callback failed: {ex.Message}");
                }
            });
        }

        return true;
    }

    private bool ReleasePressedRegistrations(VirtualKey key) {
        lock (_syncRoot) {
            List<int>? released = null;
            foreach (LowLevelKeyboardHotkeyRegistration registration in _registrations.Values) {
                if (registration.Key != key) {
                    continue;
                }

                if (_pressedRegistrations.Contains(registration.Id)) {
                    released ??= new List<int>();
                    released.Add(registration.Id);
                }
            }

            if (released == null) {
                return false;
            }

            foreach (int id in released) {
                _pressedRegistrations.Remove(id);
            }

            return true;
        }
    }

    private void TrackKeyDown(VirtualKey key) {
        lock (_syncRoot) {
            _pressedKeys.Add(key);
        }
    }

    private void TrackKeyUp(VirtualKey key) {
        lock (_syncRoot) {
            _pressedKeys.Remove(key);
        }
    }

    private bool IsTrackedKeyDown(VirtualKey key) {
        return _pressedKeys.Contains(key);
    }

    internal static bool ForegroundProcessMatches(IntPtr foregroundHandle, IReadOnlyList<string> processNames) {
        if (processNames.Count == 0) {
            return true;
        }

        if (foregroundHandle == IntPtr.Zero) {
            return false;
        }

        try {
            MonitorNativeMethods.GetWindowThreadProcessId(foregroundHandle, out uint processId);
            if (processId == 0) {
                return false;
            }

            using Process process = Process.GetProcessById(unchecked((int)processId));
            foreach (string processName in processNames) {
                if (string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
        } catch {
            return false;
        }

        return false;
    }

    internal static bool CanCaptureRegistration(
        LowLevelKeyboardHotkeyRegistration registration,
        VirtualKey key,
        Func<VirtualKey, bool> isKeyDown,
        IntPtr foregroundHandle) {
        return registration.Matches(key, isKeyDown);
    }

    /// <inheritdoc />
    public void Dispose() {
        Thread? thread;
        lock (_actions) {
            thread = _thread;
            if (thread == null) {
                return;
            }
            _stopping = true;
            MonitorNativeMethods.PostThreadMessage(_threadId, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }
        if (thread != null) {
            if (Thread.CurrentThread != thread && !thread.Join(TimeSpan.FromSeconds(2))) {
                throw new TimeoutException("The low-level keyboard hook thread did not stop within two seconds.");
            }
            _thread = null;
        }
    }
}

internal sealed class LowLevelKeyboardHotkeyRegistration {
    public LowLevelKeyboardHotkeyRegistration(int id, HotkeyModifiers modifiers, VirtualKey key, Action<IntPtr> callback, LowLevelKeyboardHotkeyOptions? options = null) {
        Id = id;
        Modifiers = modifiers & ~HotkeyModifiers.NoRepeat;
        Key = key;
        Callback = callback;
        Options = options ?? new LowLevelKeyboardHotkeyOptions();
    }

    public int Id { get; }
    public HotkeyModifiers Modifiers { get; }
    public VirtualKey Key { get; }
    public Action<IntPtr> Callback { get; }
    public LowLevelKeyboardHotkeyOptions Options { get; }

    public bool Matches(VirtualKey key, Func<VirtualKey, bool> isKeyDown) {
        return Key == key &&
            IsModifierSatisfied(HotkeyModifiers.Control, VirtualKey.VK_CONTROL, VirtualKey.VK_LCONTROL, VirtualKey.VK_RCONTROL, isKeyDown) &&
            IsModifierSatisfied(HotkeyModifiers.Alt, VirtualKey.VK_MENU, VirtualKey.VK_LMENU, VirtualKey.VK_RMENU, isKeyDown) &&
            IsModifierSatisfied(HotkeyModifiers.Shift, VirtualKey.VK_SHIFT, VirtualKey.VK_LSHIFT, VirtualKey.VK_RSHIFT, isKeyDown) &&
            IsModifierSatisfied(HotkeyModifiers.Win, VirtualKey.VK_LWIN, VirtualKey.VK_LWIN, VirtualKey.VK_RWIN, isKeyDown) &&
            HasOnlyRequiredModifiersDown(isKeyDown);
    }

    public bool HasOnlyRequiredModifiersDown(Func<VirtualKey, bool> isKeyDown) {
        return IsOptionalModifierAllowed(HotkeyModifiers.Control, VirtualKey.VK_CONTROL, VirtualKey.VK_LCONTROL, VirtualKey.VK_RCONTROL, isKeyDown) &&
            IsOptionalModifierAllowed(HotkeyModifiers.Alt, VirtualKey.VK_MENU, VirtualKey.VK_LMENU, VirtualKey.VK_RMENU, isKeyDown) &&
            IsOptionalModifierAllowed(HotkeyModifiers.Shift, VirtualKey.VK_SHIFT, VirtualKey.VK_LSHIFT, VirtualKey.VK_RSHIFT, isKeyDown) &&
            IsOptionalModifierAllowed(HotkeyModifiers.Win, VirtualKey.VK_LWIN, VirtualKey.VK_LWIN, VirtualKey.VK_RWIN, isKeyDown);
    }

    private bool IsModifierSatisfied(
        HotkeyModifiers modifier,
        VirtualKey commonKey,
        VirtualKey leftKey,
        VirtualKey rightKey,
        Func<VirtualKey, bool> isKeyDown) {
        if (!Modifiers.HasFlag(modifier)) {
            return true;
        }

        return isKeyDown(commonKey) || isKeyDown(leftKey) || isKeyDown(rightKey);
    }

    private bool IsOptionalModifierAllowed(
        HotkeyModifiers modifier,
        VirtualKey commonKey,
        VirtualKey leftKey,
        VirtualKey rightKey,
        Func<VirtualKey, bool> isKeyDown) {
        bool anyDown = IsAnyDown(commonKey, leftKey, rightKey, isKeyDown);
        return Modifiers.HasFlag(modifier) || !anyDown;
    }

    private static bool IsAnyDown(VirtualKey commonKey, VirtualKey leftKey, VirtualKey rightKey, Func<VirtualKey, bool> isKeyDown) {
        return isKeyDown(commonKey) || isKeyDown(leftKey) || isKeyDown(rightKey);
    }

    private static bool IsControlKey(VirtualKey key) {
        return key == VirtualKey.VK_CONTROL || key == VirtualKey.VK_LCONTROL || key == VirtualKey.VK_RCONTROL;
    }

    private static bool IsAltKey(VirtualKey key) {
        return key == VirtualKey.VK_MENU || key == VirtualKey.VK_LMENU || key == VirtualKey.VK_RMENU;
    }

    private static bool IsShiftKey(VirtualKey key) {
        return key == VirtualKey.VK_SHIFT || key == VirtualKey.VK_LSHIFT || key == VirtualKey.VK_RSHIFT;
    }

    private static bool IsWinKey(VirtualKey key) {
        return key == VirtualKey.VK_LWIN || key == VirtualKey.VK_RWIN;
    }
}
