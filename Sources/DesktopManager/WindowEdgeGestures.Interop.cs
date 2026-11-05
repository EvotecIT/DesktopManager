using System;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace DesktopManager;

[StructLayout(LayoutKind.Sequential)]
internal struct WindowPropertyKey {
    internal Guid FormatId;
    internal uint PropertyId;
}

// A VT_BOOL PROPVARIANT. PROPVARIANT is 16 bytes on x86 and 24 on x64/ARM64; the trailing pointer keeps that size.
[StructLayout(LayoutKind.Sequential)]
internal struct WindowPropertyBoolValue {
    internal ushort Type;
    private ushort _reserved1, _reserved2, _reserved3;
    internal int Value;
    private IntPtr _padding;
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
internal partial interface IWindowPropertyStore {
    [PreserveSig] int GetCount(out uint count);
    [PreserveSig] int GetAt(uint index, out WindowPropertyKey key);
    [PreserveSig] int GetValue(in WindowPropertyKey key, IntPtr value);
    [PreserveSig] int SetValue(in WindowPropertyKey key, in WindowPropertyBoolValue value);
    [PreserveSig] int Commit();
}
