namespace DesktopManager;

/// <summary>
/// Provides native methods for interacting with display devices and settings.
/// </summary>
public static class MonitorNativeMethods {
    /// <summary>
    /// Enumerates the display settings for a specified display device.
    /// </summary>
    /// <param name="lpszDeviceName">The name of the display device.</param>
    /// <param name="iModeNum">The type of information to retrieve.</param>
    /// <param name="lpDevMode">A pointer to a <see cref="DEVMODE"/> structure that receives the information.</param>
    /// <returns>True if the function succeeds; otherwise, false.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    /// <summary>
    /// Enumerates the display devices that are currently available.
    /// </summary>
    /// <param name="lpDevice">The device name.</param>
    /// <param name="iDevNum">The index of the display device.</param>
    /// <param name="lpDisplayDevice">A pointer to a <see cref="DISPLAY_DEVICE"/> structure that receives the information.</param>
    /// <param name="dwFlags">Set this flag to 0.</param>
    /// <returns>True if the function succeeds; otherwise, false.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    /// <summary>
    /// Changes the settings of the specified display device to the specified graphics mode.
    /// </summary>
    /// <param name="lpszDeviceName">The name of the display device.</param>
    /// <param name="lpDevMode">A pointer to a <see cref="DEVMODE"/> structure that describes the new graphics mode.</param>
    /// <param name="hwnd">Reserved; must be IntPtr.Zero.</param>
    /// <param name="dwflags">Indicates how the graphics mode should be changed.</param>
    /// <param name="lParam">Reserved; must be IntPtr.Zero.</param>
    /// <returns>A <see cref="DisplayChangeConfirmation"/> value indicating the result of the operation.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern DisplayChangeConfirmation ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, ChangeDisplaySettingsFlags dwflags, IntPtr lParam);

    /// <summary>
    /// Changes the settings of the specified display device to the specified graphics mode.
    /// </summary>
    /// <param name="lpszDeviceName">The name of the display device.</param>
    /// <param name="lpDevMode">A pointer to a <see cref="DEVMODE"/> structure that describes the new graphics mode, or IntPtr.Zero.</param>
    /// <param name="hwnd">Reserved; must be IntPtr.Zero.</param>
    /// <param name="dwflags">Indicates how the graphics mode should be changed.</param>
    /// <param name="lParam">Reserved; must be IntPtr.Zero.</param>
    /// <returns>A <see cref="DisplayChangeConfirmation"/> value indicating the result of the operation.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    // A signature for ChangeDisplaySettingsEx with a DEVMODE struct as the second parameter won't allow you to pass in IntPtr.Zero, so create an overload
    public static extern DisplayChangeConfirmation ChangeDisplaySettingsEx(string lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, ChangeDisplaySettingsFlags dwflags, IntPtr lParam);
}
