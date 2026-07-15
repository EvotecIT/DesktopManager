using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
public class MonitorEnumerationMappingTests {
    [TestMethod]
    public void ResolveDisplayPathForMonitor_PrefersExactDeviceIdMatchOverOrdinalFallback() {
        List<MonitorService.DisplayPathSnapshot> displayPaths = new List<MonitorService.DisplayPathSnapshot> {
            new MonitorService.DisplayPathSnapshot {
                AdapterDeviceName = @"\\.\DISPLAY1",
                AdapterStateFlags = DisplayDeviceStateFlags.AttachedToDesktop,
                MonitorDeviceId = @"\\?\DISPLAY#WRONG"
            },
            new MonitorService.DisplayPathSnapshot {
                AdapterDeviceName = @"\\.\DISPLAY4",
                AdapterStateFlags = DisplayDeviceStateFlags.AttachedToDesktop,
                MonitorDeviceId = @"\\?\DISPLAY#TARGET"
            }
        };

        RECT bounds = new RECT {
            Left = 3000,
            Top = 0,
            Right = 6000,
            Bottom = 2160
        };

        MonitorService.DisplayPathSnapshot? resolved = MonitorService.ResolveDisplayPathForMonitor(
            displayPaths,
            @"\\?\DISPLAY#TARGET",
            bounds,
            fallbackIndex: 0);

        Assert.IsNotNull(resolved);
        Assert.AreEqual(@"\\.\DISPLAY4", resolved.AdapterDeviceName);
    }

    [TestMethod]
    public void ResolveDisplayPathForMonitor_FallsBackToAttachedDesktopBoundsWhenDeviceIdIsUnknown() {
        List<MonitorService.DisplayPathSnapshot> displayPaths = new List<MonitorService.DisplayPathSnapshot> {
            new MonitorService.DisplayPathSnapshot {
                AdapterDeviceName = @"\\.\DISPLAY1",
                AdapterStateFlags = DisplayDeviceStateFlags.AttachedToDesktop,
                AdapterBounds = new RECT {
                    Left = 0,
                    Top = 0,
                    Right = 1920,
                    Bottom = 1080
                }
            },
            new MonitorService.DisplayPathSnapshot {
                AdapterDeviceName = @"\\.\DISPLAY2",
                AdapterStateFlags = DisplayDeviceStateFlags.AttachedToDesktop,
                AdapterBounds = new RECT {
                    Left = 1920,
                    Top = 0,
                    Right = 3840,
                    Bottom = 1080
                }
            }
        };

        RECT bounds = new RECT {
            Left = 1920,
            Top = 0,
            Right = 3840,
            Bottom = 1080
        };

        MonitorService.DisplayPathSnapshot? resolved = MonitorService.ResolveDisplayPathForMonitor(
            displayPaths,
            @"\\?\DISPLAY#MISSING",
            bounds,
            fallbackIndex: 0);

        Assert.IsNotNull(resolved);
        Assert.AreEqual(@"\\.\DISPLAY2", resolved.AdapterDeviceName);
    }

    [TestMethod]
    public void ResolveDisplaySourceName_FallbackEnumerationPrefersAdapterName() {
        string sourceName = MonitorService.ResolveDisplaySourceName(
            @"\\.\DISPLAY2",
            @"\\.\DISPLAY2\Monitor0");

        Assert.AreEqual(@"\\.\DISPLAY2", sourceName);
    }

    [TestMethod]
    public void ResolveDisplaySourceName_MissingAdapterUsesMonitorName() {
        string sourceName = MonitorService.ResolveDisplaySourceName(
            string.Empty,
            @"\\.\DISPLAY2\Monitor0");

        Assert.AreEqual(@"\\.\DISPLAY2\Monitor0", sourceName);
    }
}
