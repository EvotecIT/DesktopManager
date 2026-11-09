using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>A bounded synchronous worker request that cancels queued work and safely retires late results.</summary>
internal sealed class WorkerInvocation<T> : IDisposable {
    internal const int DefaultTimeoutMilliseconds = 15000;
    private const int Queued = 0, Executing = 1, Completed = 2, Abandoned = 3, AbandonedInFlight = 4;
    private readonly WorkerCompletion _completion = new();
    private readonly Action<T>? _abandonedResultHandler;
    private Action? _operationCleanup;
    private ExceptionDispatchInfo? _exception;
    private T _result = default!;
    private int _state;

    internal WorkerInvocation(Action<T>? abandonedResultHandler = null, Action? operationCleanup = null) {
        _abandonedResultHandler = abandonedResultHandler;
        _operationCleanup = operationCleanup;
    }

    internal bool IsAbandonedInFlight => Volatile.Read(ref _state) == AbandonedInFlight;
    internal WaitHandle CompletionSignal => _completion.Signal;

    internal void Execute(Func<T> operation) {
        if (Interlocked.CompareExchange(ref _state, Executing, Queued) != Queued) {
            _completion.Complete();
            return;
        }
        try {
            _result = operation();
        } catch (Exception exception) {
            _exception = ExceptionDispatchInfo.Capture(exception);
        } finally {
            Finish();
        }
    }

    /// <summary>Bridges an already-started asynchronous operation without blocking on a Task's managed condition.</summary>
    internal static T WaitFor(Task<T> task, string operation, int timeoutMilliseconds = DefaultTimeoutMilliseconds) {
        using var invocation = new WorkerInvocation<T>();
        invocation.Observe(task, () => task.GetAwaiter().GetResult());
        return invocation.GetResult(operation, timeoutMilliseconds);
    }

    internal static void WaitFor(Task task, string operation, int timeoutMilliseconds = DefaultTimeoutMilliseconds) {
        using var invocation = new WorkerInvocation<T>();
        invocation.Observe(task, () => default!);
        invocation.GetResult(operation, timeoutMilliseconds);
    }

    private void Observe(Task task, Func<T> result) {
        Volatile.Write(ref _state, Executing);
        _ = task.ContinueWith(completed => {
            try {
                // This callback only runs for a completed Task. GetResult unwraps its error; it never waits here.
                completed.GetAwaiter().GetResult();
                _result = result();
            } catch (Exception exception) { _exception = ExceptionDispatchInfo.Capture(exception); }
            finally { Finish(); }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void Finish() {
        CleanupOperation();
        int previous = Interlocked.CompareExchange(ref _state, Completed, Executing);
        if (previous == AbandonedInFlight && _exception == null && _abandonedResultHandler != null) {
            try { _abandonedResultHandler(_result); }
            catch (Exception exception) { DesktopManagerDiagnostics.Report($"Late worker result cleanup failed: {exception.Message}"); }
        }
        Volatile.Write(ref _state, Completed);
        _completion.Complete();
    }

    // Only used when admission failed and no worker can execute this request.
    internal void Reject() {
        CleanupOperation();
        _completion.Complete();
    }

    internal void CancelPending() {
        if (Interlocked.CompareExchange(ref _state, Abandoned, Queued) == Queued) { CleanupOperation(); }
    }

    private void CleanupOperation() {
        var cleanup = Interlocked.Exchange(ref _operationCleanup, null);
        if (cleanup == null) { return; }
        try { cleanup(); }
        catch (Exception exception) { DesktopManagerDiagnostics.Report($"Worker operation cleanup failed: {exception.Message}"); }
    }

    internal T GetResult(string operation, int timeoutMilliseconds = DefaultTimeoutMilliseconds) {
        return GetResult(operation, timeoutMilliseconds, _completion.Wait(timeoutMilliseconds));
    }

    internal T GetResult(string operation, int timeoutMilliseconds, bool completionObserved) {
        if (!completionObserved) {
            if (Interlocked.CompareExchange(ref _state, Abandoned, Queued) == Queued) {
                CleanupOperation();
                throw new TimeoutException($"{operation} did not complete within {timeoutMilliseconds}ms and was canceled before it started.");
            }
            if (Interlocked.CompareExchange(ref _state, AbandonedInFlight, Executing) == Executing) {
                throw new NativeOperationOutcomeUnknownException(operation, timeoutMilliseconds);
            }
            // A completion can win the race with timeout. Its result was published before Completed;
            // never follow that race with another (potentially infinite) managed wait.
        }
        _exception?.Throw();
        return _result;
    }

    public void Dispose() {
        CancelPending();
        Interlocked.CompareExchange(ref _state, AbandonedInFlight, Executing);
        _completion.Dispose();
    }
}
