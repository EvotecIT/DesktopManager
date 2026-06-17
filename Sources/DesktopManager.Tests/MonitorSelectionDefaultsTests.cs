#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

/// <summary>
/// Tests monitor selection defaults for active-display operations.
/// </summary>
[TestClass]
public sealed class MonitorSelectionDefaultsTests {
    /// <summary>
    /// Advanced Color and HDR operations should default to connected monitors when no monitor selector is provided.
    /// </summary>
    [TestMethod]
    public void ResolveActiveDisplayConnectedOnly_NoSelector_DefaultsToConnected() {
        bool? connectedOnly = global::DesktopManager.Cli.DesktopOperations.ResolveActiveDisplayConnectedOnly(
            connectedOnly: null,
            index: null,
            deviceId: null,
            deviceName: null);

        Assert.AreEqual(true, connectedOnly);
    }

    /// <summary>
    /// Explicit monitor selectors should preserve the caller's broader monitor query.
    /// </summary>
    [TestMethod]
    public void ResolveActiveDisplayConnectedOnly_ExplicitIndex_PreservesUnspecifiedConnectedOnly() {
        bool? connectedOnly = global::DesktopManager.Cli.DesktopOperations.ResolveActiveDisplayConnectedOnly(
            connectedOnly: null,
            index: 4,
            deviceId: null,
            deviceName: null);

        Assert.IsNull(connectedOnly);
    }

    /// <summary>
    /// Explicit connected flags should not be overwritten.
    /// </summary>
    [TestMethod]
    public void ResolveActiveDisplayConnectedOnly_ExplicitConnectedFlag_PreservesValue() {
        bool? connectedOnly = global::DesktopManager.Cli.DesktopOperations.ResolveActiveDisplayConnectedOnly(
            connectedOnly: false,
            index: null,
            deviceId: null,
            deviceName: null);

        Assert.AreEqual(false, connectedOnly);
    }
}
#endif
