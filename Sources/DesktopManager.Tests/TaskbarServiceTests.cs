using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>Tests for <see cref="TaskbarService"/>.</summary>
public class TaskbarServiceTests {
    [TestMethod]
    /// <summary>Ensure taskbars can be enumerated.</summary>
    public void GetTaskbars_ReturnsItems() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        TaskbarService service = new TaskbarService();
        var bars = service.GetTaskbars();
        Assert.IsTrue(bars.Count >= 1);
    }

    [TestMethod]
    public void CreateTaskbarBounds_PreservesCapturedThicknessAtEveryEdge() {
        var monitor = new RECT { Left = -1920, Top = 0, Right = 0, Bottom = 1080 };

        RECT left = TaskbarService.CreateTaskbarBounds(monitor, TaskbarPosition.Left, 48, 40);
        RECT top = TaskbarService.CreateTaskbarBounds(monitor, TaskbarPosition.Top, 48, 40);
        RECT right = TaskbarService.CreateTaskbarBounds(monitor, TaskbarPosition.Right, 48, 40);
        RECT bottom = TaskbarService.CreateTaskbarBounds(monitor, TaskbarPosition.Bottom, 48, 40);

        AssertRect(left, -1920, 0, -1872, 1080);
        AssertRect(top, -1920, 0, 0, 40);
        AssertRect(right, -48, 0, 0, 1080);
        AssertRect(bottom, -1920, 1040, 0, 1080);
    }

    [TestMethod]
    public void ConstrainTaskbarThickness_RestoresThicknessAfterShellQuery() {
        var bounds = new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };

        TaskbarService.ConstrainTaskbarThickness(ref bounds, TaskbarPosition.Bottom, 48, 40);

        AssertRect(bounds, 0, 1040, 1920, 1080);
    }

    private static void AssertRect(RECT actual, int left, int top, int right, int bottom) {
        Assert.AreEqual(left, actual.Left);
        Assert.AreEqual(top, actual.Top);
        Assert.AreEqual(right, actual.Right);
        Assert.AreEqual(bottom, actual.Bottom);
    }
}
