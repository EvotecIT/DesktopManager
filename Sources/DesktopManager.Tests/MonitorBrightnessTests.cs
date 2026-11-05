using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Brightness through DDC/CI, read only.
/// </summary>
public class MonitorBrightnessTests {
    [TestMethod]
    /// <summary>
    /// Every monitor that answers DDC/CI when asked directly also answers through the library, by device ID, by display name and on the monitor itself.
    /// </summary>
    public void Brightness_MonitorsThatAnswerDdcCi_ReadTheSameByDeviceIdNameAndMonitor() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        // Asked directly, without the library's lookup, so a lookup that never matches can't pass as "not supported".
        var answering = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        MonitorNativeMethods.MonitorEnumProc proc = (IntPtr handle, IntPtr hdc, ref RECT rect, IntPtr data) => {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (MonitorNativeMethods.GetMonitorInfo(handle, ref info)
                && MonitorNativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(handle, out uint count) && count > 0) {
                var physical = new PHYSICAL_MONITOR[count];
                if (MonitorNativeMethods.GetPhysicalMonitorsFromHMONITOR(handle, count, physical)) {
                    if (MonitorNativeMethods.GetMonitorBrightness(physical[0].hPhysicalMonitor, out _, out _, out _)) {
                        answering.Add(info.szDevice);
                    }
                    MonitorNativeMethods.DestroyPhysicalMonitors(count, physical);
                }
            }
            return true;
        };
        MonitorNativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
        if (answering.Count == 0) {
            Assert.Inconclusive("No monitor answers DDC/CI brightness.");
        }

        var service = new Monitors();
        var connected = service.GetMonitorsConnected().Where(monitor => answering.Contains(monitor.DeviceName)).ToList();
        Assert.HasCount(answering.Count, connected);
        foreach (Monitor monitor in connected) {
            int byName = service.GetMonitorBrightness(monitor.DeviceName);
            Assert.AreEqual(byName, service.GetMonitorBrightness(monitor.DeviceId), monitor.DeviceName);
            Assert.AreEqual(byName, monitor.GetBrightness(), monitor.DeviceName);
        }
    }
}
