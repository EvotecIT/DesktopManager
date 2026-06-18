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

    [TestMethod]
    /// <summary>
    /// Legacy Advanced Color packets should keep HDR state independent from wide-color enforcement.
    /// </summary>
    public void CreateLegacyAdvancedColorInfo_WideColorNotEnforced_KeepsLegacyHdrState() {
        Monitor monitor = new(new MonitorService(new StubDesktopManager())) {
            Index = 1,
            DeviceId = "DISPLAY1",
            DeviceName = "\\\\.\\DISPLAY1"
        };
        DisplayConfigGetAdvancedColorInfo packet = new() {
            Value = 0x3,
            ColorEncoding = DisplayConfigColorEncoding.Rgb,
            BitsPerColorChannel = 10
        };

        MonitorAdvancedColorInfo result = MonitorService.CreateLegacyAdvancedColorInfo(monitor, packet);

        Assert.IsTrue(result.AdvancedColorSupported);
        Assert.IsTrue(result.AdvancedColorEnabled);
        Assert.IsFalse(result.WideColorEnforced);
        Assert.IsTrue(result.HdrSupported);
        Assert.IsTrue(result.HdrEnabled);
    }

    [TestMethod]
    /// <summary>
    /// Legacy Advanced Color packets should not expose WCG-only Advanced Color as HDR-toggleable.
    /// </summary>
    public void CreateLegacyAdvancedColorInfo_WideColorEnforced_DoesNotReportHdr() {
        Monitor monitor = new(new MonitorService(new StubDesktopManager())) {
            Index = 1,
            DeviceId = "DISPLAY1",
            DeviceName = "\\\\.\\DISPLAY1"
        };
        DisplayConfigGetAdvancedColorInfo packet = new() {
            Value = 0x7,
            ColorEncoding = DisplayConfigColorEncoding.Rgb,
            BitsPerColorChannel = 10
        };

        MonitorAdvancedColorInfo result = MonitorService.CreateLegacyAdvancedColorInfo(monitor, packet);

        Assert.IsTrue(result.AdvancedColorSupported);
        Assert.IsTrue(result.AdvancedColorEnabled);
        Assert.IsTrue(result.WideColorEnforced);
        Assert.IsFalse(result.HdrSupported);
        Assert.IsFalse(result.HdrEnabled);
    }

    [TestMethod]
    /// <summary>
    /// Fallback monitor enumeration should use the source display name before child monitor names.
    /// </summary>
    public void GetDisplayConfigSourceNameCandidates_FallbackMonitor_PrefersSourceDisplayName() {
        Monitor monitor = new(new MonitorService(new StubDesktopManager())) {
            DeviceId = "\\\\.\\DISPLAY1",
            DeviceName = "\\\\.\\DISPLAY1\\Monitor0"
        };

        IReadOnlyList<string> candidates = MonitorService.GetDisplayConfigSourceNameCandidates(monitor);

        Assert.AreEqual(2, candidates.Count);
        Assert.AreEqual("\\\\.\\DISPLAY1", candidates[0]);
        Assert.AreEqual("\\\\.\\DISPLAY1\\Monitor0", candidates[1]);
    }
}
#endif
