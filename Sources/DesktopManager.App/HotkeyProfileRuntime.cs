using DesktopManager.App.Core;
using DesktopHotkeyModifiers = global::DesktopManager.HotkeyModifiers;
using DesktopVirtualKey = global::DesktopManager.VirtualKey;

namespace DesktopManager.App;

internal sealed class HotkeyProfileRuntime : IDisposable {
    private readonly WindowHotkeyActionExecutor _executor = new();
    private readonly List<HotkeyRegistrationHandle> _registrations = new();
    private readonly SemaphoreSlim _executionGate = new(1, 1);
    private readonly ManualResetEventSlim _executionDrained = new(true);
    private readonly object _lifetimeSync = new();
    private string _hotkeyBackend = HotkeyBackendKinds.LowLevelKeyboardHook;
    private global::DesktopManager.LowLevelKeyboardHotkeyOptions _lowLevelHookOptions = new();
    private int _queuedExecutions;
    private bool _disposed;

    public event EventHandler<string>? StatusChanged;

    public int RegisteredCount => _registrations.Count;

    public void Start(HotkeyProfile profile) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stop();

        if (!profile.Enabled) {
            OnStatusChanged("Profile is disabled.");
            return;
        }

        _hotkeyBackend = string.IsNullOrWhiteSpace(profile.HotkeyBackend)
            ? HotkeyBackendKinds.LowLevelKeyboardHook
            : profile.HotkeyBackend;
        _lowLevelHookOptions = new global::DesktopManager.LowLevelKeyboardHotkeyOptions {
            SuppressPotentialChordKeys = false,
            ExclusiveForegroundProcessNames = profile.LowLevelHookExclusiveProcessNames.ToArray()
        };

        foreach (HotkeyFunctionDefinition function in profile.Functions.Where(function => function.Enabled)) {
            if (!HotkeyGestureParser.TryParse(function.Hotkey, out DesktopHotkeyModifiers modifiers, out DesktopVirtualKey key, out string error)) {
                OnStatusChanged($"{function.Name}: {error}");
                continue;
            }

            HotkeyFunctionDefinition captured = function;
            try {
                HotkeyRegistrationHandle registration = RegisterHotkey(modifiers, key, captured);
                _registrations.Add(registration);
                HotkeyDiagnosticsWriter.WriteRuntimeEvent(
                    "registered",
                    captured,
                    details: new {
                        registration.RegistrationId,
                        registration.Backend,
                        Modifiers = modifiers.ToString(),
                        Key = key.ToString()
                    });
            } catch (Exception ex) {
                HotkeyDiagnosticsWriter.WriteRuntimeEvent("registration-failed", captured, ex.Message);
                OnStatusChanged($"{function.Name}: {ex.Message}");
            }
        }

