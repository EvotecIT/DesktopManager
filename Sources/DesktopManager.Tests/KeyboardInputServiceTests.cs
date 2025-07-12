using System.Runtime.InteropServices;
using System.Diagnostics;

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

        KeyboardInputService.PressKey(VirtualKey.VK_F24);
    }

    [TestMethod]
    /// <summary>
    /// Test for PressShortcut_DoesNotThrow.
    /// </summary>
    public void PressShortcut_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        KeyboardInputService.PressShortcut(0, VirtualKey.VK_F23, VirtualKey.VK_F24);
    }

    [TestMethod]
    /// <summary>
    /// Test that delay is honored when pressing shortcuts.
    /// </summary>
    public void PressShortcut_HonorsDelay() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        const int delay = 200;
        Stopwatch sw = Stopwatch.StartNew();
        KeyboardInputService.PressShortcut(delay, VirtualKey.VK_F23, VirtualKey.VK_F24);
        sw.Stop();

        long expected = delay * 4; // 2 keys => 4 events
        Assert.IsTrue(sw.ElapsedMilliseconds >= expected, $"Delay not honored. Expected >= {expected}, got {sw.ElapsedMilliseconds}");
    }

    [TestMethod]
    /// <summary>
    /// Test for KeyDown_DoesNotThrow.
    /// </summary>
    public void KeyDown_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        KeyboardInputService.KeyDown(VirtualKey.VK_F24);
    }

    [TestMethod]
    /// <summary>
    /// Test for KeyUp_DoesNotThrow.
    /// </summary>
    public void KeyUp_DoesNotThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        KeyboardInputService.KeyUp(VirtualKey.VK_F24);
    }
}
