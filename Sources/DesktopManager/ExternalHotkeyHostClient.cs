using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Registers hotkeys in an out-of-process helper and dispatches captured foreground-window handles.
/// </summary>
/// <remarks>
/// This backend isolates keyboard-hook ownership from the WinUI host. It is intended for remote desktop
/// clients and other foreground applications that can consume keyboard chords before standard hotkeys
/// behave reliably.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class ExternalHotkeyHostClient : IDisposable {
    private static readonly Lazy<ExternalHotkeyHostClient> _instance = new(() => new ExternalHotkeyHostClient());

    /// <summary>Gets the shared external hotkey host client.</summary>
    public static ExternalHotkeyHostClient Instance => _instance.Value;

    private readonly Dictionary<int, Action<IntPtr>> _callbacks = new();
    private readonly object _syncRoot = new();
    private readonly WorkerGate _commands = new();
    private int _nextId;
    private Process? _process;
    private StreamWriter? _input;
    private Thread? _readerThread;
    private bool _disposed;

    private ExternalHotkeyHostClient() {
    }

    /// <summary>
    /// Registers a hotkey in the helper process.
    /// </summary>
    /// <param name="modifiers">Required modifier keys.</param>
    /// <param name="key">Required trigger key.</param>
    /// <param name="options">Helper and capture options.</param>
    /// <param name="callback">Callback invoked with the foreground window captured by the helper hook.</param>
    /// <returns>Registration identifier.</returns>
    /// <exception cref="TimeoutException">Command admission or sending exceeded five seconds, or helper readiness exceeded the configured startup deadline.</exception>
    public int RegisterHotkey(HotkeyModifiers modifiers, VirtualKey key, ExternalHotkeyHostOptions? options, Action<IntPtr> callback) {
        if (callback == null) {
            throw new ArgumentNullException(nameof(callback));
        }

        options ??= new ExternalHotkeyHostOptions();
        _commands.Enter("Hotkey helper command");
        try {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(ExternalHotkeyHostClient));
            }

            EnsureStarted(options);

            int id = ++_nextId;
            lock (_syncRoot) { _callbacks[id] = callback; }
            try {
                Send(new ExternalHotkeyHostCommand {
                    Type = ExternalHotkeyHostCommandTypes.Register,
                    RegistrationId = id,
                    Modifiers = (int)(modifiers & ~HotkeyModifiers.NoRepeat),
                    Key = (int)key,
                    SuppressPotentialChordKeys = options.SuppressPotentialChordKeys,
                    ExclusiveForegroundProcessNames = new List<string>(options.ExclusiveForegroundProcessNames)
                });
            } catch {
                lock (_syncRoot) { _callbacks.Remove(id); }
                throw;
            }
            return id;
        } finally { _commands.Exit(); }
    }

    /// <summary>
    /// Unregisters a hotkey from the helper process.
    /// </summary>
    /// <param name="registrationId">Registration identifier returned by <see cref="RegisterHotkey"/>.</param>
    /// <exception cref="TimeoutException">Command admission or sending exceeded five seconds.</exception>
    public void UnregisterHotkey(int registrationId) {
        _commands.Enter("Hotkey helper command");
        try {
            lock (_syncRoot) { _callbacks.Remove(registrationId); }
            if (_process == null || _process.HasExited) {
                return;
            }

            Send(new ExternalHotkeyHostCommand {
                Type = ExternalHotkeyHostCommandTypes.Unregister,
                RegistrationId = registrationId
            });
        } finally { _commands.Exit(); }
    }

    /// <summary>Stops the helper after bounded command admission, sending and process-exit waits.</summary>
    /// <exception cref="TimeoutException">Command admission or sending exceeded five seconds. A send timeout stops the helper.</exception>
    public void Dispose() {
        _commands.Enter("Hotkey helper shutdown");
        try {
            if (_disposed) {
                return;
            }

            _disposed = true;
            lock (_syncRoot) { _callbacks.Clear(); }
            if (_process != null && !_process.HasExited) {
                TrySendShutdown();
            }

            if (_process != null && !_process.HasExited && !_process.WaitForExit(2000)) {
                TryStopHelper(_process);
                _input = null;
                _process = null;
            }

            _input?.Dispose();
            _process?.Dispose();
            _input = null;
            _process = null;
        } finally { _commands.Exit(); }
    }

    private void EnsureStarted(ExternalHotkeyHostOptions options) {
        if (_process != null && !_process.HasExited) {
            return;
        }

        string helperPath = ResolveHelperPath(options.HelperPath);
        var startInfo = new ProcessStartInfo(helperPath) {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(helperPath) ?? AppContext.BaseDirectory
        };

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Unable to start hotkey helper '{helperPath}'.");
        _input = _process.StandardInput;
        var process = _process;
        var ready = new ThreadStartup();
        _readerThread = new Thread(() => ReadEvents(process, ready)) {
            IsBackground = true,
            Name = "DesktopManager hotkey host reader"
        };
        try {
            ready.Start(_readerThread, "hotkey helper", Math.Max(1000, options.StartupTimeoutMilliseconds));
        } catch {
            TryStopHelper(process);
            _input = null;
            _process = null;
            throw;
        }
    }

    private static string ResolveHelperPath(string? configuredPath) {
        if (!string.IsNullOrWhiteSpace(configuredPath)) {
            string helperPath = configuredPath!;
            return File.Exists(helperPath)
                ? helperPath
                : throw new FileNotFoundException("Configured hotkey helper was not found.", helperPath);
        }

        string helperName = "DesktopManager.HotkeyHost.exe";
        string nestedPath = Path.Combine(AppContext.BaseDirectory, "DesktopManager.HotkeyHost", helperName);
        if (File.Exists(nestedPath)) {
            return nestedPath;
        }

        string flatPath = Path.Combine(AppContext.BaseDirectory, helperName);
        if (File.Exists(flatPath)) {
            return flatPath;
        }

        throw new FileNotFoundException("DesktopManager hotkey helper was not found.", nestedPath);
    }

    private void Send(ExternalHotkeyHostCommand command) {
        if (_input == null) {
            throw new InvalidOperationException("Hotkey helper input stream is not available.");
        }

        string line = JsonSerializer.Serialize(command, ExternalHotkeyHostJsonContext.Default.ExternalHotkeyHostCommand);
        try {
            WorkerInvocation<bool>.WaitFor(SendAsync(_input, line), "Hotkey helper command", 5000);
        } catch (TimeoutException) {
            if (_process != null) { TryStopHelper(_process); }
            _input = null;
            _process = null;
            throw;
        }
    }

    private static async Task SendAsync(StreamWriter input, string line) {
        await input.WriteLineAsync(line).ConfigureAwait(false);
        await input.FlushAsync().ConfigureAwait(false);
    }

    private void TrySendShutdown() {
        try {
            Send(new ExternalHotkeyHostCommand { Type = ExternalHotkeyHostCommandTypes.Shutdown });
        } catch (IOException) {
        } catch (ObjectDisposedException) {
        } catch (InvalidOperationException) {
        }
    }

    private void ReadEvents(Process process, ThreadStartup ready) {
        bool reported = false;
        try {
            StreamReader output = process.StandardOutput;
            string? line;
            while ((line = output.ReadLine()) != null) {
                ExternalHotkeyHostEvent? hotkeyEvent = JsonSerializer.Deserialize(
                    line, ExternalHotkeyHostJsonContext.Default.ExternalHotkeyHostEvent);
                if (hotkeyEvent == null) { continue; }
                if (string.Equals(hotkeyEvent.Type, ExternalHotkeyHostEventTypes.Ready, StringComparison.OrdinalIgnoreCase)) {
                    if (!reported) {
                        reported = true;
                        ready.Signal();
                    }
                } else { Dispatch(hotkeyEvent); }
            }
        } catch (IOException) {
        } catch (ObjectDisposedException) {
        } catch (InvalidOperationException) {
        } catch (JsonException) {
        } finally {
            if (!reported) { ready.Signal(new InvalidOperationException("The hotkey helper exited before reporting readiness.")); }
        }
    }

    private void Dispatch(ExternalHotkeyHostEvent hotkeyEvent) {
        if (!string.Equals(hotkeyEvent.Type, ExternalHotkeyHostEventTypes.Triggered, StringComparison.OrdinalIgnoreCase)) { return; }
        Action<IntPtr>? callback;
        lock (_syncRoot) { _callbacks.TryGetValue(hotkeyEvent.RegistrationId, out callback); }
        if (callback == null) { return; }
        IntPtr foregroundHandle = new(hotkeyEvent.ForegroundWindowHandle);
        ThreadPool.QueueUserWorkItem(_ => callback(foregroundHandle));
    }

    private static void TryStopHelper(Process process) {
        try {
            if (!process.HasExited) {
                process.Kill();
            }

            process.Dispose();
        } catch (InvalidOperationException) {
        } catch (Win32Exception) {
        }
    }
}
