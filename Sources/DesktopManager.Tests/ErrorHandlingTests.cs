using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for ErrorHandlingTests.
/// </summary>
public class ErrorHandlingTests {
    [TestMethod]
    /// <summary>
    /// Test for DeleteTempFile_NoThrowOnMissingFile.
    /// </summary>
    public void DeleteTempFile_NoThrowOnMissingFile() {
        var method = typeof(MonitorService).GetMethod("DeleteTempFile", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        method.Invoke(null, new object?[] { System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString()) });
    }

    [TestMethod]
    /// <summary>
    /// Test for FallbackMethods_DoNotThrow.
    /// </summary>
    public void FallbackMethods_DoNotThrow() {
        var service = new MonitorService(new FakeDesktopManager());
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires non-Windows platform to trigger fallback exceptions.");
        }
        typeof(MonitorService).GetMethod("GetWallpaperPositionFallback", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(service, null);
        typeof(MonitorService).GetMethod("SetWallpaperPositionFallback", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(service, new object?[] { DesktopWallpaperPosition.Center });
        typeof(MonitorService).GetMethod("GetBackgroundColorFallback", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(service, null);
        typeof(MonitorService).GetMethod("SetBackgroundColorFallback", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(service, new object?[] { 0u });
    }
}
