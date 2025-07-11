using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for <see cref="KeyboardInputService"/>.
/// </summary>
public class KeyboardInputServiceTests {
    [TestMethod]
    /// <summary>
    /// Test for PressKey_DoesNotThrow.
    /// </summary>
    public void PressKey_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        KeyboardInputService.PressKey(VirtualKey.VK_ESCAPE);
    }

    [TestMethod]
    /// <summary>
    /// Test for PressShortcut_DoesNotThrow.
    /// </summary>
    public void PressShortcut_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        KeyboardInputService.PressShortcut(VirtualKey.VK_CONTROL, VirtualKey.VK_C);
    }
}
