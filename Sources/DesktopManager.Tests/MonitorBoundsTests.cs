using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorBoundsTests {
    [TestMethod]
    public void Constructor_SetsFields() {
        var rect = new RECT { Left = 1, Top = 2, Right = 3, Bottom = 4 };

        var bounds = new MonitorBounds(rect);

        Assert.AreEqual(1, bounds.Left);
        Assert.AreEqual(2, bounds.Top);
        Assert.AreEqual(3, bounds.Right);
        Assert.AreEqual(4, bounds.Bottom);
    }
}

