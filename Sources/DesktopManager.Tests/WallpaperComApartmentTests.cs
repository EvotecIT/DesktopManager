using System.Threading;

namespace DesktopManager.Tests;

/// <summary>Protects an STA caller waiting for completed wallpaper apartment work.</summary>
[TestClass]
[DoNotParallelize]
public class WallpaperComApartmentTests {
    [TestMethod]
    public void Invoke_OnSta_ReturnsCompletedResultWithoutAnUnboundedWait() {
        using var release = new ManualResetEvent(false);
        var context = new CompletionWaitContext(release);
        Exception? failure = null;
        int result = 0;
        var caller = new Thread(() => {
            try {
                // Start the real apartment before observing the operation's completion wait.
                WallpaperComApartment.Invoke(() => true);
                SynchronizationContext.SetSynchronizationContext(context);
                result = WallpaperComApartment.Invoke(() => {
                    if (!release.WaitOne(5000)) {
                        throw new TimeoutException("The STA wait did not release its worker operation.");
                    }
                    return 42;
                });
            } catch (Exception exception) {
                failure = exception;
            } finally {
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }) { IsBackground = true };
        caller.SetApartmentState(ApartmentState.STA);
        caller.Start();
        try {
            Assert.IsTrue(caller.Join(10000), "The STA invocation did not finish after its worker completed.");
        } finally {
            release.Set();
        }
        if (failure != null) {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        Assert.AreEqual(42, result);
        Assert.IsTrue(context.CompletionObserved, "The real apartment must finish the operation during the wait.");
        Assert.IsInRange(1, 15000, context.TimeoutMilliseconds, "The caller must use a finite completion wait.");
    }

    private sealed class CompletionWaitContext : SynchronizationContext {
        private readonly EventWaitHandle _release;
        internal int TimeoutMilliseconds { get; private set; }
        internal bool CompletionObserved { get; private set; }

        internal CompletionWaitContext(EventWaitHandle release) {
            _release = release;
            SetWaitNotificationRequired();
        }

        public override int Wait(IntPtr[] handles, bool waitAll, int millisecondsTimeout) {
            TimeoutMilliseconds = millisecondsTimeout;
            _release.Set();
            CompletionObserved = WaitHelper(handles, waitAll, 5000) == 0;
            // Exercise completion racing with a failed observation, without trusting another unbounded wait.
            return WaitHandle.WaitTimeout;
        }
    }
}
