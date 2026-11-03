using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

[SupportedOSPlatform("windows")]
internal sealed class AudioEndpointNativeBackend : IAudioEndpointBackend {
    private IMMDevice? _device;
    private IAudioEndpointVolume? _volume;
    private IAudioMeterInformation? _meter;

    private AudioEndpointNativeBackend(IMMDevice device) {
        _device = device;
    }

    internal static AudioEndpointNativeBackend Open(AudioDataFlow flow, AudioRole role) {
        var backend = new AudioEndpointNativeBackend(CoreAudioSessionsNative.OpenDefaultDevice(flow, role));
        try {
            object volume = CoreAudioSessionsNative.TakeReference(backend.ActivateInterface(CoreAudioNative.AudioEndpointVolumeInterfaceId));
            try {
                backend._volume = (IAudioEndpointVolume)volume;
            } catch {
                NativeComActivation.Release(volume);
                throw;
            }

            int result = CoreAudioSessionsNative.Activate(backend._device!, CoreAudioSessionsNative.MeterInterfaceId, out IntPtr pointer);
            if (result == CoreAudioSessionsNative.NoInterface) {
                CoreAudioSessionsNative.ReleasePointer(pointer);
            } else {
                object meter = CoreAudioSessionsNative.TakeReference(result, pointer);
                try {
                    backend._meter = (IAudioMeterInformation)meter;
                } catch {
                    NativeComActivation.Release(meter);
                    throw;
                }
            }
            return backend;
        } catch {
            backend.Dispose();
            throw;
        }
    }

    public bool IsAvailable => _device != null && _device.GetState(out AudioEndpointState state) >= 0 &&
        (state & AudioEndpointState.Active) != 0;

    public AudioEndpointLevelInfo GetState() {
        Marshal.ThrowExceptionForHR(_volume!.GetMasterVolumeLevelScalar(out float volume));
        Marshal.ThrowExceptionForHR(_volume.GetMute(out bool muted));
        float? peak = null;
        if (_meter != null) {
            Marshal.ThrowExceptionForHR(_meter.GetPeakValue(out float value));
            peak = CoreAudioSessionsNative.NormalizePeak(value);
        }
        return new AudioEndpointLevelInfo(volume * 100f, muted, peak);
    }

    public void SetVolume(float scalar) {
        Guid context = Guid.Empty;
        Marshal.ThrowExceptionForHR(_volume!.SetMasterVolumeLevelScalar(scalar, ref context));
    }

    public void SetMute(bool muted) {
        Guid context = Guid.Empty;
        Marshal.ThrowExceptionForHR(_volume!.SetMute(muted, ref context));
    }

    public IntPtr ActivateInterface(Guid interfaceId) {
        int result = CoreAudioSessionsNative.Activate(_device!, interfaceId, out IntPtr pointer);
        if (result < 0 || pointer == IntPtr.Zero) {
            CoreAudioSessionsNative.ReleasePointer(pointer);
            Marshal.ThrowExceptionForHR(result);
            throw new InvalidOperationException("Windows returned no interface for the audio endpoint.");
        }
        return pointer;
    }

    public void Dispose() {
        NativeComActivation.Release(_meter);
        NativeComActivation.Release(_volume);
        NativeComActivation.Release(_device);
        _meter = null;
        _volume = null;
        _device = null;
    }
}
