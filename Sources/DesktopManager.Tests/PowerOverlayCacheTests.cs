namespace DesktopManager.Tests;

/// <summary>Protects when <see cref="SystemPowerService.GetActivePowerPlan"/> reads the power mode overlay again.</summary>
[TestClass]
public sealed class PowerOverlayCacheTests {
    private static readonly Guid Balanced = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid HighPerformance = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    private static readonly Guid BestPerformance = new("ded574b5-45a0-4f42-8737-46345c09c238");

    [TestMethod]
    public void Get_ReadsAgainOnlyWhenRegistryPowerSourceOrPlanChanges() {
        var stamp = new PowerOverlayStamp(100, acLineStatus: 1, systemStatusFlag: 0);
        int reads = 0;
        var cache = new PowerOverlayCache(() => stamp, () => { reads++; return BestPerformance; });

        Assert.AreEqual(BestPerformance, cache.Get(Balanced));
        cache.Get(Balanced);
        Assert.AreEqual(1, reads);

        stamp = new PowerOverlayStamp(200, acLineStatus: 1, systemStatusFlag: 0);
        cache.Get(Balanced);
        Assert.AreEqual(2, reads, "Settings writes the overlay under the power schemes key.");

        stamp = new PowerOverlayStamp(200, acLineStatus: 0, systemStatusFlag: 0);
        cache.Get(Balanced);
        Assert.AreEqual(3, reads, "Unplugging switches to the battery overlay.");

        stamp = new PowerOverlayStamp(200, acLineStatus: 0, systemStatusFlag: 1);
        cache.Get(Balanced);
        Assert.AreEqual(4, reads, "Battery saver can change the effective overlay.");

        cache.Get(HighPerformance);
        Assert.AreEqual(5, reads, "The effective overlay depends on the active plan.");
    }

    [TestMethod]
    public void Get_WithoutStamp_ReadsEveryTime() {
        int reads = 0;
        var cache = new PowerOverlayCache(() => null, () => { reads++; return null; });

        cache.Get(Balanced);
        cache.Get(Balanced);

        Assert.AreEqual(2, reads);
    }

    [TestMethod]
    public void Get_AfterMaximumAge_ReadsAgain() {
        TimeSpan now = TimeSpan.FromHours(1);
        int reads = 0;
        var cache = new PowerOverlayCache(
            () => new PowerOverlayStamp(100, 1, 0),
            () => { reads++; return Guid.Empty; },
            () => now);

        cache.Get(Balanced);
        now += PowerOverlayCache.MaximumAge;
        cache.Get(Balanced);

        Assert.AreEqual(2, reads);
    }
}
