namespace DesktopManager;

/// <summary>
/// Flags for <see cref="MonitorNativeMethods.EnumDisplayDevices"/>.
/// </summary>
[Flags]
public enum EnumDisplayDevicesFlags : uint {
    /// <summary>No flags.</summary>
    None = 0,
    /// <summary>
    /// Requests that <see cref="DISPLAY_DEVICE.DeviceID"/> contain the device interface name.
    /// </summary>
    EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001
}
