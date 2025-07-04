using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for MonitorBoundsTests.
/// </summary>
public class MonitorBoundsTests {
    [TestMethod]
    /// <summary>
    /// Test for Constructor_SetsFields.
    /// </summary>
    public void Constructor_SetsFields() {
        var rect = new RECT { Left = 1, Top = 2, Right = 3, Bottom = 4 };

        var bounds = new MonitorBounds(rect);

        Assert.AreEqual(1, bounds.Left);
        Assert.AreEqual(2, bounds.Top);
        Assert.AreEqual(3, bounds.Right);
        Assert.AreEqual(4, bounds.Bottom);
    }
}

