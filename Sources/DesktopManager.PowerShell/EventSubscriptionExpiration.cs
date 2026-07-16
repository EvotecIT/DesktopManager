using System.Timers;

namespace DesktopManager.PowerShell;

/// <summary>
/// Retains event-subscription expiration timers until their cleanup callback completes.
/// </summary>
internal static class EventSubscriptionExpiration {
    private static readonly object SyncRoot = new();
    private static readonly HashSet<Timer> ActiveTimers = new();

    /// <summary>
    /// Schedules cleanup after a positive duration and retains the timer for its full lifetime.
    /// </summary>
    /// <param name="duration">Delay before cleanup. Non-positive values disable expiration.</param>
    /// <param name="cleanup">Cleanup to run when the duration elapses.</param>
    internal static void Schedule(TimeSpan duration, Action cleanup) {
        if (duration <= TimeSpan.Zero) {
            return;
        }
        if (cleanup == null) {
            throw new ArgumentNullException(nameof(cleanup));
        }

        var timer = new Timer(duration.TotalMilliseconds) { AutoReset = false };
        ElapsedEventHandler elapsedHandler = null;
        elapsedHandler = (_, _) => {
            try {
                cleanup();
            } finally {
                timer.Elapsed -= elapsedHandler;
                timer.Dispose();
                lock (SyncRoot) {
                    ActiveTimers.Remove(timer);
                }
            }
        };
        timer.Elapsed += elapsedHandler;

        lock (SyncRoot) {
            ActiveTimers.Add(timer);
        }

        try {
            timer.Start();
        } catch {
            lock (SyncRoot) {
                ActiveTimers.Remove(timer);
            }
            timer.Elapsed -= elapsedHandler;
            timer.Dispose();
            throw;
        }
    }
}
