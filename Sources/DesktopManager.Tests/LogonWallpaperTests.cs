using System;
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
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        if (Type.GetType("Windows.System.UserProfile.LockScreen, Windows, ContentType=WindowsRuntime") == null ||
            Type.GetType("Windows.Storage.StorageFile, Windows, ContentType=WindowsRuntime") == null) {
            Assert.Inconclusive("Required Windows Runtime types not available");
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
    /// Ensure SetLogonWallpaper throws when not elevated.
    /// </summary>
    public void SetLogonWallpaper_ThrowsWhenNotElevated() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        if (PrivilegeChecker.IsElevated) {
            Assert.Inconclusive("Test requires non-elevated context");
        }

        if (Type.GetType("Windows.System.UserProfile.LockScreen, Windows, ContentType=WindowsRuntime") == null ||
            Type.GetType("Windows.Storage.StorageFile, Windows, ContentType=WindowsRuntime") == null) {
            Assert.Inconclusive("Required Windows Runtime types not available");
        }

        var service = new MonitorService(new FakeDesktopManager());
        string temp = Path.GetTempFileName();
        File.WriteAllBytes(temp, new byte[] { 1 });
        try {
            Assert.ThrowsException<InvalidOperationException>(() => service.SetLogonWallpaper(temp));
        } finally {
            File.Delete(temp);
        }
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
        Assert.ThrowsException<PlatformNotSupportedException>(() => service.SetLogonWallpaper("path"));
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
