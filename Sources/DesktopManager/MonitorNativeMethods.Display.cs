using System.Text;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Provides native methods for interacting with display devices and settings.
/// </summary>
public static partial class MonitorNativeMethods {
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
    /// <param name="dwFlags">Combination of <see cref="EnumDisplayDevicesFlags"/> values.</param>
    /// <returns>True if the function succeeds; otherwise, false.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    /// <summary>
    /// Sets the process DPI awareness.
    /// </summary>
    /// <param name="value">The DPI awareness value.</param>
    /// <returns>Returns S_OK on success.</returns>
    [DllImport("shcore.dll")]
    public static extern int SetProcessDpiAwareness(ProcessDpiAwareness value);

    /// <summary>
    /// Represents a callback function that processes display monitors during enumeration.
    /// </summary>
    /// <param name="hMonitor">Handle to the display monitor.</param>
    /// <param name="hdcMonitor">Handle to a device context.</param>
    /// <param name="lprcMonitor">Pointer to a RECT structure with monitor bounds.</param>
    /// <param name="dwData">Application-defined data.</param>
    /// <returns>True to continue enumeration.</returns>
    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    /// <summary>
    /// Enumerates the display monitors.
    /// </summary>
    /// <param name="hdc">Optional device context.</param>
    /// <param name="lprcClip">Optional clipping rectangle.</param>
    /// <param name="lpfnEnum">Callback function.</param>
    /// <param name="dwData">Application-defined data.</param>
    /// <returns>True if enumeration succeeds.</returns>
    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    /// <summary>
    /// Retrieves information about a display monitor.
    /// </summary>
    /// <param name="hMonitor">Handle to the display monitor.</param>
    /// <param name="lpmi">Structure that receives monitor information.</param>
    /// <returns>True if the function succeeds.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    /// <summary>
    /// The monitor is the primary display monitor.
    /// </summary>
    public const int MONITORINFOF_PRIMARY = 0x00000001;

    /// <summary>
    /// Returns NULL if no display monitors intersect.
    /// </summary>
    public const int MONITOR_DEFAULTTONULL = 0x00000000;

    /// <summary>
    /// Returns a handle to the primary display monitor.
    /// </summary>
    public const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;

    /// <summary>
    /// Returns the nearest monitor to the passed rectangle or point.
    /// </summary>
    public const int MONITOR_DEFAULTTONEAREST = 0x00000002;

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

    /// <summary>
    /// Gets the number of physical monitors associated with an HMONITOR handle.
    /// </summary>
    /// <param name="hMonitor">The monitor handle.</param>
    /// <param name="pdwNumberOfPhysicalMonitors">Receives the number of monitors.</param>
    /// <returns><c>true</c> if the call succeeds.</returns>
    [DllImport("Dxva2.dll", SetLastError = true)]
    public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

    /// <summary>
    /// Retrieves handles to the physical monitors associated with an HMONITOR.
    /// </summary>
    /// <param name="hMonitor">The monitor handle.</param>
    /// <param name="dwPhysicalMonitorArraySize">Size of the array.</param>
    /// <param name="pPhysicalMonitorArray">The array that receives the monitor handles.</param>
    /// <returns><c>true</c> if the call succeeds.</returns>
    [DllImport("Dxva2.dll", SetLastError = true)]
    public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    /// <summary>
    /// Closes monitor handles obtained from <see cref="GetPhysicalMonitorsFromHMONITOR"/>.
    /// </summary>
    /// <param name="dwPhysicalMonitorArraySize">Size of the array.</param>
    /// <param name="pPhysicalMonitorArray">Array of monitor handles to close.</param>
    /// <returns><c>true</c> if the call succeeds.</returns>
    [DllImport("Dxva2.dll", SetLastError = true)]
    public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    /// <summary>
    /// Retrieves the monitor's brightness information.
    /// </summary>
    /// <param name="hMonitor">Handle to the physical monitor.</param>
    /// <param name="pdwMinimumBrightness">Receives the minimum brightness.</param>
    /// <param name="pdwCurrentBrightness">Receives the current brightness.</param>
    /// <param name="pdwMaximumBrightness">Receives the maximum brightness.</param>
    /// <returns><c>true</c> if the call succeeds.</returns>
    [DllImport("Dxva2.dll", SetLastError = true)]
    public static extern bool GetMonitorBrightness(IntPtr hMonitor, out uint pdwMinimumBrightness, out uint pdwCurrentBrightness, out uint pdwMaximumBrightness);

    /// <summary>
    /// Sets the monitor's brightness.
    /// </summary>
    /// <param name="hMonitor">Handle to the physical monitor.</param>
    /// <param name="dwNewBrightness">The brightness value to set.</param>
    /// <returns><c>true</c> if the call succeeds.</returns>
    [DllImport("Dxva2.dll", SetLastError = true)]
    public static extern bool SetMonitorBrightness(IntPtr hMonitor, uint dwNewBrightness);
}
