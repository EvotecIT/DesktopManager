using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Provides native DisplayConfig interop used by monitor color management.
/// </summary>
public static partial class MonitorNativeMethods {
    internal const int DisplayConfigErrorSuccess = 0;
    internal const int DisplayConfigErrorInsufficientBuffer = 122;
    internal const int ErrorInvalidParameter = 87;
    internal const int ErrorNotSupported = 50;
    internal const uint QdcOnlyActivePaths = 0x00000002;

    [DllImport("user32.dll")]
    internal static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    internal static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] DisplayConfigPathInfo[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DisplayConfigModeInfo[] modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo", CharSet = CharSet.Unicode)]
    internal static extern int DisplayConfigGetSourceDeviceName(ref DisplayConfigSourceDeviceName requestPacket);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    internal static extern int DisplayConfigGetAdvancedColorInfo(ref DisplayConfigGetAdvancedColorInfo requestPacket);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    internal static extern int DisplayConfigGetAdvancedColorInfo2(ref DisplayConfigGetAdvancedColorInfo2 requestPacket);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    internal static extern int DisplayConfigGetSdrWhiteLevel(ref DisplayConfigSdrWhiteLevel requestPacket);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigSetDeviceInfo")]
    internal static extern int DisplayConfigSetAdvancedColorState(ref DisplayConfigSetAdvancedColorState requestPacket);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigSetDeviceInfo")]
    internal static extern int DisplayConfigSetHdrState(ref DisplayConfigSetHdrState requestPacket);
}

[StructLayout(LayoutKind.Sequential)]
internal struct Luid {
    public uint LowPart;
    public int HighPart;
}

internal enum DisplayConfigDeviceInfoType : uint {
    GetSourceName = 1,
    GetAdvancedColorInfo = 9,
    SetAdvancedColorState = 10,
    GetSdrWhiteLevel = 11,
    GetAdvancedColorInfo2 = 15,
    SetHdrState = 16
}

internal enum DisplayConfigColorEncoding : uint {
    Rgb = 0,
    YCbCr444 = 1,
    YCbCr422 = 2,
    YCbCr420 = 3,
    Intensity = 4
}

internal enum DisplayConfigAdvancedColorMode : uint {
    Sdr = 0,
    Wcg = 1,
    Hdr = 2
}

internal enum DisplayConfigVideoOutputTechnology : uint {
    Other = 0xFFFFFFFF
}

internal enum DisplayConfigRotation : uint {
    Identity = 1
}

internal enum DisplayConfigScaling : uint {
    Identity = 1
}

internal enum DisplayConfigScanLineOrdering : uint {
    Unspecified = 0
}

internal enum DisplayConfigPixelFormat : uint {
    EightBpp = 1,
    SixteenBpp = 2,
    TwentyFourBpp = 3,
    ThirtyTwoBpp = 4,
    NonGdi = 5
}

internal enum DisplayConfigModeInfoType : uint {
    Source = 1,
    Target = 2,
    DesktopImage = 3
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigDeviceInfoHeader {
    public DisplayConfigDeviceInfoType Type;
    public uint Size;
    public Luid AdapterId;
    public uint Id;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DisplayConfigSourceDeviceName {
    public DisplayConfigDeviceInfoHeader Header;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string ViewGdiDeviceName;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigGetAdvancedColorInfo {
    public DisplayConfigDeviceInfoHeader Header;
    public uint Value;
    public DisplayConfigColorEncoding ColorEncoding;
    public uint BitsPerColorChannel;

    public bool AdvancedColorSupported => (Value & 0x1) != 0;
    public bool AdvancedColorEnabled => (Value & 0x2) != 0;
    public bool WideColorEnforced => (Value & 0x4) != 0;
    public bool AdvancedColorForceDisabled => (Value & 0x8) != 0;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigGetAdvancedColorInfo2 {
    public DisplayConfigDeviceInfoHeader Header;
    public uint Value;
    public DisplayConfigColorEncoding ColorEncoding;
    public uint BitsPerColorChannel;
    public DisplayConfigAdvancedColorMode ActiveColorMode;

    public bool AdvancedColorSupported => (Value & 0x1) != 0;
    public bool AdvancedColorActive => (Value & 0x2) != 0;
    public bool AdvancedColorLimitedByPolicy => (Value & 0x8) != 0;
    public bool HighDynamicRangeSupported => (Value & 0x10) != 0;
    public bool HighDynamicRangeUserEnabled => (Value & 0x20) != 0;
    public bool WideColorSupported => (Value & 0x40) != 0;
    public bool WideColorUserEnabled => (Value & 0x80) != 0;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigSetAdvancedColorState {
    public DisplayConfigDeviceInfoHeader Header;
    public uint Value;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigSetHdrState {
    public DisplayConfigDeviceInfoHeader Header;
    public uint Value;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigSdrWhiteLevel {
    public DisplayConfigDeviceInfoHeader Header;
    public uint SdrWhiteLevel;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathSourceInfo {
    public Luid AdapterId;
    public uint Id;
    public uint ModeInfoIdx;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigRational {
    public uint Numerator;
    public uint Denominator;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfig2DRegion {
    public uint Width;
    public uint Height;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigVideoSignalInfo {
    public ulong PixelRate;
    public DisplayConfigRational HSyncFreq;
    public DisplayConfigRational VSyncFreq;
    public DisplayConfig2DRegion ActiveSize;
    public DisplayConfig2DRegion TotalSize;
    public uint VideoStandard;
    public DisplayConfigScanLineOrdering ScanLineOrdering;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathTargetInfo {
    public Luid AdapterId;
    public uint Id;
    public uint ModeInfoIdx;
    public DisplayConfigVideoOutputTechnology OutputTechnology;
    public DisplayConfigRotation Rotation;
    public DisplayConfigScaling Scaling;
    public DisplayConfigRational RefreshRate;
    public DisplayConfigScanLineOrdering ScanLineOrdering;
    public int TargetAvailable;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathInfo {
    public DisplayConfigPathSourceInfo SourceInfo;
    public DisplayConfigPathTargetInfo TargetInfo;
    public uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigSourceMode {
    public uint Width;
    public uint Height;
    public DisplayConfigPixelFormat PixelFormat;
    public POINTL Position;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigTargetMode {
    public DisplayConfigVideoSignalInfo TargetVideoSignalInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigDesktopImageInfo {
    public POINTL PathSourceSize;
    public RECT DesktopImageRegion;
    public RECT DesktopImageClip;
}

[StructLayout(LayoutKind.Explicit)]
internal struct DisplayConfigModeInfoUnion {
    [FieldOffset(0)]
    public DisplayConfigTargetMode TargetMode;

    [FieldOffset(0)]
    public DisplayConfigSourceMode SourceMode;

    [FieldOffset(0)]
    public DisplayConfigDesktopImageInfo DesktopImageInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigModeInfo {
    public DisplayConfigModeInfoType InfoType;
    public uint Id;
    public Luid AdapterId;
    public DisplayConfigModeInfoUnion ModeInfo;
}
