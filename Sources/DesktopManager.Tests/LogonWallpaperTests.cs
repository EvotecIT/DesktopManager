using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for logon wallpaper functionality.
/// </summary>
public class LogonWallpaperTests {
    [TestMethod]
    /// <summary>
    /// Ensure SetLogonWallpaper does not throw for existing file.
    /// </summary>
    public void SetLogonWallpaper_NoThrow() {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var service = new MonitorService(new FakeDesktopManager());
        string temp = Path.GetTempFileName();
        File.WriteAllBytes(temp, new byte[] { 1 });
        try {
            service.SetLogonWallpaper(temp);
        } finally {
            File.Delete(temp);
        }
        Assert.IsTrue(true);
    }

    [TestMethod]
    /// <summary>
    /// Ensure SetLogonWallpaper throws when Windows Runtime types are missing.
    /// </summary>
    public void SetLogonWallpaper_ThrowsOnMissingRuntime() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires non-Windows");
        }

        var service = new MonitorService(new FakeDesktopManager());
        Assert.ThrowsException<InvalidOperationException>(() => service.SetLogonWallpaper("path"));
    }

    [TestMethod]
    /// <summary>
    /// Ensure GetLogonWallpaper throws when Windows Runtime types are missing.
    /// </summary>
    public void GetLogonWallpaper_ThrowsOnMissingRuntime() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires non-Windows");
        }

        var service = new MonitorService(new FakeDesktopManager());
        Assert.ThrowsException<InvalidOperationException>(() => service.GetLogonWallpaper());
}
}
