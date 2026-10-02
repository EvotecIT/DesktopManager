using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

internal sealed class UiAutomationOperationInFlightException : NativeOperationOutcomeUnknownException {
    internal UiAutomationOperationInFlightException(int timeoutMilliseconds)
        : base("UI Automation", timeoutMilliseconds) {
    }
}

/// <summary>Serializes one process's provider work on a bounded STA queue.</summary>
internal sealed class UiAutomationStaDispatcher : IDisposable {
    internal const int DefaultInvocationTimeoutMilliseconds = 15000;
    internal const int QueueCapacity = 64;
    [ThreadStatic] internal static UiAutomationStaDispatcher? Current;
    private readonly object _sync = new();
    private readonly BlockingCollection<IWorkItem> _queue = new(QueueCapacity);
    private readonly Thread _thread;
    private IWorkItem? _active;
    private int _disposed;
    private int _subscriptions;

    internal UiAutomationStaDispatcher() {
        _thread = new Thread(Run) { IsBackground = true, Name = "DesktopManager UI Automation" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    internal bool IsCurrentThread => Current == this;
    internal bool IsBlocked => Volatile.Read(ref _active)?.IsAbandonedInFlight == true;
    internal bool IsIdle => Volatile.Read(ref _active) == null && _queue.Count == 0;
    internal int PendingCount => _queue.Count;
    internal int SubscriptionCount => Volatile.Read(ref _subscriptions);
    internal void RetainSubscription() => Interlocked.Increment(ref _subscriptions);
    internal void ReleaseSubscription() => Interlocked.Decrement(ref _subscriptions);

    internal T Invoke<T>(Func<UiAutomationControlService, T> operation) {
        return Invoke(operation, DefaultInvocationTimeoutMilliseconds);
    }

    internal T Invoke<T>(Func<UiAutomationControlService, T> operation, int timeoutMilliseconds,
        Action<T>? abandonedResultHandler = null) {
        if (operation == null) { throw new ArgumentNullException(nameof(operation)); }
        if (timeoutMilliseconds <= 0) { throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds)); }
        var item = new WorkItem<T>(operation, abandonedResultHandler);
        Add(item);
        return item.GetResult(timeoutMilliseconds);
    }

    internal void Post(Action<UiAutomationControlService> operation) {
        if (operation == null) { throw new ArgumentNullException(nameof(operation)); }
        Add(new FireAndForgetWorkItem(operation));
    }

    private void Add(IWorkItem item) {
        lock (_sync) {
            if (_disposed != 0) { throw new ObjectDisposedException(nameof(UiAutomationStaDispatcher)); }
            if (!_queue.TryAdd(item)) {
                throw new TimeoutException("The UI Automation provider queue is full; the operation was not started.");
            }
        }
    }

    public void Dispose() {
        lock (_sync) {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) { return; }
            _queue.CompleteAdding();
        }
        // Native providers cannot be interrupted safely. Shutdown must remain bounded.
        if (!IsCurrentThread && !_thread.Join(250)) {
            DesktopManagerDiagnostics.Report("UI Automation provider shutdown is pending on an in-flight native call.");
        }
    }

    private void Run() {
        Current = this;
        try {
            var service = new UiAutomationControlService();
            foreach (IWorkItem item in _queue.GetConsumingEnumerable()) {
                Volatile.Write(ref _active, item);
                try {
                    if (Volatile.Read(ref _disposed) != 0) { item.Cancel(); }
                    else { item.Execute(service); }
                } finally {
                    Volatile.Write(ref _active, null);
                }
            }
        } finally {
            Current = null;
            // Only the worker owns disposal; a timed-out shutdown may still be consuming.
            _queue.Dispose();
        }
    }

    private interface IWorkItem {
        bool IsAbandonedInFlight { get; }
        void Execute(UiAutomationControlService service);
        void Cancel();
    }

    private sealed class FireAndForgetWorkItem : IWorkItem {
        private readonly Action<UiAutomationControlService> _operation;
        internal FireAndForgetWorkItem(Action<UiAutomationControlService> operation) { _operation = operation; }
        public bool IsAbandonedInFlight => false;
        public void Cancel() { }
        public void Execute(UiAutomationControlService service) {
            try { _operation(service); } catch { /* Cleanup must not terminate the worker. */ }
        }
    }

    private sealed class WorkItem<T> : IWorkItem {
        private const int Queued = 0, Executing = 1, Completed = 2, Abandoned = 3, AbandonedInFlight = 4;
        private readonly Func<UiAutomationControlService, T> _operation;
        private readonly Action<T>? _abandonedResultHandler;
        private readonly TaskCompletionSource<bool> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private ExceptionDispatchInfo? _exception;
        private T _result = default!;
        private int _state;
        internal WorkItem(Func<UiAutomationControlService, T> operation, Action<T>? abandonedResultHandler) {
            _operation = operation;
            _abandonedResultHandler = abandonedResultHandler;
        }
        public bool IsAbandonedInFlight => Volatile.Read(ref _state) == AbandonedInFlight;
        public void Cancel() {
            if (Interlocked.CompareExchange(ref _state, Completed, Queued) == Queued) {
                _exception = ExceptionDispatchInfo.Capture(new ObjectDisposedException(nameof(UiAutomationStaDispatcher)));
                _completed.TrySetResult(true);
            }
        }
        public void Execute(UiAutomationControlService service) {
            if (Interlocked.CompareExchange(ref _state, Executing, Queued) != Queued) { return; }
            try {
                _result = _operation(service);
            } catch (Exception ex) {
                _exception = ExceptionDispatchInfo.Capture(ex);
            } finally {
                int priorState = Interlocked.CompareExchange(ref _state, Completed, Executing);
                if (priorState == AbandonedInFlight && _exception == null && _abandonedResultHandler != null) {
                    try { _abandonedResultHandler(_result); } catch { /* Late-result cleanup is best effort. */ }
                }
                Volatile.Write(ref _state, Completed);
                _completed.TrySetResult(true);
            }
        }
        internal T GetResult(int timeoutMilliseconds) {
            if (!_completed.Task.Wait(timeoutMilliseconds)) {
                if (Interlocked.CompareExchange(ref _state, Abandoned, Queued) == Queued) {
                    throw new TimeoutException($"UI Automation did not complete within {timeoutMilliseconds}ms and was canceled before it started.");
                }
                if (Interlocked.CompareExchange(ref _state, AbandonedInFlight, Executing) == Executing) {
                    throw new UiAutomationOperationInFlightException(timeoutMilliseconds);
                }
                _completed.Task.GetAwaiter().GetResult();
            }
            _exception?.Throw();
            return _result;
        }
    }
}
