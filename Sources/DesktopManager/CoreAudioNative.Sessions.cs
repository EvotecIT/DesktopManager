using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace DesktopManager;

// One set of vtable declarations for generated COM and legacy built-in interop.
// Existing IMMDeviceEnumerator, IMMDevice and IAudioEndpointVolume declarations remain the owners.
#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
internal partial interface IAudioSessionManager2 {
    [PreserveSig] int GetAudioSessionControl(IntPtr sessionGuid, uint flags, out IntPtr control);
    [PreserveSig] int GetSimpleAudioVolume(IntPtr sessionGuid, uint flags, out IntPtr volume);
    [PreserveSig] int GetSessionEnumerator(out IntPtr enumerator);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
internal partial interface IAudioSessionEnumerator {
    [PreserveSig] int GetCount(out int count);
    [PreserveSig] int GetSession(int index, out IntPtr session);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
internal partial interface IAudioSessionControl2 {
    // IAudioSessionControl precedes IAudioSessionControl2 in native vtable order.
    [PreserveSig] int GetState(out int state);
    [PreserveSig] int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);
    [PreserveSig] int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, ref Guid context);
    [PreserveSig] int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string path);
    [PreserveSig] int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string path, ref Guid context);
    [PreserveSig] int GetGroupingParam(out Guid group);
    [PreserveSig] int SetGroupingParam(ref Guid group, ref Guid context);
    [PreserveSig] int RegisterAudioSessionNotification(IntPtr client);
    [PreserveSig] int UnregisterAudioSessionNotification(IntPtr client);
    [PreserveSig] int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string id);
    [PreserveSig] int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string id);
    [PreserveSig] int GetProcessId(out uint processId);
    [PreserveSig] int IsSystemSoundsSession();
    [PreserveSig] int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)] bool optOut);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
internal partial interface ISimpleAudioVolume {
    [PreserveSig] int SetMasterVolume(float level, ref Guid context);
    [PreserveSig] int GetMasterVolume(out float level);
    [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool muted, ref Guid context);
    [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
}

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
internal partial interface IAudioMeterInformation {
    [PreserveSig] int GetPeakValue(out float peak);
}

[SupportedOSPlatform("windows")]
internal static class CoreAudioSessionsNative {
    internal const int NoInterface = unchecked((int)0x80004002);
    internal static readonly Guid SessionManagerInterfaceId = new("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");
    internal static readonly Guid MeterInterfaceId = new("C02216F6-8C67-4B5B-9D00-D008E73E0064");

    internal static IMMDevice OpenDefaultDevice(AudioDataFlow flow, AudioRole role) {
        if (flow != AudioDataFlow.Render && flow != AudioDataFlow.Capture) {
            throw new ArgumentOutOfRangeException(nameof(flow), "A default endpoint requires render or capture.");
        }
        NativeAudioDataFlow nativeFlow = AudioService.ToNativeFlow(flow);
        ERole nativeRole = AudioService.ToNativeRole(role);
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            throw new PlatformNotSupportedException("Core Audio requires Windows.");
        }
        IMMDeviceEnumerator enumerator = CoreAudioNative.CreateEnumerator();
        try {
            // Obtain an owned raw output rather than a shared generated wrapper, so disposal is deterministic.
            int result = GetDefaultDevice(enumerator, nativeFlow, nativeRole, out IntPtr pointer);
            object device = TakeReference(result, pointer);
            try {
                return (IMMDevice)device;
            } catch {
                NativeComActivation.Release(device);
                throw;
            }
        } finally {
            NativeComActivation.Release(enumerator);
        }
    }

    // FromPointer borrows the input and owns its own reference. Always release the incoming reference separately.
    internal static object TakeReference(IntPtr pointer) => TakeReference(0, pointer);

    internal static object TakeReference(int result, IntPtr pointer) {
        try {
            Marshal.ThrowExceptionForHR(result);
            if (pointer == IntPtr.Zero) {
                throw new InvalidOperationException("Windows returned an empty Core Audio interface.");
            }
            return NativeComActivation.FromPointer(pointer);
        } finally {
            ReleasePointer(pointer);
        }
    }

    internal static void ReleasePointer(IntPtr pointer) {
        if (pointer != IntPtr.Zero) {
            Marshal.Release(pointer);
        }
    }

    internal static float NormalizePeak(float peak) => float.IsNaN(peak) ? 0f : Math.Max(0f, Math.Min(1f, peak));

#if NET8_0_OR_GREATER
    private static unsafe int GetDefaultDevice(IMMDeviceEnumerator enumerator, NativeAudioDataFlow flow, ERole role, out IntPtr device) {
        void* native = ComInterfaceMarshaller<IMMDeviceEnumerator>.ConvertToUnmanaged(enumerator);
        try {
            IntPtr value = IntPtr.Zero;
            var getDefault = (delegate* unmanaged[Stdcall]<void*, NativeAudioDataFlow, ERole, IntPtr*, int>)(*(void***)native)[4];
            int result = getDefault(native, flow, role, &value);
            device = value;
            return result;
        } finally {
            ComInterfaceMarshaller<IMMDeviceEnumerator>.Free(native);
        }
    }

    internal static unsafe int Activate(IMMDevice device, Guid interfaceId, out IntPtr instance) {
        void* native = ComInterfaceMarshaller<IMMDevice>.ConvertToUnmanaged(device);
        try {
            IntPtr value = IntPtr.Zero;
            // IMMDevice.Activate is slot 3. Its existing managed signature is specifically typed to endpoint volume.
            var activate = (delegate* unmanaged[Stdcall]<void*, Guid*, uint, IntPtr, IntPtr*, int>)(*(void***)native)[3];
            int result = activate(native, &interfaceId, CoreAudioNative.ClassContextAll, IntPtr.Zero, &value);
            instance = value;
            return result;
        } finally {
            ComInterfaceMarshaller<IMMDevice>.Free(native);
        }
    }
#else
    private static int GetDefaultDevice(IMMDeviceEnumerator enumerator, NativeAudioDataFlow flow, ERole role, out IntPtr device) {
        IntPtr native = Marshal.GetComInterfaceForObject(enumerator, typeof(IMMDeviceEnumerator));
        try {
            var getDefault = Marshal.GetDelegateForFunctionPointer<GetDefaultDeviceDelegate>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(native), 4 * IntPtr.Size));
            return getDefault(native, flow, role, out device);
        } finally {
            Marshal.Release(native);
        }
    }

    internal static int Activate(IMMDevice device, Guid interfaceId, out IntPtr instance) {
        IntPtr native = Marshal.GetComInterfaceForObject(device, typeof(IMMDevice));
        try {
            var activate = Marshal.GetDelegateForFunctionPointer<ActivateDelegate>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(native), 3 * IntPtr.Size));
            return activate(native, ref interfaceId, CoreAudioNative.ClassContextAll, IntPtr.Zero, out instance);
        } finally {
            Marshal.Release(native);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDefaultDeviceDelegate(IntPtr enumerator, NativeAudioDataFlow flow, ERole role, out IntPtr device);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int ActivateDelegate(IntPtr device, ref Guid interfaceId, uint classContext, IntPtr parameters, out IntPtr instance);
#endif
}
