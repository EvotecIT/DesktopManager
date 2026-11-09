using System.Threading;

namespace DesktopManager.Tests;

/// <summary>Protects an STA caller waiting for completed wallpaper apartment work.</summary>
[TestClass]
[DoNotParallelize]
public class WallpaperComApartmentTests {
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void SetWallpaper_FromStream_KeepsInputUntilAbandonedWorkerFinishes(bool allMonitors, bool interruptWait) {
        WallpaperComApartment.Invoke(() => true);
        using var started = new ManualResetEvent(false);
        using var release = new ManualResetEvent(false);
        var native = new DelayedWallpaperDesktopManager(started, release);
        native.DevicePaths[0] = "test-monitor";
        var service = new MonitorService(native);
        Exception? failure = null;
        var caller = new Thread(() => {
            if (!interruptWait) { SynchronizationContext.SetSynchronizationContext(new InFlightTimeoutContext(started)); }
            try {
                using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
                Action apply = () => {
                    if (allMonitors) { service.SetWallpaper(stream); }
                    else { service.SetWallpaper("test-monitor", stream); }
                };
                if (interruptWait) { Assert.ThrowsExactly<ThreadInterruptedException>(apply); }
                else { Assert.ThrowsExactly<NativeOperationOutcomeUnknownException>(apply); }
            } catch (Exception exception) { failure = exception; }
            finally { SynchronizationContext.SetSynchronizationContext(null); }
        }) { IsBackground = true };
        caller.SetApartmentState(ApartmentState.STA);
        caller.Start();
        try {
            if (interruptWait) {
                Assert.IsTrue(started.WaitOne(5000), "The worker must own the input before its caller is interrupted.");
                caller.Interrupt();
            }
            Assert.IsTrue(caller.Join(5000), "The caller must return while wallpaper work is still running.");
            if (failure != null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); }
            Assert.IsNotNull(native.InputPath);
            Assert.IsTrue(File.Exists(native.InputPath), "A timed-out caller must not delete an executing worker's input.");
        } finally {
            release.Set();
            // Drain the real apartment, including the operation's finally, before inspecting cleanup.
            WallpaperComApartment.Invoke(() => true);
        }
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, native.ConsumedBytes);
        Assert.IsFalse(File.Exists(native.InputPath), "The worker must delete its temporary input after finishing.");
    }

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

    private sealed class InFlightTimeoutContext : SynchronizationContext {
        private readonly WaitHandle _started;
        internal InFlightTimeoutContext(WaitHandle started) {
            _started = started;
            SetWaitNotificationRequired();
        }
        public override int Wait(IntPtr[] handles, bool waitAll, int millisecondsTimeout) {
            if (WaitHelper(new[] { _started.SafeWaitHandle.DangerousGetHandle() }, false, 5000) != 0) {
                throw new TimeoutException("The wallpaper worker did not start consuming its input.");
            }
            return WaitHandle.WaitTimeout;
        }
    }

    private sealed class DelayedWallpaperDesktopManager : FakeDesktopManager, IDesktopManager {
        private readonly EventWaitHandle _started;
        private readonly WaitHandle _release;
        internal string? InputPath;
        internal byte[] ConsumedBytes = Array.Empty<byte>();
        internal DelayedWallpaperDesktopManager(EventWaitHandle started, WaitHandle release) {
            _started = started;
            _release = release;
        }
        public new void SetWallpaper(string monitorId, string wallpaper) {
            WallpaperComApartment.Invoke(() => {
                InputPath = wallpaper;
                _started.Set();
                if (!_release.WaitOne(5000)) { throw new TimeoutException("The test did not release wallpaper consumption."); }
                ConsumedBytes = File.ReadAllBytes(wallpaper);
                return true;
            });
        }
    }
}
