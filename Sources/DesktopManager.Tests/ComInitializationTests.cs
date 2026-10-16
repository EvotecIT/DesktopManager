using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows10.0.10240.0")]
public class ComInitializationTests {
    private sealed class ComTrackingService : MonitorService {
        internal int Initializations;
        internal int Uninitializations;
        internal ComTrackingService() : base(new FakeDesktopManager()) { }
        protected override bool InitializeCom() { Initializations++; return true; }
        protected override void UninitializeCom() { Uninitializations++; }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SetLogonWallpaper_ValidationFailure_BalancesSuccessfulComInitialization(bool returnResult) {
        var service = new ComTrackingService();
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        Assert.ThrowsExactly<FileNotFoundException>(() => {
            if (returnResult) {
                service.SetLogonWallpaperWithResult(missing);
            } else {
                service.SetLogonWallpaper(missing);
            }
        });
        Assert.AreEqual(1, service.Initializations);
        Assert.AreEqual(1, service.Uninitializations);
    }
}
