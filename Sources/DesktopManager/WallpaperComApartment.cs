using System.Collections.Concurrent;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Process-wide MTA for the out-of-process wallpaper service. Shell operations and
/// checkpoint resolution run here so native pointers never enter a caller's STA.
/// </summary>
internal static class WallpaperComApartment {
    private static readonly WorkerGate StartGate = new();
    private static BlockingCollection<Action>? _queue;
    [ThreadStatic] private static bool _isCurrent;

    /// <summary>Runs on the wallpaper apartment with a fifteen-second completion deadline.</summary>
    /// <exception cref="TimeoutException">Startup/admission timed out, or queued work was canceled before execution.</exception>
    /// <exception cref="NativeOperationOutcomeUnknownException">The operation is still running; inspect state before retrying mutations.</exception>
    internal static T Invoke<T>(Func<T> operation, Action<T>? abandonedResultHandler = null) {
        if (_isCurrent) { return operation(); }
        var queue = GetQueue();
        using var invocation = new WorkerInvocation<T>(abandonedResultHandler ?? (result => {
            if (result is IDisposable disposable) { disposable.Dispose(); }
        }));
        bool admitted = false;
        try {
            admitted = queue.TryAdd(() => invocation.Execute(operation));
            if (!admitted) { throw new TimeoutException("The wallpaper COM queue is full; the operation was not started."); }
        } catch {
            if (!admitted) { invocation.Reject(); }
            throw;
        }
        return invocation.GetResult("Wallpaper COM");
    }

    private static BlockingCollection<Action> GetQueue() {
        var queue = Volatile.Read(ref _queue);
        if (queue != null) { return queue; }
        StartGate.Enter("Wallpaper COM startup");
        try {
            // Publish only successful starts, so a transient startup failure can be retried.
            queue = _queue;
            if (queue == null) { Volatile.Write(ref _queue, queue = Start()); }
            return queue;
        } finally { StartGate.Exit(); }
    }

    private static BlockingCollection<Action> Start() {
        var queue = new BlockingCollection<Action>(64);
        var ready = new ThreadStartup();
        var thread = new Thread(() => {
            try {
                AudioComThread.EnterApartment();
            } catch (Exception exception) {
                ready.Signal(exception);
                return;
            }
            _isCurrent = true;
            ready.Signal();
            try {
                foreach (var action in queue.GetConsumingEnumerable()) { action(); }
            } finally {
                _isCurrent = false;
                AudioComThread.LeaveApartment();
            }
        }) { IsBackground = true, Name = "DesktopManager wallpaper COM" };
        thread.SetApartmentState(ApartmentState.MTA);
        try {
            ready.Start(thread, "wallpaper COM thread");
        } catch {
            queue.CompleteAdding();
            throw;
        }
        return queue;
    }
}
