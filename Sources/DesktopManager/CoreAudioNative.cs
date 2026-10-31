using System;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace DesktopManager;

// Core Audio interfaces use source-generated COM on .NET 8+ so Native AOT and trimmed
// consumers can activate them. net472 and netstandard2.0 keep built-in COM interop.

internal enum NativeAudioDataFlow {
    Render = 0,
    Capture = 1,
    All = 2
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
internal partial interface IMMDeviceEnumerator {
    [PreserveSig]
    int EnumAudioEndpoints(NativeAudioDataFlow dataFlow, AudioEndpointState stateMask, out IMMDeviceCollection devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(NativeAudioDataFlow dataFlow, ERole role, out IMMDevice device);

    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

    [PreserveSig]
#if NET8_0_OR_GREATER
    int RegisterEndpointNotificationCallback(IntPtr client);
#else
    int RegisterEndpointNotificationCallback(IMMNotificationClient client);
#endif

    [PreserveSig]
#if NET8_0_OR_GREATER
    int UnregisterEndpointNotificationCallback(IntPtr client);
#else
    int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
#endif
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
internal partial interface IMMDeviceCollection {
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int Item(uint index, out IMMDevice device);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
internal partial interface IMMDevice {
    /// <remarks>Only <see cref="IAudioEndpointVolume"/> is activated, so the result is typed to it.</remarks>
    [PreserveSig]
    int Activate(ref Guid interfaceId, uint classContext, IntPtr activationParameters, out IAudioEndpointVolume instance);

    [PreserveSig]
    int OpenPropertyStore(uint accessMode, out IPropertyStore properties);

    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

    [PreserveSig]
    int GetState(out AudioEndpointState state);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
internal partial interface IPropertyStore {
    [PreserveSig]
    int GetCount(out uint propertyCount);

    [PreserveSig]
    int GetAt(uint propertyIndex, out PropertyKey key);

    [PreserveSig]
    int GetValue(ref PropertyKey key, out PropVariant value);

    [PreserveSig]
    int SetValue(ref PropertyKey key, ref PropVariant value);

    [PreserveSig]
    int Commit();
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
internal partial interface IAudioEndpointVolume {
    [PreserveSig]
    int RegisterControlChangeNotify(IntPtr notificationClient);
    [PreserveSig]
    int UnregisterControlChangeNotify(IntPtr notificationClient);
    [PreserveSig]
    int GetChannelCount(out uint channelCount);
    [PreserveSig]
    int SetMasterVolumeLevel(float levelDb, ref Guid eventContext);
    [PreserveSig]
    int SetMasterVolumeLevelScalar(float level, ref Guid eventContext);
    [PreserveSig]
    int GetMasterVolumeLevel(out float levelDb);
    [PreserveSig]
    int GetMasterVolumeLevelScalar(out float level);
    [PreserveSig]
    int SetChannelVolumeLevel(uint channel, float levelDb, ref Guid eventContext);
    [PreserveSig]
    int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid eventContext);
    [PreserveSig]
    int GetChannelVolumeLevel(uint channel, out float levelDb);
    [PreserveSig]
    int GetChannelVolumeLevelScalar(uint channel, out float level);
    [PreserveSig]
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid eventContext);
    [PreserveSig]
    int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    [PreserveSig]
    int GetVolumeStepInfo(out uint step, out uint stepCount);
    [PreserveSig]
    int VolumeStepUp(ref Guid eventContext);
    [PreserveSig]
    int VolumeStepDown(ref Guid eventContext);
    [PreserveSig]
    int QueryHardwareSupport(out uint hardwareSupportMask);
    [PreserveSig]
    int GetVolumeRange(out float minimumDb, out float maximumDb, out float incrementDb);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
internal partial interface IMMNotificationClient {
    [PreserveSig]
    int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, AudioEndpointState newState);

    [PreserveSig]
    int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [PreserveSig]
    int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [PreserveSig]
    int OnDefaultDeviceChanged(NativeAudioDataFlow dataFlow, ERole deviceRole, [MarshalAs(UnmanagedType.LPWStr)] string defaultDeviceId);

    [PreserveSig]
    int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, PropertyKey key);
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropertyKey {
    public Guid FormatId;
    public uint PropertyId;
}

[StructLayout(LayoutKind.Explicit)]
internal struct PropVariant : IDisposable {
    [FieldOffset(0)]
    private ushort _valueType;
    [FieldOffset(8)]
    private IntPtr _pointerValue;
    // The native union includes counted arrays, including pointer alignment on x64.
    [FieldOffset(8)]
    private CountedArray _countedArray;

    [StructLayout(LayoutKind.Sequential)]
    private struct CountedArray {
        internal uint Count;
        internal IntPtr Values;
    }

    public string? GetString() {
        return _valueType == 31 && _pointerValue != IntPtr.Zero
            ? Marshal.PtrToStringUni(_pointerValue)
            : null;
    }

    public void Dispose() {
        PropVariantClear(ref this);
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant value);
}

internal static class CoreAudioNative {
    internal const uint ClassContextAll = 23;
    internal const uint StorageModeRead = 0;
    internal static readonly Guid AudioEndpointVolumeInterfaceId = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    internal static readonly Guid DeviceEnumeratorClassId = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    internal static readonly Guid PolicyConfigClassId = new("870af99c-171d-4f9e-af0d-e63df40c2bc9");
    internal static readonly PropertyKey DeviceFriendlyName = new() {
        FormatId = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
        PropertyId = 14
    };

    internal static IMMDeviceEnumerator CreateEnumerator() {
        return (IMMDeviceEnumerator)NativeComActivation.CreateInstance(DeviceEnumeratorClassId);
    }
}
