#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

/// <summary>
/// Verifies duration-based PowerShell event subscriptions retain their expiration owner.
/// </summary>
[TestClass]
public class EventSubscriptionExpirationTests {
    [TestMethod]
    public void Schedule_RetainsTimerUntilCleanupRuns() {
        using var cleanupCompleted = new ManualResetEventSlim();

        global::DesktopManager.PowerShell.EventSubscriptionExpiration.Schedule(
            TimeSpan.FromMilliseconds(100),
            cleanupCompleted.Set);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsTrue(cleanupCompleted.Wait(TimeSpan.FromSeconds(5)), "The retained expiration timer did not invoke cleanup.");
    }
}
#endif
