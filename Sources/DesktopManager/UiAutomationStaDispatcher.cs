using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace DesktopManager;

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
        private readonly Func<UiAutomationControlService, T> _operation;
        private readonly ManualResetEventSlim _completed = new(false);
        private ExceptionDispatchInfo? _exception;
        private T _result = default!;

        internal WorkItem(Func<UiAutomationControlService, T> operation) {
            _operation = operation;
        }

        public void Execute(UiAutomationControlService service) {
            try {
                _result = _operation(service);
            } catch (Exception ex) {
                _exception = ExceptionDispatchInfo.Capture(ex);
            } finally {
                _completed.Set();
            }
        }

        internal T GetResult(int timeoutMilliseconds) {
            if (!_completed.Wait(timeoutMilliseconds)) {
                throw new TimeoutException($"UI Automation did not complete within {timeoutMilliseconds}ms.");
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
