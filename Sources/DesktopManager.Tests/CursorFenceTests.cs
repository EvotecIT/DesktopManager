using System.Diagnostics;
using System.Threading;

namespace DesktopManager.Tests;

/// <summary>Protects the fence constructor's deadline at the CLR wait boundary used by STA callers.</summary>
[TestClass]
[DoNotParallelize]
public class CursorFenceTests {
    [TestMethod]
    public void Constructor_OnSta_ThrowsWhenReadinessCannotBeObserved() {
        Exception? failure = null;
        var context = new ReadinessTimeoutContext();
        var elapsed = new Stopwatch();
        var caller = new Thread(() => {
            SynchronizationContext.SetSynchronizationContext(context);
            elapsed.Start();
            try {
                Assert.ThrowsExactly<TimeoutException>(() => {
                    // Outside the desktop: the real hook is installed, but no input is moved or fenced.
                    using var fence = new CursorFence(new RECT { Left = -32000, Top = -32000, Right = -31999, Bottom = -31999 }, CursorFenceMode.Locked);
                });
            } catch (Exception exception) {
                failure = exception;
            } finally {
                elapsed.Stop();
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }) { IsBackground = true };
        caller.SetApartmentState(ApartmentState.STA);
        caller.Start();

        Assert.IsTrue(caller.Join(TimeSpan.FromSeconds(12)), "The STA constructor exceeded its readiness and shutdown deadlines.");
        if (failure != null) {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        Assert.IsInRange(1, 5000, context.ReadinessTimeoutMilliseconds, "Startup must pass a finite deadline to the CLR wait.");
        Assert.IsTrue(context.WorkerSignaled, "The real hook worker must report readiness while the STA wait simulates a timeout.");
        Assert.IsLessThan(TimeSpan.FromSeconds(10), elapsed.Elapsed);
    }

    private sealed class ReadinessTimeoutContext : SynchronizationContext {
        private bool _firstWait = true;
        internal int ReadinessTimeoutMilliseconds { get; private set; }
        internal bool WorkerSignaled { get; private set; }

        internal ReadinessTimeoutContext() => SetWaitNotificationRequired();

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) {
            if (!_firstWait) {
                return WaitHelper(waitHandles, waitAll, millisecondsTimeout);
            }
            _firstWait = false;
            ReadinessTimeoutMilliseconds = millisecondsTimeout;
            var elapsed = Stopwatch.StartNew();
            WorkerSignaled = WaitHelper(waitHandles, waitAll, 1000) == 0;
            // Model a caller that cannot observe readiness even while the real worker is able to signal it.
            // Bound the negative control too: an infinite product wait fails without hanging the test host.
            int deadline = millisecondsTimeout < 0 ? 5000 : Math.Min(millisecondsTimeout, 5000);
            int remaining = deadline - (int)elapsed.ElapsedMilliseconds;
            if (remaining > 0) {
                Thread.Sleep(remaining);
            }
            return WaitHandle.WaitTimeout;
        }
    }
}
