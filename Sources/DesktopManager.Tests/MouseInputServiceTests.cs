using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for MouseInputService.
/// </summary>
public class MouseInputServiceTests {
    [TestMethod]
    /// <summary>
    /// Test for MoveCursor_DoesNotThrow.
    /// </summary>
    public void MoveCursor_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        MouseInputService.MoveCursor(0, 0);
    }

    [TestMethod]
    /// <summary>
    /// Test for Click_DoesNotThrow.
    /// </summary>
    public void Click_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        MouseInputService.Click(MouseButton.Left);
    }

    [TestMethod]
    /// <summary>
    /// Test for Scroll_DoesNotThrow.
    /// </summary>
    public void Scroll_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        MouseInputService.Scroll(0);
    }

    [TestMethod]
    /// <summary>
    /// Test for MouseDrag_DoesNotThrow.
    /// </summary>
    public void MouseDrag_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        MouseInputService.MouseDrag(MouseButton.Left, 0, 0, 1, 1, 0);
    }
}
