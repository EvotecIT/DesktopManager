using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
public class LogonWallpaperTests {
    private sealed class FakeBackend : ILogonWallpaperBackend {
        internal Exception? ImageError;
        internal Exception? PolicyError;
        internal string? ImagePath;
        internal string? PolicyPath;
        internal string Image = "exported-image.png";
        internal string Policy = "configured-policy.jpg";

        public void SetImage(string path) {
            if (ImageError != null) { throw ImageError; }
            ImagePath = path;
        }
        public string GetImage() {
            if (ImageError != null) { throw ImageError; }
            return Image;
        }
        public void SetPolicy(string path) {
            if (PolicyError != null) { throw PolicyError; }
            PolicyPath = path;
        }
        public string GetPolicy() {
            if (PolicyError != null) { throw PolicyError; }
            return Policy;
        }
    }

    [TestMethod]
    public void SetLogonWallpaper_WinRtSuccess_ReportsPerUserAndDoesNotWritePolicy() {
        WithImage((path, backend, service) => {
            // Policy would fail even for an elevated caller. A successful per-user
            // operation must neither check nor depend on machine-policy access.
            backend.PolicyError = new UnauthorizedAccessException("policy denied");
            LogonWallpaperResult result = service.Set(path, allowPolicyFallback: true);
            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(LogonWallpaperMechanism.WindowsRuntime, result.Mechanism);
            Assert.IsNull(result.WindowsRuntimeError);
            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(Path.GetFullPath(path), backend.ImagePath);
            Assert.IsNull(backend.PolicyPath);
        });
    }

    [TestMethod]
    public void SetLogonWallpaper_PerUserFailure_DoesNotWritePolicyWithoutOptIn() {
        WithImage((path, backend, service) => {
            backend.ImageError = new COMException("WinRT denied");
            LogonWallpaperResult result = service.Set(path, allowPolicyFallback: false);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(LogonWallpaperMechanism.None, result.Mechanism);
            Assert.AreEqual("WinRT denied", result.ErrorMessage);
            Assert.AreEqual("WinRT denied", result.WindowsRuntimeError);
            Assert.IsNull(backend.PolicyPath);
        });
    }

    [TestMethod]
    public void SetLogonWallpaper_OptInPolicy_ReportsPolicyAndOriginalFailure() {
        WithImage((path, backend, service) => {
            backend.ImageError = new EntryPointNotFoundException("WinRT unavailable");
            LogonWallpaperResult result = service.Set(path, allowPolicyFallback: true);
            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(LogonWallpaperMechanism.PersonalizationPolicy, result.Mechanism);
            Assert.AreEqual("WinRT unavailable", result.WindowsRuntimeError);
            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(Path.GetFullPath(path), backend.PolicyPath);
        });
    }

    [TestMethod]
    public void SetLogonWallpaper_DeniedPolicy_ReportsFailureWithoutClaimingApplication() {
        WithImage((path, backend, service) => {
            backend.ImageError = new COMException("image denied");
            backend.PolicyError = new UnauthorizedAccessException("policy denied");
            LogonWallpaperResult result = service.Set(path, allowPolicyFallback: true);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(LogonWallpaperMechanism.None, result.Mechanism);
            Assert.AreEqual("image denied", result.WindowsRuntimeError);
            Assert.AreEqual("policy denied", result.ErrorMessage);
        });
    }

    [TestMethod]
    public void SetLogonWallpaper_InvalidInput_FailsBeforeEitherNativeWrite() {
        var backend = new FakeBackend();
        var service = new LogonWallpaperService(backend);
        Assert.ThrowsExactly<ArgumentNullException>(() => service.Set(" ", true));
        Assert.ThrowsExactly<FileNotFoundException>(() => service.Set(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png"), true));
        Assert.IsNull(backend.ImagePath);
        Assert.IsNull(backend.PolicyPath);
    }

    [TestMethod]
    public void GetLogonWallpaper_ReturnsImageOrPolicyOrEmptyOnFailure() {
        var backend = new FakeBackend();
        var service = new LogonWallpaperService(backend);
        Assert.AreEqual(backend.Image, service.Get());
        backend.ImageError = new COMException("image unavailable");
        Assert.AreEqual(backend.Policy, service.Get());
        backend.PolicyError = new UnauthorizedAccessException("policy unavailable");
        Assert.AreEqual(string.Empty, service.Get());
    }

    private static void WithImage(Action<string, FakeBackend, LogonWallpaperService> test) {
        string path = Path.GetTempFileName();
        try {
            var backend = new FakeBackend();
            test(path, backend, new LogonWallpaperService(backend));
        } finally {
            File.Delete(path);
        }
    }
}
