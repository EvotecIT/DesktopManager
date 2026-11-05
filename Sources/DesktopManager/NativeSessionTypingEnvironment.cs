using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>Reads the live desktop for <see cref="SessionTypingRunner"/>.</summary>
[SupportedOSPlatform("windows")]
internal sealed class NativeSessionTypingEnvironment : ISessionTypingEnvironment {
    internal static readonly NativeSessionTypingEnvironment Instance = new();

    private static readonly (int VirtualKey, string Name)[] Modifiers = {
        (0xA0, "Left Shift"), (0xA1, "Right Shift"),
        (0xA2, "Left Ctrl"), (0xA3, "Right Ctrl"),
        (0xA4, "Left Alt"), (0xA5, "Right Alt"),
        (0x5B, "Left Windows"), (0x5C, "Right Windows")
    };

    /// <inheritdoc />
    public IntPtr GetRootWindow(IntPtr window) {
        return MonitorNativeMethods.GetAncestor(window, MonitorNativeMethods.GA_ROOT);
    }

    /// <inheritdoc />
    public IntPtr GetForegroundWindow() {
        return MonitorNativeMethods.GetForegroundWindow();
    }

    /// <inheritdoc />
    public IntPtr GetFocusedWindow() {
        var info = new MonitorNativeMethods.GUITHREADINFO {
            cbSize = Marshal.SizeOf<MonitorNativeMethods.GUITHREADINFO>()
        };

        // Thread zero means the foreground thread, which owns the keyboard focus even when it differs from the window's thread.
        return MonitorNativeMethods.GetGUIThreadInfo(0, ref info) ? info.hwndFocus : IntPtr.Zero;
    }

    /// <inheritdoc />
    public void TryActivate(IntPtr window) {
        WindowActivationService.TryActivateWindow(window, 3, 100);
    }

