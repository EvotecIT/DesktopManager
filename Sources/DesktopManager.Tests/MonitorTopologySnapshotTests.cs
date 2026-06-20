using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests stable monitor identity and topology mapping.
/// </summary>
public class MonitorTopologySnapshotTests {
    [TestMethod]
    /// <summary>
    /// EDID information is the preferred identity because it survives display index changes.
    /// </summary>
    public void MonitorIdentity_FromMonitor_PrefersEdidSerialOverDeviceId() {
        Monitor monitor = CreateMonitor(7, -100, 0, 1820, 1080);
        typeof(Monitor).GetProperty(nameof(Monitor.Manufacturer))!.SetValue(monitor, "DEL");
        typeof(Monitor).GetProperty(nameof(Monitor.SerialNumber))!.SetValue(monitor, "ABC123");

        MonitorIdentity identity = MonitorIdentity.FromMonitor(monitor);

        Assert.AreEqual("edid:DEL:ABC123", identity.StableKey);
        Assert.AreEqual("edid", identity.Source);
    }

    [TestMethod]
    /// <summary>
    /// Device ID is the preferred fallback when EDID serial information is unavailable.
    /// </summary>
    public void MonitorIdentity_FromMonitor_UsesDeviceIdWhenEdidMissing() {
        Monitor monitor = CreateMonitor(2, 0, 0, 1920, 1080);

        MonitorIdentity identity = MonitorIdentity.FromMonitor(monitor);

        Assert.AreEqual("device-id:DISPLAY2", identity.StableKey);
        Assert.AreEqual("device-id", identity.Source);
    }

    [TestMethod]
    /// <summary>
    /// Topology uses visual rows and columns while skipping disconnected placeholder monitors.
    /// </summary>
    public void FromMonitors_TwoByTwoLayout_MapsVisualTopologyAndSkipsDisconnectedPlaceholders() {
        Monitor topRight = CreateMonitor(0, 0, 0, 3840, 2160);
        Monitor topLeft = CreateMonitor(1, -3840, 20, 0, 2180);
        Monitor bottomRight = CreateMonitor(2, 0, 2169, 3840, 4329);
        Monitor bottomLeft = CreateMonitor(3, -3853, 2180, -13, 4340);
        Monitor placeholder = CreateMonitor(4, 0, 0, 0, 0, connected: false, deviceId: string.Empty);

        MonitorTopologySnapshot topology = MonitorTopologySnapshot.FromMonitors(new[] {
            topRight,
            topLeft,
            bottomRight,
            bottomLeft,
            placeholder
        });

        Assert.AreEqual(4, topology.Items.Count);
        AssertTopology(topology.Items[0], 1, 0, 0, "Top Left");
        AssertTopology(topology.Items[1], 0, 0, 1, "Top Right");
        AssertTopology(topology.Items[2], 3, 1, 0, "Bottom Left");
        AssertTopology(topology.Items[3], 2, 1, 1, "Bottom Right");
    }

    [TestMethod]
    /// <summary>
    /// Row grouping accepts side-by-side displays whose taskbars or DPI offsets create small Y differences.
    /// </summary>
    public void GroupRows_SideBySideOffsetDisplays_KeepsVisualRow() {
        Monitor left = CreateMonitor(1, -3840, 20, 0, 2180);
        Monitor right = CreateMonitor(0, 0, 0, 3840, 2160);

        IReadOnlyList<IReadOnlyList<Monitor>> rows = MonitorTopologySnapshot.GroupRows(new[] { left, right });

        Assert.AreEqual(1, rows.Count);
        CollectionAssert.AreEqual(new[] { 1, 0 }, rows[0].Select(monitor => monitor.Index).ToArray());
    }

    private static void AssertTopology(MonitorTopologyItem item, int index, int row, int column, string topologyName) {
        Assert.AreEqual(index, item.Monitor.Index);
        Assert.AreEqual(row, item.Row);
        Assert.AreEqual(column, item.Column);
        Assert.AreEqual(topologyName, item.TopologyName);
        StringAssert.Contains(item.DisplayName, topologyName);
    }

    private static Monitor CreateMonitor(
        int index,
        int left,
        int top,
        int right,
        int bottom,
        bool connected = true,
        string? deviceId = null) {
        string resolvedDeviceId = deviceId ?? $"DISPLAY{index}";
        RECT rect = new() {
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom
        };

        return new Monitor(new MonitorService(new StubDesktopManager(new Dictionary<string, RECT> {
            [resolvedDeviceId] = rect
        }))) {
            Index = index,
            DeviceId = resolvedDeviceId,
            DeviceName = connected ? $"\\\\.\\DISPLAY{index}" : string.Empty,
            DeviceString = connected ? $"Display {index}" : string.Empty,
            StateFlags = connected ? DisplayDeviceStateFlags.AttachedToDesktop : 0,
            Rect = rect
        };
    }
}
