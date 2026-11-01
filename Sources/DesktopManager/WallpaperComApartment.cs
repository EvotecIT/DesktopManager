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
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => {
            int result = MonitorNativeMethods.CoInitializeEx(IntPtr.Zero, 0);
            if (result < 0) {
                ready.SetException(Marshal.GetExceptionForHR(result)!);
                return;
            }
            _isCurrent = true;
            ready.SetResult(true);
            try {
                foreach (var action in queue.GetConsumingEnumerable()) { action(); }
            } finally {
                _isCurrent = false;
                if (result >= 0) { MonitorNativeMethods.CoUninitialize(); }
            }
        }) { IsBackground = true, Name = "DesktopManager wallpaper COM" };
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        ready.Task.GetAwaiter().GetResult();
        return queue;
    }
}
