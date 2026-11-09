using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace DesktopManager;

/// <summary>A one-shot, bounded thread-start handshake. The waiter and worker each release their event ownership.</summary>
internal sealed class ThreadStartup {
    private readonly ManualResetEvent _ready = new(false);
    private int _owners = 2;
    private Exception? _failure;

    internal void Start(Thread thread, string description) {
        try {
            thread.Start();
        } catch {
            Release(); // There is no worker to signal readiness.
            Release();
            throw;
        }

        try {
            // A kernel event avoids ManualResetEventSlim's Monitor.Wait path on STA callers.
            if (!_ready.WaitOne(TimeSpan.FromSeconds(5))) {
                throw new TimeoutException($"The {description} did not start within five seconds.");
            }
            if (_failure != null) {
                ExceptionDispatchInfo.Capture(_failure).Throw();
            }
        } finally {
            Release();
        }
    }

    /// <summary>Called once by the worker after initialization, including a failed or abandoned start.</summary>
    internal void Signal(Exception? failure = null) {
        _failure = failure;
        _ready.Set();
        Release();
    }

    private void Release() {
        if (Interlocked.Decrement(ref _owners) == 0) {
            _ready.Dispose();
        }
    }
}
