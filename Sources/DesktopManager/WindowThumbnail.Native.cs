using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

// DWM_THUMBNAIL_PROPERTIES and its flags. All fields are blittable, so plain DllImport stays trimming and AOT safe.
[StructLayout(LayoutKind.Sequential)]
internal struct DwmThumbnailProperties {
    internal const uint DestinationFlag = 0x1;
    internal const uint SourceFlag = 0x2;
    internal const uint OpacityFlag = 0x4;
    internal const uint VisibleFlag = 0x8;
    internal const uint SourceClientAreaOnlyFlag = 0x10;

    public uint Flags;
    public RECT Destination;
    public RECT Source;
    public byte Opacity;
    public int Visible;
    public int SourceClientAreaOnly;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DwmThumbnailSize {
    public int Width;
    public int Height;
}

internal static class DwmThumbnailNative {
    [DllImport("dwmapi.dll")]
    internal static extern int DwmRegisterThumbnail(IntPtr destination, IntPtr source, out IntPtr thumbnail);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmUnregisterThumbnail(IntPtr thumbnail);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmUpdateThumbnailProperties(IntPtr thumbnail, ref DwmThumbnailProperties properties);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmQueryThumbnailSourceSize(IntPtr thumbnail, out DwmThumbnailSize size);
}
