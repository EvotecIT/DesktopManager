using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

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
    internal const int SubscriptionCapacity = 64;
    [ThreadStatic] internal static UiAutomationStaDispatcher? Current;
    private readonly object _sync = new();
    private readonly BlockingCollection<IWorkItem> _queue = new(QueueCapacity);
    private readonly Queue<IWorkItem> _cleanup = new();
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
    internal bool IsIdle => Volatile.Read(ref _active) == null && PendingCount == 0;
    internal int PendingCount { get { lock (_sync) { return _queue.Count + _cleanup.Count; } } }
    internal int SubscriptionCount => Volatile.Read(ref _subscriptions);
    internal bool TryRetainSubscription() {
        while (true) {
            int count = Volatile.Read(ref _subscriptions);
            if (count >= SubscriptionCapacity) { return false; }
            if (Interlocked.CompareExchange(ref _subscriptions, count + 1, count) == count) { return true; }
        }
    }
    internal void ReleaseSubscription() => Interlocked.Decrement(ref _subscriptions);

    internal T Invoke<T>(Func<UiAutomationControlService, T> operation) {
        return Invoke(operation, DefaultInvocationTimeoutMilliseconds);
    }

    internal T Invoke<T>(Func<UiAutomationControlService, T> operation, int timeoutMilliseconds,
        Action<T>? abandonedResultHandler = null) {
        if (operation == null) { throw new ArgumentNullException(nameof(operation)); }
        if (timeoutMilliseconds <= 0) { throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds)); }
        using var item = new WorkItem<T>(operation, abandonedResultHandler);
        try { Add(item); }
        catch { item.Reject(); throw; }
        return item.GetResult(timeoutMilliseconds);
    }

    internal void Post(Action<UiAutomationControlService> operation) {
        if (operation == null) { throw new ArgumentNullException(nameof(operation)); }
        Add(new FireAndForgetWorkItem(operation));
    }

    // One cleanup per retained subscription. Reserving before registration bounds this queue
    // independently of ordinary operations and guarantees cleanup admission under saturation.
    internal void PostSubscriptionCleanup(Action<UiAutomationControlService> operation) {
        lock (_sync) {
            if (_disposed != 0) { throw new ObjectDisposedException(nameof(UiAutomationStaDispatcher)); }
            _cleanup.Enqueue(new FireAndForgetWorkItem(operation));
            // A full queue already wakes the worker. Otherwise enqueue a wake marker.
            if (_queue.Count == 0) { _queue.TryAdd(new FireAndForgetWorkItem(_ => { })); }
        }
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
                DrainCleanup(service);
                Volatile.Write(ref _active, item);
                try {
                    if (Volatile.Read(ref _disposed) != 0) { item.Cancel(); }
                    else { item.Execute(service); }
                } finally {
                    Volatile.Write(ref _active, null);
                }
                DrainCleanup(service);
            }
            DrainCleanup(service);
        } finally {
            Current = null;
            // Only the worker owns disposal; a timed-out shutdown may still be consuming.
            _queue.Dispose();
        }
    }

    private void DrainCleanup(UiAutomationControlService service) {
        while (true) {
            IWorkItem item;
            lock (_sync) {
                if (_cleanup.Count == 0) { return; }
                item = _cleanup.Dequeue();
            }
            Volatile.Write(ref _active, item);
            try { item.Execute(service); }
            finally { Volatile.Write(ref _active, null); }
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

    private sealed class WorkItem<T> : IWorkItem, IDisposable {
        private readonly Func<UiAutomationControlService, T> _operation;
        private readonly WorkerInvocation<T> _invocation;
        internal WorkItem(Func<UiAutomationControlService, T> operation, Action<T>? abandonedResultHandler) {
            _operation = operation;
            _invocation = new WorkerInvocation<T>(abandonedResultHandler);
        }
        public bool IsAbandonedInFlight => _invocation.IsAbandonedInFlight;
        public void Cancel() {
            _invocation.Execute(() => throw new ObjectDisposedException(nameof(UiAutomationStaDispatcher)));
        }
        public void Execute(UiAutomationControlService service) {
            _invocation.Execute(() => _operation(service));
        }
        internal T GetResult(int timeoutMilliseconds) {
            try { return _invocation.GetResult("UI Automation", timeoutMilliseconds); }
            catch (NativeOperationOutcomeUnknownException) { throw new UiAutomationOperationInFlightException(timeoutMilliseconds); }
        }
        internal void Reject() => _invocation.Reject();
        public void Dispose() => _invocation.Dispose();
    }
}
