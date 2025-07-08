using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for the <see cref="WindowKeepAlive"/> service.
/// </summary>
public class WindowKeepAliveTests {
    [TestMethod]
    /// <summary>
    /// Ensure StopAll does not throw when timers are active.
    /// </summary>
    public void StopAll_DoesNotThrow() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        var keepAlive = WindowKeepAlive.Instance;
        keepAlive.Start(new IntPtr(1), TimeSpan.FromMilliseconds(10));
        keepAlive.Start(new IntPtr(2), TimeSpan.FromMilliseconds(10));

        Thread.Sleep(50);

        try {
            keepAlive.StopAll();
        } finally {
            keepAlive.Dispose();
        }

        Assert.IsTrue(true);
    }
}
