using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    [TestMethod]
    /// <summary>
    /// Verify start and stop can run concurrently without leaving timers active.
    /// </summary>
    public void StartStop_Concurrent_IsActiveReflectsState() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        var keepAlive = WindowKeepAlive.Instance;
        var handle = new IntPtr(3);
        var tasks = new List<Task>();

        for (int i = 0; i < 20; i++) {
            tasks.Add(Task.Run(() => keepAlive.Start(handle, TimeSpan.FromMilliseconds(10))));
            tasks.Add(Task.Run(() => keepAlive.Stop(handle)));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.IsFalse(keepAlive.IsActive(handle));
        keepAlive.Dispose();
    }
}
