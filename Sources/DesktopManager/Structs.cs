using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Represents a rectangle defined by the coordinates of its upper-left and lower-right corners.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RECT {
    /// <summary>
    /// The x-coordinate of the upper-left corner of the rectangle.
    /// </summary>
    public int Left;
    /// <summary>
    /// The y-coordinate of the upper-left corner of the rectangle.
    /// </summary>
    public int Top;
    /// <summary>
    /// The x-coordinate of the lower-right corner of the rectangle.
    /// </summary>
    public int Right;
    /// <summary>
    /// The y-coordinate of the lower-right corner of the rectangle.
    /// </summary>
    public int Bottom;
}

/// <summary>
/// Contains information about a display monitor.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MonitorInfo {
    /// <summary>
    /// The size of the structure, in bytes.
    /// </summary>
    public int cbSize;
    /// <summary>
    /// A RECT structure that specifies the display monitor rectangle.
    /// </summary>
    public MonitorBounds rcMonitor;
    /// <summary>
    /// A RECT structure that specifies the work area rectangle of the display monitor.
    /// </summary>
    public MonitorBounds rcWork;
    /// <summary>
    /// The attributes of the display monitor.
    /// </summary>
    public uint dwFlags;
}

/// <summary>
/// Extends <see cref="MonitorInfo"/> with the device name.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct MONITORINFOEX {
    /// <summary>
    /// The size of the structure, in bytes.
    /// </summary>
    public int cbSize;
    /// <summary>
    /// The display monitor rectangle.
    /// </summary>
    public RECT rcMonitor;
    /// <summary>
    /// The work area rectangle of the display monitor.
    /// </summary>
    public RECT rcWork;
    /// <summary>
    /// The attributes of the display monitor.
    /// </summary>
    public uint dwFlags;
    /// <summary>
    /// The device name.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szDevice;
}

/// <summary>
/// Represents a point with x and y coordinates.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct POINTL {
    /// <summary>
    /// The x-coordinate of the point.
    /// </summary>
    public int x;
    /// <summary>
    /// The y-coordinate of the point.
    /// </summary>
    public int y;
}

/// <summary>
/// Contains information about a display device.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct DISPLAY_DEVICE {
    /// <summary>
    /// The size of the structure, in bytes.
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public int cb;
    /// <summary>
    /// The name of the device.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;
    /// <summary>
    /// The string that describes the device.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceString;
    /// <summary>
    /// The state of the device.
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public DisplayDeviceStateFlags StateFlags;
    /// <summary>
    /// The device ID.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceID;
    /// <summary>
    /// The device key.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceKey;
}

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

/// <summary>
/// Specifies flags for changing display settings.
/// </summary>
[Flags]
public enum ChangeDisplaySettingsFlags : uint {
    /// <summary>
    /// No flags.
    /// </summary>
    CDS_NONE = 0,
    /// <summary>
    /// Update the registry with the new settings.
    /// </summary>
    CDS_UPDATEREGISTRY = 0x00000001,
    /// <summary>
    /// Test the new settings.
    /// </summary>
    CDS_TEST = 0x00000002,
    /// <summary>
    /// The settings are for a full-screen application.
    /// </summary>
    CDS_FULLSCREEN = 0x00000004,
    /// <summary>
    /// The settings are global.
    /// </summary>
    CDS_GLOBAL = 0x00000008,
    /// <summary>
    /// Set the primary display device.
    /// </summary>
    CDS_SET_PRIMARY = 0x00000010,
    /// <summary>
    /// The settings are for video parameters.
    /// </summary>
    CDS_VIDEOPARAMETERS = 0x00000020,
    /// <summary>
    /// Enable unsafe modes.
    /// </summary>
    CDS_ENABLE_UNSAFE_MODES = 0x00000100,
    /// <summary>
    /// Disable unsafe modes.
    /// </summary>
    CDS_DISABLE_UNSAFE_MODES = 0x00000200,
    /// <summary>
    /// Reset the settings.
    /// </summary>
    CDS_RESET = 0x40000000,
    /// <summary>
    /// Reset the settings (extended).
    /// </summary>
    CDS_RESET_EX = 0x20000000,
    /// <summary>
    /// Do not reset the settings.
    /// </summary>
    CDS_NORESET = 0x10000000
}

/// <summary>
/// Contains information about the initialization and environment of a printer or a display device.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct DEVMODE {
    private const int CCHDEVICENAME = 32;
    private const int CCHFORMNAME = 32;
    /// <summary>
    /// The name of the device.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
    public string dmDeviceName;
    /// <summary>
    /// The version number of the initialization data specification.
    /// </summary>
    public short dmSpecVersion;
    /// <summary>
    /// The version number of the printer or display driver.
    /// </summary>
    public short dmDriverVersion;
    /// <summary>
    /// The size of the DEVMODE structure, in bytes.
    /// </summary>
    public short dmSize;
    /// <summary>
    /// The number of bytes of private driver data that follow this structure.
    /// </summary>
    public short dmDriverExtra;
    /// <summary>
    /// A bit field that specifies which members of the DEVMODE structure have been initialized.
    /// </summary>
    public int dmFields;
    /// <summary>
    /// The x-coordinate of the display position.
    /// </summary>
    public int dmPositionX;
    /// <summary>
    /// The y-coordinate of the display position.
    /// </summary>
    public int dmPositionY;
    /// <summary>
    /// The orientation of the display.
    /// </summary>
    public int dmDisplayOrientation;
    /// <summary>
    /// The fixed output setting of the display.
    /// </summary>
    public int dmDisplayFixedOutput;
    /// <summary>
    /// The color setting of the display.
    /// </summary>
    public short dmColor;
    /// <summary>
    /// The duplex setting of the printer.
    /// </summary>
    public short dmDuplex;
    /// <summary>
    /// The y-resolution of the printer.
    /// </summary>
    public short dmYResolution;
    /// <summary>
    /// The TrueType option of the printer.
    /// </summary>
    public short dmTTOption;
    /// <summary>
    /// The collation setting of the printer.
    /// </summary>
    public short dmCollate;
    /// <summary>
    /// The form name of the printer.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
    public string dmFormName;
    /// <summary>
    /// The log pixels setting of the display.
    /// </summary>
    public short dmLogPixels;
    /// <summary>
    /// The bits per pixel setting of the display.
    /// </summary>
    public short dmBitsPerPel;
    /// <summary>
    /// The width of the display, in pixels.
    /// </summary>
    public int dmPelsWidth;
    /// <summary>
    /// The height of the display, in pixels.
    /// </summary>
    public int dmPelsHeight;
    /// <summary>
    /// The display flags.
    /// </summary>
    public int dmDisplayFlags;
    /// <summary>
    /// The display frequency.
    /// </summary>
    public int dmDisplayFrequency;
    /// <summary>
    /// The ICM method.
    /// </summary>
    public int dmICMMethod;
    /// <summary>
    /// The ICM intent.
    /// </summary>
    public int dmICMIntent;
    /// <summary>
    /// The media type.
    /// </summary>
    public int dmMediaType;
    /// <summary>
    /// The dither type.
    /// </summary>
    public int dmDitherType;
    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public int dmReserved1;
    /// <summary>
    /// Reserved; must be zero.
    /// </summary>
    public int dmReserved2;
    /// <summary>
    /// The width of the panning area.
    /// </summary>
    public int dmPanningWidth;
    /// <summary>
    /// The height of the panning area.
    /// </summary>
    public int dmPanningHeight;
}
