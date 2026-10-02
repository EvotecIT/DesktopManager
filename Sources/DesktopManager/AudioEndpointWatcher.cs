using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Observes Core Audio endpoint arrival, removal, state, default, and property changes.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[SupportedOSPlatform("windows")]
public sealed class AudioEndpointWatcher : IMMNotificationClient, IDisposable {
    private readonly IMMDeviceEnumerator _enumerator;
    private int _disposed;

    /// <summary>Initializes and registers a Core Audio notification watcher.</summary>
    public AudioEndpointWatcher() {
        _enumerator = CoreAudioNative.CreateEnumerator();
        try {
            Marshal.ThrowExceptionForHR(_enumerator.RegisterEndpointNotificationCallback(this));
        } catch {
            CoreAudioNative.Release(_enumerator);
            throw;
        }
    }

    /// <summary>Raised on a managed worker when Core Audio reports an endpoint change.</summary>
    public event EventHandler<AudioEndpointChangedEventArgs>? Changed;

    /// <inheritdoc/>
    public void Dispose() {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) {
            return;
        }

        try {
            Marshal.ThrowExceptionForHR(_enumerator.UnregisterEndpointNotificationCallback(this));
        } catch {
            Volatile.Write(ref _disposed, 0);
            throw;
        }
        CoreAudioNative.Release(_enumerator);
    }

    int IMMNotificationClient.OnDeviceStateChanged(string deviceId, AudioEndpointState newState) {
        Raise(new AudioEndpointChangedEventArgs(AudioEndpointChangeKind.StateChanged, deviceId, state: newState));
        return 0;
    }

    int IMMNotificationClient.OnDeviceAdded(string deviceId) {
        Raise(new AudioEndpointChangedEventArgs(AudioEndpointChangeKind.Added, deviceId));
        return 0;
    }

    int IMMNotificationClient.OnDeviceRemoved(string deviceId) {
        Raise(new AudioEndpointChangedEventArgs(AudioEndpointChangeKind.Removed, deviceId));
        return 0;
    }

    int IMMNotificationClient.OnDefaultDeviceChanged(NativeAudioDataFlow dataFlow, ERole deviceRole, string defaultDeviceId) {
        Raise(new AudioEndpointChangedEventArgs(
            AudioEndpointChangeKind.DefaultChanged,
            defaultDeviceId ?? string.Empty,
            dataFlow: AudioService.FromNativeFlow(dataFlow),
            role: AudioService.FromNativeRole(deviceRole)));
        return 0;
    }

    int IMMNotificationClient.OnPropertyValueChanged(string deviceId, PropertyKey key) {
        Raise(new AudioEndpointChangedEventArgs(AudioEndpointChangeKind.PropertyChanged, deviceId));
        return 0;
    }

    private void Raise(AudioEndpointChangedEventArgs args) {
        // Subscribers may dispose the watcher. Never run them inside IMMNotificationClient.
        ThreadPool.QueueUserWorkItem(_ => Deliver(args));
    }

    private void Deliver(AudioEndpointChangedEventArgs args) {
        EventHandler<AudioEndpointChangedEventArgs>? handlers = Changed;
        if (handlers == null) {
            return;
        }
        foreach (EventHandler<AudioEndpointChangedEventArgs> handler in handlers.GetInvocationList()) {
            if (Volatile.Read(ref _disposed) != 0) {
                return;
            }
            try {
                handler(this, args);
            } catch (Exception ex) {
                DesktopManagerDiagnostics.Report($"Audio endpoint notification handler failed: {ex.Message}");
            }
        }
    }
}
