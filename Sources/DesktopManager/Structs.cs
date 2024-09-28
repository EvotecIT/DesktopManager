namespace DesktopManager;

[StructLayout(LayoutKind.Sequential)]
public struct RECT {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
public struct MonitorInfo {
    public int cbSize;
    public MonitorBounds rcMonitor;
    public MonitorBounds rcWork;
    public uint dwFlags;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINTL {
    public int x;
    public int y;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct DISPLAY_DEVICE {
    [MarshalAs(UnmanagedType.U4)]
    public int cb;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceString;
    [MarshalAs(UnmanagedType.U4)]
    public DisplayDeviceStateFlags StateFlags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceKey;
}

[Flags]
public enum DisplayDeviceStateFlags : int {
    AttachedToDesktop = 0x1,
    MultiDriver = 0x2,
    PrimaryDevice = 0x4,
    MirroringDriver = 0x8,
    VGACompatible = 0x10,
    Removable = 0x20,
    ModesPruned = 0x8000000,
    Remote = 0x4000000,
    Disconnect = 0x2000000
}

[Flags()]
public enum ChangeDisplaySettingsFlags : uint {
    CDS_NONE = 0,
    CDS_UPDATEREGISTRY = 0x00000001,
    CDS_TEST = 0x00000002,
    CDS_FULLSCREEN = 0x00000004,
    CDS_GLOBAL = 0x00000008,
    CDS_SET_PRIMARY = 0x00000010,
    CDS_VIDEOPARAMETERS = 0x00000020,
    CDS_ENABLE_UNSAFE_MODES = 0x00000100,
    CDS_DISABLE_UNSAFE_MODES = 0x00000200,
    CDS_RESET = 0x40000000,
    CDS_RESET_EX = 0x20000000,
    CDS_NORESET = 0x10000000
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct DEVMODE {
    private const int CCHDEVICENAME = 32;
    private const int CCHFORMNAME = 32;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
    public string dmDeviceName;
    public short dmSpecVersion;
    public short dmDriverVersion;
    public short dmSize;
    public short dmDriverExtra;
    public int dmFields;
    public int dmPositionX;
    public int dmPositionY;
    public int dmDisplayOrientation;
    public int dmDisplayFixedOutput;
    public short dmColor;
    public short dmDuplex;
    public short dmYResolution;
    public short dmTTOption;
    public short dmCollate;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
    public string dmFormName;
    public short dmLogPixels;
    public short dmBitsPerPel;
    public int dmPelsWidth;
    public int dmPelsHeight;
    public int dmDisplayFlags;
    public int dmDisplayFrequency;
    public int dmICMMethod;
    public int dmICMIntent;
    public int dmMediaType;
    public int dmDitherType;
    public int dmReserved1;
    public int dmReserved2;
    public int dmPanningWidth;
    public int dmPanningHeight;
}