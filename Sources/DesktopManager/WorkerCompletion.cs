using System;
using System.Threading;

namespace DesktopManager;

/// <summary>One kernel event shared by a waiting caller and its worker, including late completion after timeout.</summary>
internal sealed class WorkerCompletion : IDisposable {
    private readonly ManualResetEvent _event = new(false);
    private int _owners = 2;

    internal WaitHandle Signal => _event;

    internal bool Wait(int timeoutMilliseconds) {
        if (timeoutMilliseconds <= 0) { throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds)); }
        return _event.WaitOne(timeoutMilliseconds);
    }

    internal void Complete() {
        _event.Set();
        Release();
    }

    public void Dispose() => Release();

    private void Release() {
        if (Interlocked.Decrement(ref _owners) == 0) { _event.Dispose(); }
    }
}
