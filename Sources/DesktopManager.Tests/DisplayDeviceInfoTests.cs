using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for DisplayDeviceInfoTests.
/// </summary>
public class DisplayDeviceInfoTests {
    [TestMethod]
    /// <summary>
    /// Test for Constructor_SetsProperties.
    /// </summary>
    public void Constructor_SetsProperties() {
        var device = new DISPLAY_DEVICE { cb = 1, DeviceName = "Name" };
        var rect = new RECT { Left = 1, Top = 2, Right = 3, Bottom = 4 };

        var info = new DisplayDeviceInfo(device, rect);

        Assert.AreEqual(device, info.DisplayDevice);
        Assert.AreEqual(rect, info.Bounds);
    }
}

