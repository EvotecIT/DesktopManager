using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorEdidParsingTests {
    [TestMethod]
    public void ParseManufacturerFromEdid_ReturnsCorrectCode() {
        byte[] edid = new byte[128];
        edid[8] = 0x04;
        edid[9] = 0x72;

        var method = typeof(Monitor).GetMethod("ParseManufacturerFromEdid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        string manufacturer = (string)method!.Invoke(null, new object[] { edid })!;

        Assert.AreEqual("ACR", manufacturer);
    }

    [TestMethod]
    public void ParseSerialNumberFromEdid_ReturnsCorrectValue() {
        byte[] edid = new byte[128];
        edid[12] = 0x4D;
        edid[13] = 0x3C;
        edid[14] = 0x2B;
        edid[15] = 0x1A;

        var method = typeof(Monitor).GetMethod("ParseSerialNumberFromEdid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        string serial = (string)method!.Invoke(null, new object[] { edid })!;

        Assert.AreEqual("439041101", serial);
    }
}
