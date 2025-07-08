using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows")]
/// <summary>Tests for monitor connection events.</summary>
public class MonitorWatcherDeviceChangeTests {
    [TestMethod]
    /// <summary>Verify MonitorConnected is raised.</summary>
    public void MonitorConnected_EventRaised() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        using var watcher = new MonitorWatcher();
        bool raised = false;
        watcher.MonitorConnected += (_, _) => raised = true;
        MonitorNativeMethods.DEV_BROADCAST_DEVICEINTERFACE data = new() {
            dbcc_size = (uint)Marshal.SizeOf<MonitorNativeMethods.DEV_BROADCAST_DEVICEINTERFACE>(),
            dbcc_devicetype = MonitorNativeMethods.DBT_DEVTYP_DEVICEINTERFACE,
            dbcc_classguid = MonitorNativeMethods.GUID_DEVINTERFACE_MONITOR
        };
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
        try {
            Marshal.StructureToPtr(data, ptr, false);
            watcher.ProcessDeviceChange(ptr, true);
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
        Assert.IsTrue(raised);
    }

    [TestMethod]
    /// <summary>Verify MonitorDisconnected is raised.</summary>
    public void MonitorDisconnected_EventRaised() {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows()) {
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#endif
            Assert.Inconclusive("Test requires Windows");
        }

        using var watcher = new MonitorWatcher();
        bool raised = false;
        watcher.MonitorDisconnected += (_, _) => raised = true;
        MonitorNativeMethods.DEV_BROADCAST_DEVICEINTERFACE data = new() {
            dbcc_size = (uint)Marshal.SizeOf<MonitorNativeMethods.DEV_BROADCAST_DEVICEINTERFACE>(),
            dbcc_devicetype = MonitorNativeMethods.DBT_DEVTYP_DEVICEINTERFACE,
            dbcc_classguid = MonitorNativeMethods.GUID_DEVINTERFACE_MONITOR
        };
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
        try {
            Marshal.StructureToPtr(data, ptr, false);
            watcher.ProcessDeviceChange(ptr, false);
        } finally {
            Marshal.FreeHGlobal(ptr);
        }
        Assert.IsTrue(raised);
    }
}
