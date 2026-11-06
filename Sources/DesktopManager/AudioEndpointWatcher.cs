using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace DesktopManager;

/// <summary>
/// Observes Core Audio endpoint arrival, removal, state, default, and property changes.
/// </summary>
#if NET8_0_OR_GREATER
[GeneratedComClass]
#else
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
#endif
[SupportedOSPlatform("windows")]
public sealed partial class AudioEndpointWatcher : IMMNotificationClient, IDisposable {
    private readonly IMMDeviceEnumerator _enumerator;
    private readonly IntPtr _callbackReference;
    private readonly Action<AudioEndpointChangeKind>? _notified;
    private int _disposed;

    /// <summary>Initializes and registers a Core Audio notification watcher.</summary>
    public AudioEndpointWatcher() : this(null) {
    }

    /// <param name="notified">Runs synchronously on the Core Audio callback thread, so it must be quick and must not call back into Core Audio.</param>
    internal AudioEndpointWatcher(Action<AudioEndpointChangeKind>? notified) {
        _notified = notified;
        _enumerator = CoreAudioNative.CreateEnumerator();
        try {
            // Core Audio borrows this interface without AddRef. Keep a COM reference (and
            // therefore its managed target) alive until Unregister has drained callbacks.
            _callbackReference = CreateCallbackReference();
#if NET8_0_OR_GREATER
            Marshal.ThrowExceptionForHR(_enumerator.RegisterEndpointNotificationCallback(_callbackReference));
#else
            Marshal.ThrowExceptionForHR(_enumerator.RegisterEndpointNotificationCallback(this));
#endif
        } catch {
            if (_callbackReference != IntPtr.Zero) {
                Marshal.Release(_callbackReference);
            }
            NativeComActivation.Release(_enumerator);
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
#if NET8_0_OR_GREATER
            Marshal.ThrowExceptionForHR(_enumerator.UnregisterEndpointNotificationCallback(_callbackReference));
#else
            Marshal.ThrowExceptionForHR(_enumerator.UnregisterEndpointNotificationCallback(this));
#endif
        } catch {
            Volatile.Write(ref _disposed, 0);
            throw;
        }
        Marshal.Release(_callbackReference);
        NativeComActivation.Release(_enumerator);
        GC.KeepAlive(this);
    }

    private IntPtr CreateCallbackReference() {
#if NET8_0_OR_GREATER
        unsafe { return (IntPtr)ComInterfaceMarshaller<IMMNotificationClient>.ConvertToUnmanaged(this); }
#else
        return Marshal.GetIUnknownForObject(this);
#endif
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
        _notified?.Invoke(args.ChangeKind);
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
