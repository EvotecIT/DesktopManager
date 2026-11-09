using System;
using System.ComponentModel;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Holds a Windows execution-state request on a dedicated thread until disposed.
/// </summary>
public sealed class KeepAwakeLease : IDisposable {
    private readonly ThreadStartup _started = new();
    private readonly ManualResetEvent _stop = new(false);
    private readonly Thread _thread;
    private Exception? _startFailure;
    private bool _disposed;

    internal KeepAwakeLease(KeepAwakeOptions options) {
        if (options == 0) {
            throw new ArgumentOutOfRangeException(nameof(options), "At least one keep-awake option is required.");
        }

        Options = options;
        _thread = new Thread(Run) {
            IsBackground = true,
            Name = "DesktopManager Keep Awake"
        };
        try {
            _started.Start(_thread, "keep-awake thread");
        } catch {
            _stop.Set();
            if (!_thread.IsAlive || _thread.Join(TimeSpan.FromSeconds(2))) {
                _stop.Dispose();
            }
            throw;
        }
    }

    /// <summary>Gets the behaviors held by this lease.</summary>
    public KeepAwakeOptions Options { get; }

    /// <summary>Releases the execution-state request and waits up to two seconds for the worker to stop.</summary>
    /// <exception cref="TimeoutException">The worker did not stop within two seconds.</exception>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _stop.Set();
        if (!_thread.Join(TimeSpan.FromSeconds(2))) {
            throw new TimeoutException("The keep-awake thread did not stop within two seconds.");
        }
        _stop.Dispose();
        _disposed = true;
    }

    private void Run() {
        try {
            uint request = SystemPowerService.ExecutionStateContinuous |
                SystemPowerService.ToExecutionState(Options);
            if (SystemPowerService.SetThreadExecutionState(request) == 0) {
                throw new Win32Exception();
            }
        } catch (Exception ex) {
            _startFailure = new InvalidOperationException("Windows rejected the keep-awake request.", ex);
        } finally {
            _started.Signal(_startFailure);
        }

        if (_startFailure != null) {
            return;
        }

        _stop.WaitOne();
        SystemPowerService.SetThreadExecutionState(SystemPowerService.ExecutionStateContinuous);
    }
}
