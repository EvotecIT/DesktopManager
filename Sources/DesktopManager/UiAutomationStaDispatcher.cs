using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Indicates that a timed-out UI Automation mutation had already started and may still complete.
/// </summary>
internal sealed class UiAutomationOperationInFlightException : TimeoutException {
    internal UiAutomationOperationInFlightException(int timeoutMilliseconds)
        : base($"UI Automation did not complete within {timeoutMilliseconds}ms and had already started; its outcome is unknown.") {
    }
}

/// <summary>
/// Serializes UI Automation work on one reusable STA thread.
/// </summary>
internal sealed class UiAutomationStaDispatcher : IDisposable {
    internal const int DefaultInvocationTimeoutMilliseconds = 15000;
    private readonly BlockingCollection<IWorkItem> _queue = new();
    private readonly Thread _thread;
    private bool _disposed;

    internal UiAutomationStaDispatcher() {
        _thread = new Thread(Run) {
            IsBackground = true,
            Name = "DesktopManager UI Automation"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    internal bool IsCurrentThread => Thread.CurrentThread == _thread;

    internal T Invoke<T>(Func<UiAutomationControlService, T> operation) {
        return Invoke(operation, DefaultInvocationTimeoutMilliseconds);
    }

    internal T Invoke<T>(
        Func<UiAutomationControlService, T> operation,
        int timeoutMilliseconds,
        Action<T>? abandonedResultHandler = null) {
        if (operation == null) {
            throw new ArgumentNullException(nameof(operation));
        }
        if (timeoutMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
        }
        if (_disposed) {
            throw new ObjectDisposedException(nameof(UiAutomationStaDispatcher));
        }

        var workItem = new WorkItem<T>(operation, abandonedResultHandler);
        _queue.Add(workItem);
        return workItem.GetResult(timeoutMilliseconds);
    }

    internal void Post(Action<UiAutomationControlService> operation) {
        if (operation == null) {
            throw new ArgumentNullException(nameof(operation));
        }
        if (_disposed) {
            throw new ObjectDisposedException(nameof(UiAutomationStaDispatcher));
        }

        _queue.Add(new FireAndForgetWorkItem(operation));
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _queue.CompleteAdding();
        if (Thread.CurrentThread != _thread) {
            _thread.Join();
        }
        _queue.Dispose();
    }

    private void Run() {
        var service = new UiAutomationControlService();
        foreach (IWorkItem workItem in _queue.GetConsumingEnumerable()) {
            workItem.Execute(service);
        }
    }

    private interface IWorkItem {
        void Execute(UiAutomationControlService service);
    }

    private sealed class FireAndForgetWorkItem : IWorkItem {
        private readonly Action<UiAutomationControlService> _operation;

        internal FireAndForgetWorkItem(Action<UiAutomationControlService> operation) {
            _operation = operation;
        }

        public void Execute(UiAutomationControlService service) {
            try {
                _operation(service);
            } catch {
                // Fire-and-forget cleanup must not terminate the shared dispatcher.
            }
        }
    }

    private sealed class WorkItem<T> : IWorkItem {
        private const int Queued = 0;
        private const int Executing = 1;
        private const int Completed = 2;
        private const int Abandoned = 3;
        private const int AbandonedInFlight = 4;
        private readonly Func<UiAutomationControlService, T> _operation;
        private readonly Action<T>? _abandonedResultHandler;
        private readonly ManualResetEventSlim _completed = new(false);
        private ExceptionDispatchInfo? _exception;
        private T _result = default!;
        private int _state = Queued;

        internal WorkItem(Func<UiAutomationControlService, T> operation, Action<T>? abandonedResultHandler) {
            _operation = operation;
            _abandonedResultHandler = abandonedResultHandler;
        }

        public void Execute(UiAutomationControlService service) {
            if (Interlocked.CompareExchange(ref _state, Executing, Queued) != Queued) {
                return;
            }

            try {
                _result = _operation(service);
            } catch (Exception ex) {
                _exception = ExceptionDispatchInfo.Capture(ex);
            } finally {
                int priorState = Interlocked.CompareExchange(ref _state, Completed, Executing);
                _completed.Set();
                if (priorState == AbandonedInFlight) {
                    if (_exception == null && _abandonedResultHandler != null) {
                        try {
                            _abandonedResultHandler(_result);
                        } catch {
                            // Late-result cleanup must not terminate the shared dispatcher.
                        }
                    }

                    _completed.Dispose();
                }
            }
        }

        internal T GetResult(int timeoutMilliseconds) {
            if (!_completed.Wait(timeoutMilliseconds)) {
                if (Interlocked.CompareExchange(ref _state, Abandoned, Queued) == Queued) {
                    _completed.Dispose();
                    throw new TimeoutException($"UI Automation did not complete within {timeoutMilliseconds}ms and was canceled before it started.");
                }

                if (Interlocked.CompareExchange(ref _state, AbandonedInFlight, Executing) == Executing) {
                    throw new UiAutomationOperationInFlightException(timeoutMilliseconds);
                }

                _completed.Wait();
            }

            try {
                _exception?.Throw();
                return _result;
            } finally {
                _completed.Dispose();
            }
        }
    }
}
