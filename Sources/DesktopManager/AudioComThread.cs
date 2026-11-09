using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// One dedicated multithreaded-apartment thread that owns an audio object's Core Audio references,
/// so callers never initialize COM and native pointers never enter a caller's apartment.
/// </summary>
internal sealed class AudioComThread : IDisposable {
    private const uint MultithreadedApartment = 0x0;
    private readonly BlockingCollection<Action> _queue = new(64);
    private readonly Thread _thread;
    private readonly WorkerCompletion _stopped = new();
    private Action? _shutdownCleanup;
    private int _disposed;

    internal AudioComThread(string name) {
        var ready = new ThreadStartup();
        _thread = new Thread(() => Run(ready)) { IsBackground = true, Name = name };
        _thread.SetApartmentState(ApartmentState.MTA);
        try {
            ready.Start(_thread, "audio COM thread");
        } catch {
            // A late COM initialization must drain an empty, completed queue and leave the apartment.
            if ((_thread.ThreadState & ThreadState.Unstarted) != 0) { _stopped.Complete(); }
            Dispose();
            throw;
        }
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
        if (Volatile.Read(ref _disposed) != 0) { throw new ObjectDisposedException(_thread.Name); }
        if (IsCurrent) {
            return operation();
        }
        using var invocation = new WorkerInvocation<T>();
        try {
            if (!_queue.TryAdd(() => invocation.Execute(operation))) {
                throw new TimeoutException("The audio COM queue is full; the operation was not started.");
            }
        } catch (InvalidOperationException) {
            invocation.Reject();
            throw new ObjectDisposedException(_thread.Name);
        } catch {
            invocation.Reject();
            throw;
        }
        return invocation.GetResult("Audio COM");
    }

    /// <summary>Stops accepting work, finishes queued work, and waits for the thread to leave its apartment.</summary>
    public void Dispose() => Dispose(null);

    internal void Dispose(Action? shutdownCleanup) {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) { return; }
        _shutdownCleanup = shutdownCleanup;
        _queue.CompleteAdding();
        try {
            if (!IsCurrent && !_stopped.Wait(2000)) {
                DesktopManagerDiagnostics.Report("Audio COM shutdown is pending on an in-flight native call.");
            }
        } finally {
            _stopped.Dispose();
        }
    }

    private void Run(ThreadStartup ready) {
        bool entered = false;
        try {
            try {
                EnterApartment();
                entered = true;
            } catch (Exception exception) {
                _queue.CompleteAdding();
                ready.Signal(exception);
                return;
            }
            ready.Signal();
            foreach (Action action in _queue.GetConsumingEnumerable()) {
                action();
            }
        } finally {
            try {
                if (entered) {
                    try { _shutdownCleanup?.Invoke(); }
                    catch (Exception exception) { DesktopManagerDiagnostics.Report($"Audio COM cleanup failed: {exception.Message}"); }
                    finally { LeaveApartment(); }
                }
            } finally { _stopped.Complete(); }
        }
    }
}
