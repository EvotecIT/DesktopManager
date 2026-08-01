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

    internal T Invoke<T>(Func<UiAutomationControlService, T> operation, int timeoutMilliseconds) {
        if (operation == null) {
            throw new ArgumentNullException(nameof(operation));
        }
        if (timeoutMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
        }
        if (_disposed) {
            throw new ObjectDisposedException(nameof(UiAutomationStaDispatcher));
        }

        var workItem = new WorkItem<T>(operation);
        _queue.Add(workItem);
        return workItem.GetResult(timeoutMilliseconds);
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

    private sealed class WorkItem<T> : IWorkItem {
        private const int Queued = 0;
        private const int Executing = 1;
        private const int Completed = 2;
        private const int Abandoned = 3;
        private readonly Func<UiAutomationControlService, T> _operation;
        private readonly ManualResetEventSlim _completed = new(false);
        private ExceptionDispatchInfo? _exception;
        private T _result = default!;
        private int _state = Queued;

        internal WorkItem(Func<UiAutomationControlService, T> operation) {
            _operation = operation;
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
                Volatile.Write(ref _state, Completed);
                _completed.Set();
            }
        }

        internal T GetResult(int timeoutMilliseconds) {
            if (!_completed.Wait(timeoutMilliseconds)) {
                if (Interlocked.CompareExchange(ref _state, Abandoned, Queued) == Queued) {
                    _completed.Dispose();
                    throw new TimeoutException($"UI Automation did not complete within {timeoutMilliseconds}ms and was canceled before it started.");
                }

                if (!_completed.Wait(0)) {
                    throw new UiAutomationOperationInFlightException(timeoutMilliseconds);
                }
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
