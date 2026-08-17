using System;
using System.Runtime.InteropServices;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
/// <summary>Tests for <see cref="HotkeyService"/>.</summary>
public class HotkeyServiceTests {
    private const string HotkeyTestMutexName = @"Local\DesktopManager.HotkeyServiceTests.HotkeyCallback_FiresOnMessage";

    [TestMethod]
    /// <summary>Callback fires when hotkey message is posted.</summary>
    public void HotkeyCallback_FiresOnMessage() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireInteractive();

        using Mutex mutex = new(false, HotkeyTestMutexName);
        bool hasHandle;
        try {
            hasHandle = mutex.WaitOne(TimeSpan.FromSeconds(10));
        } catch (AbandonedMutexException) {
            hasHandle = true;
        }

        if (!hasHandle) {
            Assert.Inconclusive("Timed out waiting for the shared hotkey test mutex.");
        }

        var service = HotkeyService.Instance;
        bool called = false;
        int id = service.RegisterHotkey(HotkeyModifiers.Control, VirtualKey.VK_F24, () => called = true);
        try {
            MonitorNativeMethods.PostMessage(service.WindowHandle, MonitorNativeMethods.WM_HOTKEY, (IntPtr)id, IntPtr.Zero);
            Thread.Sleep(50);
            Assert.IsTrue(called);
        } finally {
            service.UnregisterHotkey(id);
            mutex.ReleaseMutex();
        }
    }
}
