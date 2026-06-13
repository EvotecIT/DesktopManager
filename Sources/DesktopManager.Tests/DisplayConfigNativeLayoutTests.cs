#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for DisplayConfig P/Invoke structure layouts.
/// </summary>
public class DisplayConfigNativeLayoutTests {
    [TestMethod]
    /// <summary>
    /// Ensures DisplayConfig device info packets match Windows SDK layouts used by DisplayConfigGetDeviceInfo.
    /// </summary>
    public void DisplayConfigDeviceInfoPackets_MatchWindowsSdkSizes() {
        Assert.AreEqual(20, Marshal.SizeOf<DisplayConfigDeviceInfoHeader>());
        Assert.AreEqual(84, Marshal.SizeOf<DisplayConfigSourceDeviceName>());
        Assert.AreEqual(32, Marshal.SizeOf<DisplayConfigGetAdvancedColorInfo>());
        Assert.AreEqual(36, Marshal.SizeOf<DisplayConfigGetAdvancedColorInfo2>());
        Assert.AreEqual(24, Marshal.SizeOf<DisplayConfigSetAdvancedColorState>());
        Assert.AreEqual(24, Marshal.SizeOf<DisplayConfigSetHdrState>());
        Assert.AreEqual(24, Marshal.SizeOf<DisplayConfigSdrWhiteLevel>());
    }

    [TestMethod]
    /// <summary>
    /// Ensures active path buffers passed to QueryDisplayConfig use the expected Windows SDK layouts.
    /// </summary>
    public void DisplayConfigPathPackets_MatchWindowsSdkSizes() {
        Assert.AreEqual(20, Marshal.SizeOf<DisplayConfigPathSourceInfo>());
        Assert.AreEqual(48, Marshal.SizeOf<DisplayConfigPathTargetInfo>());
        Assert.AreEqual(72, Marshal.SizeOf<DisplayConfigPathInfo>());
        Assert.AreEqual(64, Marshal.SizeOf<DisplayConfigModeInfo>());
    }
}
#endif
