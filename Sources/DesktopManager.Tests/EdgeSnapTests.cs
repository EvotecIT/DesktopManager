using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for edge snapping logic.
/// </summary>
public class EdgeSnapTests {
    [TestMethod]
    /// <summary>
    /// GetSnapPosition detects left edge.
    /// </summary>
    public void GetSnapPosition_LeftEdge() {
        var bounds = new RECT { Left = 0, Top = 0, Right = 1000, Bottom = 1000 };
        var pos = new WindowPosition { Left = 5, Top = 100, Right = 305, Bottom = 400 };
        var snap = WindowManager.GetSnapPosition(pos, bounds, 10);
        Assert.AreEqual(SnapPosition.Left, snap);
    }

    [TestMethod]
    /// <summary>
    /// GetSnapPosition detects top-right corner.
    /// </summary>
    public void GetSnapPosition_TopRight() {
        var bounds = new RECT { Left = 0, Top = 0, Right = 800, Bottom = 600 };
        var pos = new WindowPosition { Left = 790, Top = 5, Right = 810, Bottom = 305 };
        var snap = WindowManager.GetSnapPosition(pos, bounds, 15);
        Assert.AreEqual(SnapPosition.TopRight, snap);
    }
}
