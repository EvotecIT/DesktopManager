using System.Threading;

namespace DesktopManager.AotSmoke;

/// <summary>Exercises the reported monitor-to-wallpaper Invoke path from a NativeAOT STA caller.</summary>
internal static class StaWorkerInvocationProbe {
    internal static void Run() {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) {
            throw new InvalidOperationException("The worker invocation probe requires an STA caller.");
        }
        var monitors = new Monitors().GetMonitorsConnected();
        if (monitors.Count == 0) { throw new UnavailableException("No connected monitors are available for the wallpaper Invoke probe."); }
        var context = new CompletionObservationContext();
        var previous = SynchronizationContext.Current;
        try {
            SynchronizationContext.SetSynchronizationContext(context);
            for (int attempt = 0; attempt < 8; attempt++) {
                if (new Monitors().GetMonitorsConnected().Count == 0) {
                    throw new InvalidOperationException("A completed wallpaper operation lost its monitor result.");
                }
            }
        } finally {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        if (context.Waits == 0) { throw new InvalidOperationException("The real wallpaper completion wait was not observed."); }
        Console.WriteLine($"Observed {context.Waits} bounded kernel waits on STA; completed monitor results survived a failed wait observation. No settings changed.");
    }

    private sealed class CompletionObservationContext : SynchronizationContext {
        internal int Waits { get; private set; }
        internal CompletionObservationContext() => SetWaitNotificationRequired();

        public override int Wait(IntPtr[] handles, bool waitAll, int millisecondsTimeout) {
            if (millisecondsTimeout <= 0 || millisecondsTimeout > 15000) {
                throw new InvalidOperationException("The wallpaper invocation used an unbounded completion wait.");
            }
            if (WaitHelper(handles, waitAll, 5000) != 0) {
                throw new TimeoutException("The real wallpaper worker did not complete during the probe.");
            }
            Waits++;
            return WaitHandle.WaitTimeout;
        }
    }
}
