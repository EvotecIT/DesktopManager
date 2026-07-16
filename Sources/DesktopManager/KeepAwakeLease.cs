using System;
using System.ComponentModel;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Holds a Windows execution-state request on a dedicated thread until disposed.
/// </summary>
public sealed class KeepAwakeLease : IDisposable {
    private readonly ManualResetEvent _started = new(false);
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
        _thread.Start();
        _started.WaitOne();
        if (_startFailure != null) {
            _stop.Dispose();
            _started.Dispose();
            throw new InvalidOperationException("Windows rejected the keep-awake request.", _startFailure);
        }
    }

    /// <summary>Gets the behaviors held by this lease.</summary>
    public KeepAwakeOptions Options { get; }

    /// <inheritdoc/>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _stop.Set();
        _thread.Join();
        _stop.Dispose();
        _started.Dispose();
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
            _startFailure = ex;
        } finally {
            _started.Set();
        }

        if (_startFailure != null) {
            return;
        }

        _stop.WaitOne();
        SystemPowerService.SetThreadExecutionState(SystemPowerService.ExecutionStateContinuous);
    }
}
