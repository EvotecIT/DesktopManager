using System;

namespace DesktopManager;

/// <summary>
/// Describes the best available stable identity for a monitor across Windows display reordering.
/// </summary>
public sealed class MonitorIdentity {
    private MonitorIdentity(
        string stableKey,
        string source,
        string deviceId,
        string deviceName,
        string deviceString,
        string deviceKey,
        string? manufacturer,
        string? serialNumber) {
        StableKey = stableKey;
        Source = source;
        DeviceId = deviceId;
        DeviceName = deviceName;
        DeviceString = deviceString;
        DeviceKey = deviceKey;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
    }

    /// <summary>
    /// Gets the key that should be used when saving monitor-specific profile and layout preferences.
    /// </summary>
    public string StableKey { get; }

    /// <summary>
    /// Gets the identity source used to build <see cref="StableKey"/>.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the Windows monitor device interface path when available.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// Gets the display adapter name, such as \\.\DISPLAY1.
    /// </summary>
    public string DeviceName { get; }

    /// <summary>
    /// Gets the monitor or adapter display string.
    /// </summary>
    public string DeviceString { get; }

    /// <summary>
    /// Gets the registry device key when available.
    /// </summary>
    public string DeviceKey { get; }

    /// <summary>
    /// Gets the EDID manufacturer code when available.
    /// </summary>
    public string? Manufacturer { get; }

    /// <summary>
    /// Gets the EDID serial number when available.
    /// </summary>
    public string? SerialNumber { get; }

    /// <summary>
    /// Creates the most stable identity available for a monitor snapshot.
    /// </summary>
    /// <param name="monitor">The monitor snapshot.</param>
    /// <returns>The best available monitor identity.</returns>
    public static MonitorIdentity FromMonitor(Monitor monitor) {
        if (monitor == null) {
            throw new ArgumentNullException(nameof(monitor));
        }

        string? manufacturer = monitor.Manufacturer;
        string? serialNumber = monitor.SerialNumber;
        if (!string.IsNullOrWhiteSpace(manufacturer) &&
            !string.IsNullOrWhiteSpace(serialNumber)) {
            string normalizedManufacturer = Normalize(manufacturer!);
            string normalizedSerialNumber = Normalize(serialNumber!);
            return new MonitorIdentity(
                $"edid:{normalizedManufacturer}:{normalizedSerialNumber}",
                "edid",
                monitor.DeviceId,
                monitor.DeviceName,
                monitor.DeviceString,
                monitor.DeviceKey,
                manufacturer,
                serialNumber);
        }

        if (!string.IsNullOrWhiteSpace(monitor.DeviceId)) {
            return new MonitorIdentity(
                $"device-id:{Normalize(monitor.DeviceId)}",
                "device-id",
                monitor.DeviceId,
                monitor.DeviceName,
                monitor.DeviceString,
                monitor.DeviceKey,
                monitor.Manufacturer,
                monitor.SerialNumber);
        }

        if (!string.IsNullOrWhiteSpace(monitor.DeviceName)) {
            return new MonitorIdentity(
                $"device-name:{Normalize(monitor.DeviceName)}",
                "device-name",
                monitor.DeviceId,
                monitor.DeviceName,
                monitor.DeviceString,
                monitor.DeviceKey,
                monitor.Manufacturer,
                monitor.SerialNumber);
        }

        MonitorPosition position = monitor.Position;
        return new MonitorIdentity(
            $"geometry:{position.Left},{position.Top},{position.Right},{position.Bottom}",
            "geometry",
            monitor.DeviceId,
            monitor.DeviceName,
            monitor.DeviceString,
            monitor.DeviceKey,
            monitor.Manufacturer,
            monitor.SerialNumber);
    }

    private static string Normalize(string value) {
        return value.Trim().ToUpperInvariant();
    }
}
