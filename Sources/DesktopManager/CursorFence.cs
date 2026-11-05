using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Keeps the mouse pointer out of a display rectangle, or makes it stick at the rectangle's edge,
/// through a low-level mouse hook on its own message thread.
/// </summary>
/// <remarks>
/// <para>Touch and pen input still reach the rectangle, because Windows marks the mouse messages it
/// promotes from them. Holding Control lets the mouse cross in either mode, and a pointer already
/// inside the rectangle can always move and leave.</para>
/// <para>All geometry is in physical virtual-desktop pixels. The fence starts when constructed and
/// stops when disposed.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class CursorFence : IDisposable {
    private static readonly int ExtraInfoOffset = IntPtr.Size == 8 ? 24 : 20;
    private const int TimeOffset = 16;

    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly MonitorNativeMethods.LowLevelKeyboardProc _callback;
    private Exception? _startError;
    private IntPtr _hook;
    private uint _threadId;
    private int _stopError;
    private volatile bool _stopping;
    private bool _correcting;
    private bool _disposed;
    private FencePush _push;

    /// <summary>
    /// Starts a fence around <paramref name="bounds"/>.
    /// </summary>
    /// <param name="bounds">The guarded rectangle in physical pixels; right and bottom are exclusive.</param>
    /// <param name="mode"><see cref="CursorFenceMode.Sticky"/> or <see cref="CursorFenceMode.Locked"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="mode"/> is <see cref="CursorFenceMode.Off"/>.</exception>
    /// <exception cref="Win32Exception">Windows refused the mouse hook.</exception>
    public CursorFence(RECT bounds, CursorFenceMode mode) {
        if (mode == CursorFenceMode.Off) {
            throw new ArgumentException("Off does not need a mouse hook.", nameof(mode));
        }

        Bounds = bounds;
        Mode = mode;
        // The hook API takes the same (code, wParam, lParam) signature for keyboard and mouse hooks.
        _callback = MouseHook;
        _thread = new Thread(Run) { IsBackground = true, Name = "DesktopManager cursor fence" };
        _thread.Start();
        _ready.Wait();
        if (_startError != null) {
            _thread.Join();
            _ready.Dispose();
            throw _startError;
        }
    }

    /// <summary>Gets the guarded rectangle in physical pixels.</summary>
    public RECT Bounds { get; }

    /// <summary>Gets the fence mode.</summary>
    public CursorFenceMode Mode { get; }

    private void Run() {
        // Hook points, GetCursorPos, SetCursorPos and Bounds must all be physical pixels, whatever the host's DPI awareness.
        try {
            MonitorNativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));
        } catch (EntryPointNotFoundException) {
            // Before Windows 10 1607 there is no per-thread DPI context; the process setting applies.
        }
        _threadId = MonitorNativeMethods.GetCurrentThreadId();
        // Create the message queue before publishing readiness, so Dispose can always post WM_QUIT.
        MonitorNativeMethods.PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        try {
            _hook = MonitorNativeMethods.SetWindowsHookEx(
                MonitorNativeMethods.WH_MOUSE_LL,
                _callback,
                MonitorNativeMethods.GetModuleHandle(null),
                0);
            if (_hook == IntPtr.Zero) {
                _startError = new Win32Exception(Marshal.GetLastWin32Error());
            }

            _ready.Set();
            if (_startError != null) {
                return;
            }

            while (MonitorNativeMethods.GetMessage(out MonitorNativeMethods.MSG message, IntPtr.Zero, 0, 0) > 0) {
                MonitorNativeMethods.TranslateMessage(ref message);
                MonitorNativeMethods.DispatchMessage(ref message);
            }
        } finally {
            RemoveHook();
        }
    }

    private IntPtr MouseHook(int code, IntPtr message, IntPtr data) {
        if (code < 0 || message.ToInt64() != MonitorNativeMethods.WM_MOUSEMOVE_HOOK || _stopping || _correcting) {
            return MonitorNativeMethods.CallNextHookEx(IntPtr.Zero, code, message, data);
        }

        if (!MonitorNativeMethods.GetCursorPos(out MonitorNativeMethods.POINT current)) {
            return MonitorNativeMethods.CallNextHookEx(IntPtr.Zero, code, message, data);
        }

        // MSLLHOOKSTRUCT: pt, mouseData, flags, time, dwExtraInfo. Read fields directly so the hot path does not allocate.
        var proposed = new FencePoint(Marshal.ReadInt32(data, 0), Marshal.ReadInt32(data, 4));
        uint time = unchecked((uint)Marshal.ReadInt32(data, TimeOffset));
        ulong extraInfo = unchecked((ulong)Marshal.ReadIntPtr(data, ExtraInfoOffset).ToInt64());
        bool control = (MonitorNativeMethods.GetAsyncKeyState(MonitorNativeMethods.VK_CONTROL_KEY) & 0x8000) != 0;
        FenceDecision decision = CursorFenceRules.Decide(new FencePoint(current.x, current.y), proposed, Bounds,
            Mode, control, extraInfo, time, _push);
        _push = decision.Push;
        if (decision.Point.Equals(proposed)) {
            return MonitorNativeMethods.CallNextHookEx(IntPtr.Zero, code, message, data);
        }

        // SetCursorPos can re-enter the hook. Corrections pass straight through; nothing on this
        // callback may queue, log, lock or wait, or Windows drops the hook for timing out.
        _correcting = true;
        bool moved = MonitorNativeMethods.SetCursorPos(decision.Point.X, decision.Point.Y);
        _correcting = false;
        return moved ? new IntPtr(1) : MonitorNativeMethods.CallNextHookEx(IntPtr.Zero, code, message, data);
    }

    /// <summary>Stops the fence and removes the mouse hook.</summary>
    /// <exception cref="Win32Exception">The hook could not be removed cleanly.</exception>
    /// <exception cref="TimeoutException">The fence thread did not stop within two seconds.</exception>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _stopping = true; // Fail open at once, even if a callback is already pending.
        if (!MonitorNativeMethods.PostThreadMessage(_threadId, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero) && _thread.IsAlive) {
            int error = Marshal.GetLastWin32Error();
            RemoveHook();
            throw new Win32Exception(error);
        }

        if (!_thread.Join(TimeSpan.FromSeconds(2))) {
            RemoveHook();
            throw new TimeoutException("The cursor fence thread did not stop.");
        }

        _ready.Dispose();
        if (_stopError != 0) {
            throw new Win32Exception(_stopError);
        }
    }

    private void RemoveHook() {
        IntPtr hook = Interlocked.Exchange(ref _hook, IntPtr.Zero);
        if (hook != IntPtr.Zero && !MonitorNativeMethods.UnhookWindowsHookEx(hook)) {
            _stopError = Marshal.GetLastWin32Error();
        }
    }
}
