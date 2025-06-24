namespace DesktopManager;

/// <summary>
/// Specifies the state of a display device.
/// </summary>
[Flags]
public enum DisplayDeviceStateFlags : int {
    /// <summary>
    /// The device is part of the desktop.
    /// </summary>
    AttachedToDesktop = 0x1,
    /// <summary>
    /// The device is a multi-driver.
    /// </summary>
    MultiDriver = 0x2,
    /// <summary>
    /// The device is the primary device.
    /// </summary>
    PrimaryDevice = 0x4,
    /// <summary>
    /// The device is a mirroring driver.
    /// </summary>
    MirroringDriver = 0x8,
    /// <summary>
    /// The device is VGA compatible.
    /// </summary>
    VGACompatible = 0x10,
    /// <summary>
    /// The device is removable.
    /// </summary>
    Removable = 0x20,
    /// <summary>
    /// The device has modes pruned.
    /// </summary>
    ModesPruned = 0x8000000,
    /// <summary>
    /// The device is remote.
    /// </summary>
    Remote = 0x4000000,
    /// <summary>
    /// The device is disconnected.
    /// </summary>
    Disconnect = 0x2000000
}
