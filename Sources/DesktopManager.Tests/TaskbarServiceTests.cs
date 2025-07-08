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
}