    /// <inheritdoc />
    public bool IsElevatedBeyondCaller(IntPtr window) {
        MonitorNativeMethods.GetWindowThreadProcessId(window, out uint processId);
        if (processId == 0 || IsElevated(SessionTypingNative.GetCurrentProcess()) == true) {
            return false;
        }

        IntPtr process = MonitorNativeMethods.OpenProcess(MonitorNativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (process == IntPtr.Zero) {
            return false;
        }

        try {
            return IsElevated(process) == true;
        } finally {
            MonitorNativeMethods.CloseHandle(process);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetHeldModifiers() {
        var held = new List<string>();
        foreach ((int virtualKey, string name) in Modifiers) {
            if ((MonitorNativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0) {
                held.Add(name);
            }
        }

        return held;
    }

    /// <inheritdoc />
    public bool IsCapsLockOn() {
        return (SessionTypingNative.GetKeyState(SessionTypingNative.VK_CAPITAL) & 1) != 0;
    }

    /// <inheritdoc />
    public bool Delay(int milliseconds, CancellationToken cancellationToken) {
        if (milliseconds <= 0) {
            return !cancellationToken.IsCancellationRequested;
        }

        return !cancellationToken.WaitHandle.WaitOne(milliseconds);
    }

    /// <inheritdoc />
    public IDisposable StartUserInputMonitor(Action onUserInput) {
        return new PhysicalInputMonitor(onUserInput);
    }

    private static bool? IsElevated(IntPtr process) {
        if (!MonitorNativeMethods.OpenProcessToken(process, MonitorNativeMethods.TOKEN_QUERY, out IntPtr token)) {
            return null;
        }

        try {
            int size = Marshal.SizeOf<MonitorNativeMethods.TOKEN_ELEVATION>();
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try {
                if (!MonitorNativeMethods.GetTokenInformation(token, MonitorNativeMethods.TOKEN_INFORMATION_CLASS.TokenElevation, buffer, size, out _)) {
                    return null;
                }

                return Marshal.PtrToStructure<MonitorNativeMethods.TOKEN_ELEVATION>(buffer).TokenIsElevated != 0;
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        } finally {
            MonitorNativeMethods.CloseHandle(token);
        }
    }

    /// <summary>
    /// Watches for physical key presses, mouse button presses and wheel turns through low-level hooks on a dedicated thread.
    /// Injected events, releases and mouse movement are ignored, so the key release that started the command and the click
    /// that focused the session do not count.
    /// </summary>
    private sealed class PhysicalInputMonitor : IDisposable {
        private readonly Action _onUserInput;
        private readonly ManualResetEventSlim _ready = new(false);
        private readonly Thread _thread;
        private MonitorNativeMethods.LowLevelKeyboardProc? _keyboardProc;
        private MonitorNativeMethods.LowLevelKeyboardProc? _mouseProc;
        private IntPtr _keyboardHook;
        private IntPtr _mouseHook;
        private uint _threadId;
        private Exception? _startError;
        private bool _disposed;

        internal PhysicalInputMonitor(Action onUserInput) {
            _onUserInput = onUserInput;
            _thread = new Thread(Run) { IsBackground = true, Name = "DesktopManager session typing input monitor" };
            _thread.Start();
            if (!_ready.Wait(TimeSpan.FromSeconds(5))) {
                throw new TimeoutException("The user input monitor did not start.");
            }
            if (_startError != null) {
                _thread.Join();
                ExceptionDispatchInfo.Capture(_startError).Throw();
            }
        }

        private void Run() {
            try {
                _threadId = MonitorNativeMethods.GetCurrentThreadId();
                IntPtr module = MonitorNativeMethods.GetModuleHandle(null);
                _keyboardProc = KeyboardCallback;
                _mouseProc = MouseCallback;
                _keyboardHook = MonitorNativeMethods.SetWindowsHookEx(MonitorNativeMethods.WH_KEYBOARD_LL, _keyboardProc, module, 0);
                _mouseHook = _keyboardHook == IntPtr.Zero ? IntPtr.Zero : MonitorNativeMethods.SetWindowsHookEx(SessionTypingNative.WH_MOUSE_LL, _mouseProc, module, 0);
                if (_keyboardHook == IntPtr.Zero || _mouseHook == IntPtr.Zero) {
                    _startError = new InvalidOperationException(
                        "The user input monitor could not install its hooks, so typing could not stop when you touch the keyboard or mouse. Turn off stopping on user input to type anyway.",
                        new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
                    return;
                }
            } finally {
                _ready.Set();
            }

            try {
                while (MonitorNativeMethods.GetMessage(out MonitorNativeMethods.MSG message, IntPtr.Zero, 0, 0) > 0) {
                    MonitorNativeMethods.TranslateMessage(ref message);
                    MonitorNativeMethods.DispatchMessage(ref message);
                }
            } finally {
                Unhook();
            }
        }

        private IntPtr KeyboardCallback(int code, IntPtr message, IntPtr data) {
            if (code >= 0) {
                int kind = message.ToInt32();
                if (kind == MonitorNativeMethods.WM_KEYDOWN_HOOK || kind == MonitorNativeMethods.WM_SYSKEYDOWN_HOOK) {
                    var keyboard = Marshal.PtrToStructure<MonitorNativeMethods.KBDLLHOOKSTRUCT>(data);
                    if ((keyboard.flags & SessionTypingNative.LLKHF_INJECTED) == 0) {
                        _onUserInput();
                    }
                }
            }

            return MonitorNativeMethods.CallNextHookEx(_keyboardHook, code, message, data);
        }

        private IntPtr MouseCallback(int code, IntPtr message, IntPtr data) {
            if (code >= 0 && IsPress(message.ToInt32())) {
                var mouse = Marshal.PtrToStructure<SessionTypingNative.MSLLHOOKSTRUCT>(data);
                if ((mouse.Flags & SessionTypingNative.LLMHF_INJECTED) == 0) {
                    _onUserInput();
                }
            }

            return MonitorNativeMethods.CallNextHookEx(_mouseHook, code, message, data);
        }

        private static bool IsPress(int message) {
            return message == SessionTypingNative.WM_LBUTTONDOWN ||
                message == SessionTypingNative.WM_RBUTTONDOWN ||
                message == SessionTypingNative.WM_MBUTTONDOWN ||
                message == SessionTypingNative.WM_XBUTTONDOWN ||
                message == SessionTypingNative.WM_MOUSEWHEEL ||
                message == SessionTypingNative.WM_MOUSEHWHEEL;
        }

        private void Unhook() {
            if (_keyboardHook != IntPtr.Zero) {
                MonitorNativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
            if (_mouseHook != IntPtr.Zero) {
                MonitorNativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }

        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposed = true;
            MonitorNativeMethods.PostThreadMessage(_threadId, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(TimeSpan.FromSeconds(2));
            _ready.Dispose();
        }
    }
}
