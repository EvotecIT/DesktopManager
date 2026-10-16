using System;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
public sealed class LockScreenNativeTests {
    [TestMethod]
    public void WaitForCompletion_TerminalStatus_ReturnsWithoutCanceling() {
        var statuses = new Queue<int>(new[] { 0, 1 });
        bool canceled = false;

        LockScreenNative.WaitForCompletion(() => statuses.Dequeue(), () => canceled = true, TimeSpan.FromSeconds(1));

        Assert.IsFalse(canceled);
        Assert.AreEqual(0, statuses.Count);
    }

    [TestMethod]
    public void WaitForCompletion_StaysStartedUntilDeadline_CancelsAndTimesOut() {
        bool canceled = false;

        TimeoutException exception = Assert.ThrowsExactly<TimeoutException>(() =>
            LockScreenNative.WaitForCompletion(() => 0, () => canceled = true, TimeSpan.Zero));

        Assert.IsTrue(canceled);
        StringAssert.Contains(exception.Message, "configured timeout");
    }

    [TestMethod]
    public void WaitForCompletion_CancelFailure_StillReportsTimeout() {
        TimeoutException exception = Assert.ThrowsExactly<TimeoutException>(() =>
            LockScreenNative.WaitForCompletion(() => 0,
                () => throw new InvalidOperationException("cancel failed"), TimeSpan.Zero));

        StringAssert.Contains(exception.Message, "did not complete");
    }
}