        OnStatusChanged($"Registered {_registrations.Count} hotkey(s).");
    }

    public void Stop() {
        foreach (HotkeyRegistrationHandle registration in _registrations) {
            registration.Unregister();
        }

        _registrations.Clear();
    }

    public void Dispose() {
        lock (_lifetimeSync) {
            _disposed = true;
        }

        Stop();
        _executionDrained.Wait();
        _executionGate.Dispose();
        _executionDrained.Dispose();
    }

    public void Execute(HotkeyFunctionDefinition function) {
        QueueExecution(function, IntPtr.Zero, "manual");
    }

    public void Execute(HotkeyFunctionDefinition function, IntPtr targetWindowHandle) {
        QueueExecution(function, targetWindowHandle, "manual");
    }

    private HotkeyRegistrationHandle RegisterHotkey(DesktopHotkeyModifiers modifiers, DesktopVirtualKey key, HotkeyFunctionDefinition function) {
        if (string.Equals(_hotkeyBackend, HotkeyBackendKinds.NativeHotkeyHost, StringComparison.OrdinalIgnoreCase)) {
            var options = new global::DesktopManager.ExternalHotkeyHostOptions {
                SuppressPotentialChordKeys = _lowLevelHookOptions.SuppressPotentialChordKeys,
                ExclusiveForegroundProcessNames = _lowLevelHookOptions.ExclusiveForegroundProcessNames
            };
            int id = global::DesktopManager.ExternalHotkeyHostClient.Instance.RegisterHotkey(
                modifiers,
                key,
                options,
                capturedWindowHandle => QueueExecution(function, capturedWindowHandle, "native-hotkey-host"));

            return new HotkeyRegistrationHandle(
                id,
                "NativeHotkeyHost",
                () => global::DesktopManager.ExternalHotkeyHostClient.Instance.UnregisterHotkey(id));
        }

        if (!string.Equals(_hotkeyBackend, HotkeyBackendKinds.LowLevelKeyboardHook, StringComparison.OrdinalIgnoreCase)) {
            int id = global::DesktopManager.HotkeyService.Instance.RegisterHotkey(
                modifiers,
                key,
                () => QueueExecution(function, IntPtr.Zero, "register-hotkey"));

            return new HotkeyRegistrationHandle(
                id,
                "RegisterHotKey",
                () => global::DesktopManager.HotkeyService.Instance.UnregisterHotkey(id));
        }

        try {
            int id = global::DesktopManager.LowLevelKeyboardHotkeyService.Instance.RegisterHotkey(
                modifiers,
                key,
                _lowLevelHookOptions,
                capturedWindowHandle => QueueExecution(function, capturedWindowHandle, "keyboard-hook"));

            return new HotkeyRegistrationHandle(
                id,
                "LowLevelKeyboardHook",
                () => global::DesktopManager.LowLevelKeyboardHotkeyService.Instance.UnregisterHotkey(id));
        } catch (Exception ex) {
            HotkeyDiagnosticsWriter.WriteRuntimeEvent("low-level-registration-failed", function, ex.Message);
            int id = global::DesktopManager.HotkeyService.Instance.RegisterHotkey(
                modifiers,
                key,
                () => QueueExecution(function, IntPtr.Zero, "register-hotkey"));

            return new HotkeyRegistrationHandle(
                id,
                "RegisterHotKey",
                () => global::DesktopManager.HotkeyService.Instance.UnregisterHotkey(id));
        }
    }

    private void QueueExecution(HotkeyFunctionDefinition function, IntPtr targetWindowHandle, string source) {
        lock (_lifetimeSync) {
            if (_disposed) {
                HotkeyDiagnosticsWriter.WriteRuntimeEvent("dropped", function, "Runtime is disposed.");
                return;
            }

            _queuedExecutions++;
            _executionDrained.Reset();
        }

        IntPtr rawWindowHandle = targetWindowHandle;
        IntPtr capturedWindowHandle = targetWindowHandle;
        if (targetWindowHandle == IntPtr.Zero) {
            rawWindowHandle = global::DesktopManager.MonitorNativeMethods.GetForegroundWindow();
            capturedWindowHandle = global::DesktopManager.WindowManager.GetRootWindowHandle(rawWindowHandle);
        }

        HotkeyDiagnosticsWriter.WriteRuntimeEvent(
            "queued",
            function,
            details: new {
                Source = source,
                RawHandle = FormatHandle(rawWindowHandle),
                CapturedHandle = FormatHandle(capturedWindowHandle)
            });

        ThreadPool.QueueUserWorkItem(_ => {
            try {
                _executionGate.Wait();
                try {
                    HotkeyDiagnosticsWriter.WriteRuntimeEvent(
                        "started",
                        function,
                        details: new {
                            Source = source,
                            CapturedHandle = FormatHandle(capturedWindowHandle)
                        });
                    HotkeyExecutionResult result = _executor.Execute(function, capturedWindowHandle);
                    HotkeyDiagnosticsWriter.WriteRuntimeEvent(
                        "completed",
                        function,
                        details: new {
                            Source = source,
                            WindowHandle = FormatHandle(result.WindowHandle),
                            result.Verified,
                            result.Attempts,
                            result.DiagnosticPath
                        });
                    OnStatusChanged(result.ToStatusMessage());
                } finally {
                    _executionGate.Release();
                }
            } catch (Exception ex) {
                HotkeyDiagnosticsWriter.WriteRuntimeEvent(
                    "failed",
                    function,
                    ex.Message,
                    new {
                        Source = source,
                        CapturedHandle = FormatHandle(capturedWindowHandle)
                    });
                OnStatusChanged($"{function.Name}: {ex.Message}");
            } finally {
                lock (_lifetimeSync) {
                    _queuedExecutions--;
                    if (_queuedExecutions == 0) {
                        _executionDrained.Set();
                    }
                }
            }
        });
    }

    private static string FormatHandle(IntPtr handle) {
        return handle == IntPtr.Zero ? "0x0" : $"0x{handle.ToInt64():X}";
    }

    private void OnStatusChanged(string message) {
        StatusChanged?.Invoke(this, message);
    }

    private sealed class HotkeyRegistrationHandle {
        public HotkeyRegistrationHandle(int registrationId, string backend, Action unregister) {
            RegistrationId = registrationId;
            Backend = backend;
            _unregister = unregister;
        }

        private readonly Action _unregister;

        public int RegistrationId { get; }
        public string Backend { get; }

        public void Unregister() {
            _unregister();
        }
    }
}
