using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

internal enum NativeAudioDataFlow {
    Render = 0,
    Capture = 1,
    All = 2
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator {
    [PreserveSig]
    int EnumAudioEndpoints(NativeAudioDataFlow dataFlow, AudioEndpointState stateMask, out IMMDeviceCollection devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(NativeAudioDataFlow dataFlow, ERole role, out IMMDevice device);

    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

    [PreserveSig]
    int RegisterEndpointNotificationCallback(IMMNotificationClient client);

    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection {
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int Item(uint index, out IMMDevice device);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice {
    [PreserveSig]
    int Activate(ref Guid interfaceId, uint classContext, IntPtr activationParameters, [MarshalAs(UnmanagedType.IUnknown)] out object instance);

    [PreserveSig]
    int OpenPropertyStore(uint accessMode, out IPropertyStore properties);

    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

    [PreserveSig]
    int GetState(out AudioEndpointState state);
}

[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore {
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

[ComImport]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioEndpointVolume {
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

[ComVisible(true)]
[Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMNotificationClient {
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

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal struct PropVariant : IDisposable {
    [FieldOffset(0)]
    private ushort _valueType;
    [FieldOffset(8)]
    private IntPtr _pointerValue;

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
    internal static readonly PropertyKey DeviceFriendlyName = new() {
        FormatId = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
        PropertyId = 14
    };

    internal static IMMDeviceEnumerator CreateEnumerator() {
        Type enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"), true)
            ?? throw new InvalidOperationException("Windows did not expose the Core Audio device enumerator.");
        return (IMMDeviceEnumerator)(Activator.CreateInstance(enumeratorType)
            ?? throw new InvalidOperationException("Windows did not create the Core Audio device enumerator."));
    }

    internal static void Release(object? value) {
        if (value != null && Marshal.IsComObject(value)) {
            Marshal.ReleaseComObject(value);
        }
    }
}
