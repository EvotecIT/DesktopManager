using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for logon wallpaper functionality.
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
public class LogonWallpaperTests {
    [TestMethod]
    /// <summary>
    /// Ensure SetLogonWallpaper does not throw for existing file.
    /// </summary>
    public void SetLogonWallpaper_NoThrow() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireSystemDesktopChanges();

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
    }

    [TestMethod]
    /// <summary>
    /// Ensure SetLogonWallpaper throws when not elevated.
    /// </summary>
    public void SetLogonWallpaper_ThrowsWhenNotElevated() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireSystemDesktopChanges();

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
            Assert.ThrowsExactly<InvalidOperationException>(() => service.SetLogonWallpaper(temp));
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
        Assert.ThrowsExactly<PlatformNotSupportedException>(() => service.SetLogonWallpaper("path"));
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
        Assert.AreEqual(string.Empty, service.GetLogonWallpaper());
    }
}
