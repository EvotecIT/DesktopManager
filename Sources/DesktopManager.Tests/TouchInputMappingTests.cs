namespace DesktopManager.Tests;

[TestClass]
/// <summary>Tests reading touch-to-display mappings from registry values.</summary>
public class TouchInputMappingTests {
    [TestMethod]
    /// <summary>Only non-empty string display paths are returned, once per display regardless of case.</summary>
    public void ParseMonitorDevicePaths_KeepsDistinctStringPaths() {
        const string strip = @"\\?\DISPLAY#CRXED00#5&6c93d9b&0&UID1028#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";
        const string main = @"\\?\DISPLAY#DELA1B2#5&1a2b3c&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";

        var paths = TouchInputMapping.ParseMonitorDevicePaths(new object?[] { strip, 20, null, " ", main, strip.ToUpperInvariant() });

        CollectionAssert.AreEqual(new[] { strip, main }, paths.ToArray());
    }
}
