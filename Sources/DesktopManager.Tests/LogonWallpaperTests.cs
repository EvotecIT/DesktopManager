using System.IO;
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
        var monitors = new Monitors();
        string temp = Path.GetTempFileName();
        File.WriteAllBytes(temp, new byte[] {1});
        try {
            monitors.SetLogonWallpaper(temp);
        } finally {
            File.Delete(temp);
        }
        Assert.IsTrue(true);
    }
}
