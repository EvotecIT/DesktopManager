using System;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>Retains the endpoint that was default when opened, for repeated volume and meter access.</summary>
/// <remarks>
/// Obtain a session from <see cref="AudioService.OpenDefaultEndpoint"/> and dispose it when finished.
/// Serialize access and disposal on the consumer's COM-initialized thread. Changes to the default
/// endpoint do not redirect this session. Native activation and operation failures are propagated.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class AudioEndpointSession : IDisposable {
    private IAudioEndpointBackend? _backend;

    internal AudioEndpointSession(IAudioEndpointBackend backend) {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    /// <summary>Gets whether the retained endpoint is active. Returns false after disposal.</summary>
    public bool IsAvailable => _backend != null && _backend.IsAvailable;

    /// <summary>Reads master volume, mute, and the optional meter from the retained endpoint.</summary>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The retained endpoint is unavailable.</exception>
    public AudioEndpointLevelInfo GetState() => GetAvailableBackend().GetState();

    /// <summary>Sets the retained endpoint's master volume from 0 through 100.</summary>
    /// <param name="percent">A finite percentage from 0 through 100.</param>
    /// <exception cref="ArgumentOutOfRangeException">The percentage is outside the supported range.</exception>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The retained endpoint is unavailable.</exception>
    public void SetVolume(float percent) {
        ValidateVolume(percent);
        GetAvailableBackend().SetVolume(percent / 100f);
    }

    /// <summary>Sets the retained endpoint's master mute state.</summary>
    /// <param name="muted">The explicit mute state.</param>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The retained endpoint is unavailable.</exception>
    public void SetMute(bool muted) => GetAvailableBackend().SetMute(muted);

    /// <summary>Activates an additional native COM interface on the retained endpoint.</summary>
    /// <param name="interfaceId">The IID of the endpoint interface, for example IAudioClient.</param>
    /// <returns>
    /// One owned native COM reference. The caller MUST call <see cref="System.Runtime.InteropServices.Marshal.Release(IntPtr)"/>
    /// on this pointer when finished, including when wrapping or querying the pointer fails.
    /// A wrapper may hold its own separate reference; releasing that wrapper does not release this reference.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The retained endpoint is unavailable.</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">Windows rejected activation.</exception>
    public IntPtr ActivateInterface(Guid interfaceId) => GetAvailableBackend().ActivateInterface(interfaceId);

    /// <summary>Releases the volume, meter, and retained endpoint. Repeated calls have no effect.</summary>
    public void Dispose() {
        IAudioEndpointBackend? backend = _backend;
        _backend = null;
        backend?.Dispose();
    }

    internal static void ValidateVolume(float percent) {
        if (float.IsNaN(percent) || float.IsInfinity(percent) || percent < 0 || percent > 100) {
            throw new ArgumentOutOfRangeException(nameof(percent), "Volume must be between 0 and 100.");
        }
    }

    private IAudioEndpointBackend GetAvailableBackend() {
        IAudioEndpointBackend backend = _backend ?? throw new ObjectDisposedException(nameof(AudioEndpointSession));
        if (!backend.IsAvailable) {
            throw new InvalidOperationException("The retained audio endpoint is unavailable. Open a new session to resolve the current default.");
        }
        return backend;
    }
}

// The production boundary owns native resources; callers above it only work with readings and commands.
internal interface IAudioEndpointBackend : IDisposable {
    bool IsAvailable { get; }
    AudioEndpointLevelInfo GetState();
    void SetVolume(float scalar);
    void SetMute(bool muted);
    IntPtr ActivateInterface(Guid interfaceId);
}
