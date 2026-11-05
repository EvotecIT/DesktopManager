using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// One dedicated multithreaded-apartment thread that owns an audio object's Core Audio references,
/// so callers never initialize COM and native pointers never enter a caller's apartment.
/// </summary>
internal sealed class AudioComThread : IDisposable {
    private const uint MultithreadedApartment = 0x0;
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    internal AudioComThread(string name) {
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _thread = new Thread(() => Run(ready)) { IsBackground = true, Name = name };
        _thread.SetApartmentState(ApartmentState.MTA);
        _thread.Start();
        ready.Task.GetAwaiter().GetResult();
    }

    internal bool IsCurrent => Thread.CurrentThread == _thread;

    /// <summary>Enters the MTA on the current thread. Pair a successful call with <see cref="LeaveApartment"/>.</summary>
    /// <exception cref="COMException">The thread already belongs to a single-threaded apartment or COM failed to initialize.</exception>
    internal static void EnterApartment() {
        int result = MonitorNativeMethods.CoInitializeEx(IntPtr.Zero, MultithreadedApartment);
        if (result < 0) {
            Marshal.ThrowExceptionForHR(result);
        }
    }

    internal static void LeaveApartment() => MonitorNativeMethods.CoUninitialize();

    /// <summary>Runs <paramref name="operation"/> on the apartment thread and returns its result or rethrows its exception.</summary>
    /// <exception cref="ObjectDisposedException">The thread has stopped.</exception>
    internal T Invoke<T>(Func<T> operation) {
        if (IsCurrent) {
            return operation();
        }
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        try {
            _queue.Add(() => {
                try {
                    completion.SetResult(operation());
                } catch (Exception exception) {
                    completion.SetException(exception);
                }
            });
        } catch (InvalidOperationException) {
            throw new ObjectDisposedException(_thread.Name);
        }
        return completion.Task.GetAwaiter().GetResult();
    }

    /// <summary>Stops accepting work, finishes queued work, and waits for the thread to leave its apartment.</summary>
    public void Dispose() {
        if (_queue.IsAddingCompleted) {
            return;
        }
        _queue.CompleteAdding();
        if (!IsCurrent) {
            _thread.Join();
        }
    }

    private void Run(TaskCompletionSource<bool> ready) {
        try {
            EnterApartment();
        } catch (Exception exception) {
            _queue.CompleteAdding();
            ready.SetException(exception);
            return;
        }
        ready.SetResult(true);
        try {
            foreach (Action action in _queue.GetConsumingEnumerable()) {
                action();
            }
        } finally {
            LeaveApartment();
        }
    }
}
