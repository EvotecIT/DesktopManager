using System;
using System.Threading;

namespace DesktopManager;

/// <summary>Bounds admission when another caller may be starting or stopping a native worker.</summary>
internal sealed class WorkerGate {
    private readonly Semaphore _gate = new(1, 1);

    internal void Enter(string operation, int timeoutMilliseconds = 5000) {
        if (!_gate.WaitOne(timeoutMilliseconds)) {
            throw new TimeoutException($"{operation} admission did not complete within {timeoutMilliseconds}ms.");
        }
    }

    internal void Exit() => _gate.Release();
}
