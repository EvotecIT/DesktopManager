using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace DesktopManager;

/// <summary>A one-shot, bounded thread-start handshake. The waiter and worker each release their event ownership.</summary>
internal sealed class ThreadStartup {
    private readonly WorkerCompletion _ready = new();
    private Exception? _failure;

    internal void Start(Thread thread, string description, int timeoutMilliseconds = 5000) {
        try {
            thread.Start();
        } catch {
            _ready.Complete(); // There is no worker to signal readiness.
            _ready.Dispose();
            throw;
        }

        try {
            // A kernel event avoids ManualResetEventSlim's Monitor.Wait path on STA callers.
            if (!_ready.Wait(timeoutMilliseconds)) {
                throw new TimeoutException($"The {description} did not start within {timeoutMilliseconds}ms.");
            }
            if (_failure != null) {
                ExceptionDispatchInfo.Capture(_failure).Throw();
            }
        } finally {
            _ready.Dispose();
        }
    }

    /// <summary>Called once by the worker after initialization, including a failed or abandoned start.</summary>
    internal void Signal(Exception? failure = null) {
        _failure = failure;
        _ready.Complete();
    }
}
