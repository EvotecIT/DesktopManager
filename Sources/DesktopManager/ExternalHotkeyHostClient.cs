using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;

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
    private readonly ManualResetEventSlim _ready = new(false);
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
    public int RegisterHotkey(HotkeyModifiers modifiers, VirtualKey key, ExternalHotkeyHostOptions? options, Action<IntPtr> callback) {
        if (callback == null) {
            throw new ArgumentNullException(nameof(callback));
        }

        options ??= new ExternalHotkeyHostOptions();
        lock (_syncRoot) {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(ExternalHotkeyHostClient));
            }

            EnsureStarted(options);

            int id = ++_nextId;
            _callbacks[id] = callback;
            Send(new ExternalHotkeyHostCommand {
                Type = ExternalHotkeyHostCommandTypes.Register,
                RegistrationId = id,
                Modifiers = (int)(modifiers & ~HotkeyModifiers.NoRepeat),
                Key = (int)key,
                SuppressPotentialChordKeys = options.SuppressPotentialChordKeys,
                ExclusiveForegroundProcessNames = new List<string>(options.ExclusiveForegroundProcessNames)
            });
            return id;
        }
    }

    /// <summary>
    /// Unregisters a hotkey from the helper process.
    /// </summary>
    /// <param name="registrationId">Registration identifier returned by <see cref="RegisterHotkey"/>.</param>
    public void UnregisterHotkey(int registrationId) {
        lock (_syncRoot) {
            _callbacks.Remove(registrationId);
            if (_process == null || _process.HasExited) {
                return;
            }

            Send(new ExternalHotkeyHostCommand {
                Type = ExternalHotkeyHostCommandTypes.Unregister,
                RegistrationId = registrationId
            });
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        lock (_syncRoot) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _callbacks.Clear();
            if (_process != null && !_process.HasExited) {
                TrySendShutdown();
            }

            _input?.Dispose();
            _process?.Dispose();
            _input = null;
            _process = null;
            _ready.Dispose();
        }
    }

    private void EnsureStarted(ExternalHotkeyHostOptions options) {
        if (_process != null && !_process.HasExited) {
            return;
        }

        string helperPath = ResolveHelperPath(options.HelperPath);
        _ready.Reset();
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
        _readerThread = new Thread(ReadEvents) {
            IsBackground = true,
            Name = "DesktopManager hotkey host reader"
        };
        _readerThread.Start(_process);

        int timeout = Math.Max(1000, options.StartupTimeoutMilliseconds);
        if (!_ready.Wait(timeout)) {
            string error = TryReadError(_process);
            TryStopHelper(_process);
            _input = null;
            _process = null;
            throw new InvalidOperationException($"Hotkey helper '{helperPath}' did not become ready within {timeout} ms.{error}");
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
        _input.WriteLine(line);
        _input.Flush();
    }

    private void TrySendShutdown() {
        try {
            Send(new ExternalHotkeyHostCommand { Type = ExternalHotkeyHostCommandTypes.Shutdown });
        } catch (IOException) {
        } catch (ObjectDisposedException) {
        } catch (InvalidOperationException) {
        }
    }

    private void ReadEvents(object? state) {
        var process = (Process)state!;
        try {
            string? line;
            while ((line = process.StandardOutput.ReadLine()) != null) {
                ExternalHotkeyHostEvent? hotkeyEvent = JsonSerializer.Deserialize(
                    line,
                    ExternalHotkeyHostJsonContext.Default.ExternalHotkeyHostEvent);
                if (hotkeyEvent != null) {
                    Dispatch(hotkeyEvent);
                }
            }
        } catch (IOException) {
        } catch (ObjectDisposedException) {
        } catch (JsonException) {
        }
    }

    private void Dispatch(ExternalHotkeyHostEvent hotkeyEvent) {
        if (string.Equals(hotkeyEvent.Type, ExternalHotkeyHostEventTypes.Ready, StringComparison.OrdinalIgnoreCase)) {
            _ready.Set();
            return;
        }

        if (!string.Equals(hotkeyEvent.Type, ExternalHotkeyHostEventTypes.Triggered, StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        Action<IntPtr>? callback;
        lock (_syncRoot) {
            _callbacks.TryGetValue(hotkeyEvent.RegistrationId, out callback);
        }

        if (callback == null) {
            return;
        }

        IntPtr foregroundHandle = new(hotkeyEvent.ForegroundWindowHandle);
        ThreadPool.QueueUserWorkItem(_ => callback(foregroundHandle));
    }

    private static string TryReadError(Process process) {
        try {
            if (!process.HasExited) {
                return string.Empty;
            }

            string error = process.StandardError.ReadToEnd();
            return string.IsNullOrWhiteSpace(error) ? string.Empty : $" Error: {error.Trim()}";
        } catch (InvalidOperationException) {
            return string.Empty;
        } catch (Win32Exception) {
            return string.Empty;
        }
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
