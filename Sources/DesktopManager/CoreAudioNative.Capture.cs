using System;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace DesktopManager;

// WASAPI stream interfaces, declared once for generated COM (.NET 8+) and built-in COM (net472, netstandard2.0).
#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2")]
internal partial interface IAudioClient {
    [PreserveSig] int Initialize(int shareMode, int streamFlags, long bufferDuration, long periodicity, IntPtr format, IntPtr sessionGuid);
    [PreserveSig] int GetBufferSize(out uint frames);
    [PreserveSig] int GetStreamLatency(out long latency);
    [PreserveSig] int GetCurrentPadding(out uint frames);
    [PreserveSig] int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closest);
    [PreserveSig] int GetMixFormat(out IntPtr format);
    [PreserveSig] int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);
    [PreserveSig] int Start();
    [PreserveSig] int Stop();
    [PreserveSig] int Reset();
    [PreserveSig] int SetEventHandle(IntPtr handle);
    [PreserveSig] int GetService(ref Guid interfaceId, out IntPtr service);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317")]
internal partial interface IAudioCaptureClient {
    [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong devicePosition, out ulong performanceCounterPosition);
    [PreserveSig] int ReleaseBuffer(uint frames);
    [PreserveSig] int GetNextPacketSize(out uint frames);
}

internal static class CoreAudioCaptureNative {
    internal const int SharedMode = 0;
    internal const int LoopbackStreamFlag = 0x00020000;
    internal const uint SilentBufferFlag = 0x2;
    // 100 ms in 100-nanosecond units: enough headroom for a 10 ms polling loop.
    internal const long BufferDuration = 1_000_000;
    internal static readonly Guid AudioClientInterfaceId = new("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2");
    internal static readonly Guid CaptureClientInterfaceId = new("C8ADBD64-E71E-48A0-A4DE-185C395CD317");
}
