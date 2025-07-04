using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for BackgroundColorTests.
/// </summary>
public class BackgroundColorTests {
    [TestMethod]
    /// <summary>
    /// Test for SetAndGetBackgroundColor_RoundTrips.
    /// </summary>
    public void SetAndGetBackgroundColor_RoundTrips() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var monitors = new Monitors();
        uint original = monitors.GetBackgroundColor();
        uint newColor = original == 0xFF0000u ? 0x00FF00u : 0xFF0000u;

        monitors.SetBackgroundColor(newColor);
        uint roundTrip = monitors.GetBackgroundColor();
        monitors.SetBackgroundColor(original);

        Assert.AreEqual(newColor, roundTrip);
    }
}
