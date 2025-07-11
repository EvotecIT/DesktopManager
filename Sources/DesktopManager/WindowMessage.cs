namespace DesktopManager;

/// <summary>
/// Window message constants.
/// </summary>
public enum WindowMessage {
    /// <summary>
    /// System command message.
    /// </summary>
    WM_SYSCOMMAND = 0x0112,

    /// <summary>
    /// Broadcast of power-management events.
    /// </summary>
    WM_POWERBROADCAST = 0x0218,

    /// <summary>
    /// Broadcast of device add or remove events.
    /// </summary>
    WM_DEVICECHANGE = 0x0219
}

