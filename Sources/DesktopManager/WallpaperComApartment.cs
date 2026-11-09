using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Process-wide MTA for the out-of-process wallpaper service. Shell operations and
/// checkpoint resolution run here so native pointers never enter a caller's STA.
/// </summary>
internal static class WallpaperComApartment {
    private static readonly Lazy<BlockingCollection<Action>> Queue = new(Start);
    [ThreadStatic] private static bool _isCurrent;

    internal static T Invoke<T>(Func<T> operation) {
        if (_isCurrent) { return operation(); }
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Queue.Value.Add(() => {
            try { completion.SetResult(operation()); }
            catch (Exception exception) { completion.SetException(exception); }
        });
        return completion.Task.GetAwaiter().GetResult();
    }

    private static BlockingCollection<Action> Start() {
        var queue = new BlockingCollection<Action>();
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
