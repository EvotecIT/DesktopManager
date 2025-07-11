using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for COM initialization during logon wallpaper operations.
/// </summary>
public class ComInitializationTests {
    private class ComTrackingService : MonitorService {
        public bool InitCalled;
        public bool UninitCalled;
        public ComTrackingService() : base(new FakeDesktopManager()) { }
        protected override bool InitializeCom() { InitCalled = true; return true; }
        protected override void UninitializeCom() { UninitCalled = true; }
    }

    [TestMethod]
    /// <summary>
    /// Ensure SetLogonWallpaper initializes and uninitializes COM.
    /// </summary>
    public void SetLogonWallpaper_InitializesCom() {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        var service = new ComTrackingService();
        try { service.SetLogonWallpaper("path"); } catch { }
        Assert.IsTrue(service.InitCalled);
        Assert.IsTrue(service.UninitCalled);
    }

    [TestMethod]
    /// <summary>
    /// Ensure GetLogonWallpaper initializes and uninitializes COM.
    /// </summary>
    public void GetLogonWallpaper_InitializesCom() {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        var service = new ComTrackingService();
        try { _ = service.GetLogonWallpaper(); } catch { }
        Assert.IsTrue(service.InitCalled);
        Assert.IsTrue(service.UninitCalled);
    }
}

