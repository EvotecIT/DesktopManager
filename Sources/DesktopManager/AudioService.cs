using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Service to manage system audio devices.
/// </summary>
public class AudioService {
    private readonly IPolicyConfigClient _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioService"/> class.
    /// </summary>
    /// <param name="policy">Optional policy configuration client.</param>
    public AudioService(IPolicyConfigClient? policy = null) {
        _policy = policy ?? new PolicyConfigClient();
    }

    /// <summary>
    /// Sets the default audio device for all roles.
    /// </summary>
    /// <param name="deviceId">Identifier of the device.</param>
    [SupportedOSPlatform("windows")]
    public void SetDefaultAudioDevice(string deviceId) {
        SetDefaultAudioDevice(deviceId, AudioRole.Console, AudioRole.Multimedia, AudioRole.Communications);
    }

    /// <summary>
    /// Sets the default audio endpoint for selected roles.
    /// </summary>
    /// <param name="deviceId">The stable Windows endpoint identifier.</param>
    /// <param name="roles">The roles to assign. When empty, all roles are assigned.</param>
    [SupportedOSPlatform("windows")]
    public void SetDefaultAudioDevice(string deviceId, params AudioRole[] roles) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentNullException(nameof(deviceId));
        }

        AudioRole[] selectedRoles = roles == null || roles.Length == 0
            ? new[] { AudioRole.Console, AudioRole.Multimedia, AudioRole.Communications }
            : roles.Distinct().ToArray();
        foreach (AudioRole role in selectedRoles) {
            _policy.SetDefaultEndpoint(deviceId, ToNativeRole(role));
        }
    }

    /// <summary>Gets Windows audio endpoints and their current default, volume, and mute state.</summary>
    /// <param name="dataFlow">The endpoint direction to include.</param>
    /// <param name="states">The endpoint device states to include.</param>
    /// <returns>The current endpoint snapshots.</returns>
    [SupportedOSPlatform("windows")]
    public IReadOnlyList<AudioEndpointInfo> GetEndpoints(
        AudioDataFlow dataFlow = AudioDataFlow.All,
        AudioEndpointState states = AudioEndpointState.All) {
        IMMDeviceEnumerator enumerator = CoreAudioNative.CreateEnumerator();
        try {
            var endpoints = new List<AudioEndpointInfo>();
            AudioDataFlow[] flows = dataFlow == AudioDataFlow.All
                ? new[] { AudioDataFlow.Render, AudioDataFlow.Capture }
                : new[] { dataFlow };
            foreach (AudioDataFlow flow in flows) {
                AddEndpoints(enumerator, flow, states, endpoints);
            }

            return endpoints.ToArray();
        } finally {
            CoreAudioNative.Release(enumerator);
        }
    }

    /// <summary>Gets a Windows audio endpoint by its stable identifier.</summary>
    /// <param name="deviceId">The stable Windows endpoint identifier.</param>
    /// <returns>The current endpoint snapshot.</returns>
    [SupportedOSPlatform("windows")]
    public AudioEndpointInfo GetEndpoint(string deviceId) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentNullException(nameof(deviceId));
        }

        AudioEndpointInfo? endpoint = GetEndpoints().FirstOrDefault(candidate =>
            string.Equals(candidate.Id, deviceId, StringComparison.OrdinalIgnoreCase));
        return endpoint ?? throw new InvalidOperationException($"Audio endpoint '{deviceId}' was not found.");
    }

    /// <summary>Sets master volume for an audio endpoint.</summary>
    /// <param name="deviceId">The stable Windows endpoint identifier.</param>
    /// <param name="volumePercent">The requested master volume from 0 through 100.</param>
    [SupportedOSPlatform("windows")]
    public void SetEndpointVolume(string deviceId, float volumePercent) {
        if (float.IsNaN(volumePercent) || float.IsInfinity(volumePercent) || volumePercent < 0 || volumePercent > 100) {
            throw new ArgumentOutOfRangeException(nameof(volumePercent), "Volume must be between 0 and 100.");
        }

        WithEndpointVolume(deviceId, volume => {
            Guid context = Guid.Empty;
            Marshal.ThrowExceptionForHR(volume.SetMasterVolumeLevelScalar(volumePercent / 100f, ref context));
        });
    }

    /// <summary>Sets master mute for an audio endpoint.</summary>
    /// <param name="deviceId">The stable Windows endpoint identifier.</param>
    /// <param name="muted">The explicit mute state.</param>
    [SupportedOSPlatform("windows")]
    public void SetEndpointMute(string deviceId, bool muted) {
        WithEndpointVolume(deviceId, volume => {
            Guid context = Guid.Empty;
            Marshal.ThrowExceptionForHR(volume.SetMute(muted, ref context));
        });
    }

    /// <summary>Creates a live Core Audio endpoint notification watcher.</summary>
    /// <returns>A watcher that remains registered until disposed.</returns>
    [SupportedOSPlatform("windows")]
    public AudioEndpointWatcher CreateWatcher() {
        return new AudioEndpointWatcher();
    }

    internal static NativeAudioDataFlow ToNativeFlow(AudioDataFlow dataFlow) {
        return dataFlow switch {
            AudioDataFlow.Render => NativeAudioDataFlow.Render,
            AudioDataFlow.Capture => NativeAudioDataFlow.Capture,
            AudioDataFlow.All => NativeAudioDataFlow.All,
            _ => throw new ArgumentOutOfRangeException(nameof(dataFlow))
        };
    }

    internal static AudioDataFlow FromNativeFlow(NativeAudioDataFlow dataFlow) {
        return dataFlow switch {
            NativeAudioDataFlow.Render => AudioDataFlow.Render,
            NativeAudioDataFlow.Capture => AudioDataFlow.Capture,
            _ => AudioDataFlow.All
        };
    }

    internal static ERole ToNativeRole(AudioRole role) {
        return role switch {
            AudioRole.Console => ERole.eConsole,
            AudioRole.Multimedia => ERole.eMultimedia,
            AudioRole.Communications => ERole.eCommunications,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }

    internal static AudioRole FromNativeRole(ERole role) {
        return role switch {
            ERole.eConsole => AudioRole.Console,
            ERole.eMultimedia => AudioRole.Multimedia,
            ERole.eCommunications => AudioRole.Communications,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }

    private static AudioEndpointInfo CreateEndpointInfo(
        IMMDevice device,
        AudioDataFlow dataFlow,
        IReadOnlyDictionary<AudioRole, string> defaults) {
        Marshal.ThrowExceptionForHR(device.GetId(out string id));
        Marshal.ThrowExceptionForHR(device.GetState(out AudioEndpointState state));
        string name = GetFriendlyName(device) ?? id;
        GetVolumeState(device, out float? volumePercent, out bool? isMuted);
        AudioRole[] roles = defaults
            .Where(pair => string.Equals(pair.Value, id, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .ToArray();
        return new AudioEndpointInfo(id, name, dataFlow, state, volumePercent, isMuted, roles);
    }

    private static void AddEndpoints(
        IMMDeviceEnumerator enumerator,
        AudioDataFlow dataFlow,
        AudioEndpointState states,
        ICollection<AudioEndpointInfo> endpoints) {
        IMMDeviceCollection? collection = null;
        try {
            Marshal.ThrowExceptionForHR(enumerator.EnumAudioEndpoints(ToNativeFlow(dataFlow), states, out collection));
            Marshal.ThrowExceptionForHR(collection.GetCount(out uint count));
            Dictionary<AudioRole, string> defaults = GetDefaultEndpointIds(enumerator, dataFlow);
            for (uint index = 0; index < count; index++) {
                Marshal.ThrowExceptionForHR(collection.Item(index, out IMMDevice device));
                try {
                    endpoints.Add(CreateEndpointInfo(device, dataFlow, defaults));
                } catch (COMException ex) {
                    DesktopManagerDiagnostics.Report($"Audio endpoint {dataFlow} index {index} could not be read: {ex.Message}");
                } finally {
                    CoreAudioNative.Release(device);
                }
            }
        } finally {
            CoreAudioNative.Release(collection);
        }
    }

    private static Dictionary<AudioRole, string> GetDefaultEndpointIds(IMMDeviceEnumerator enumerator, AudioDataFlow dataFlow) {
        var defaults = new Dictionary<AudioRole, string>();
        NativeAudioDataFlow flow = dataFlow == AudioDataFlow.Capture ? NativeAudioDataFlow.Capture : NativeAudioDataFlow.Render;
        foreach (AudioRole role in new[] { AudioRole.Console, AudioRole.Multimedia, AudioRole.Communications }) {
            int result = enumerator.GetDefaultAudioEndpoint(flow, ToNativeRole(role), out IMMDevice device);
            if (result < 0) {
                continue;
            }
            try {
                Marshal.ThrowExceptionForHR(device.GetId(out string id));
                defaults[role] = id;
            } finally {
                CoreAudioNative.Release(device);
            }
        }
        return defaults;
    }

    private static string? GetFriendlyName(IMMDevice device) {
        int openResult = device.OpenPropertyStore(CoreAudioNative.StorageModeRead, out IPropertyStore properties);
        if (openResult < 0) {
            return null;
        }
        try {
            PropertyKey key = CoreAudioNative.DeviceFriendlyName;
            int valueResult = properties.GetValue(ref key, out PropVariant value);
            if (valueResult < 0) {
                return null;
            }
            try {
                return value.GetString();
            } finally {
                value.Dispose();
            }
        } finally {
            CoreAudioNative.Release(properties);
        }
    }

    private static void GetVolumeState(IMMDevice device, out float? volumePercent, out bool? isMuted) {
        volumePercent = null;
        isMuted = null;
        try {
            IAudioEndpointVolume volume = ActivateEndpointVolume(device);
            try {
                Marshal.ThrowExceptionForHR(volume.GetMasterVolumeLevelScalar(out float scalar));
                Marshal.ThrowExceptionForHR(volume.GetMute(out bool muted));
                volumePercent = scalar * 100f;
                isMuted = muted;
            } finally {
                CoreAudioNative.Release(volume);
            }
        } catch (Exception ex) when (ex is COMException || ex is IOException || ex is InvalidCastException) {
            // Disabled, unplugged, and not-present endpoints commonly reject volume activation.
        }
    }

    private static void WithEndpointVolume(string deviceId, Action<IAudioEndpointVolume> action) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentNullException(nameof(deviceId));
        }
        if (action == null) {
            throw new ArgumentNullException(nameof(action));
        }

        IMMDeviceEnumerator enumerator = CoreAudioNative.CreateEnumerator();
        IMMDevice? device = null;
        IAudioEndpointVolume? volume = null;
        try {
            Marshal.ThrowExceptionForHR(enumerator.GetDevice(deviceId, out device));
            volume = ActivateEndpointVolume(device);
            action(volume);
        } finally {
            CoreAudioNative.Release(volume);
            CoreAudioNative.Release(device);
            CoreAudioNative.Release(enumerator);
        }
    }

    private static IAudioEndpointVolume ActivateEndpointVolume(IMMDevice device) {
        Guid interfaceId = CoreAudioNative.AudioEndpointVolumeInterfaceId;
        Marshal.ThrowExceptionForHR(device.Activate(
            ref interfaceId,
            CoreAudioNative.ClassContextAll,
            IntPtr.Zero,
            out object volume));
        return (IAudioEndpointVolume)volume;
    }
}
