using System;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>Reads and controls whichever endpoint is currently the default for one direction and role.</summary>
/// <remarks>
/// <para>
/// Unlike <see cref="AudioEndpointSession"/>, this session follows the default: when Windows assigns another default
/// endpoint, or the current one is disabled, unplugged or invalidated, the next call opens the current default.
/// If notification registration is unavailable, every operation reopens the current default until it can subscribe.
/// </para>
/// <para>
/// All Core Audio work runs on the session's own multithreaded-apartment thread, so it can be used from any thread,
/// including a UI thread, without initializing COM. Calls are serialized.
/// </para>
/// <para>
/// When no default endpoint is available (none exists, it is disconnected or invalidated, access is denied, or the
/// Windows audio service is not running), operations return <see cref="DesktopOperationStatus.Unavailable"/> instead
/// of throwing. Other native failures are propagated.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed partial class AudioDefaultEndpointSession : IDisposable {
    private readonly Func<IAudioEndpointBackend> _open;
    private readonly Func<EventHandler<AudioEndpointChangedEventArgs>, IDisposable> _subscribe;
    private readonly AudioComThread _thread;
    private IAudioEndpointBackend? _backend;
    private IDisposable? _subscription;
    private int _defaultChanged;
    private int _disposed;

    /// <summary>Starts a session that follows the default endpoint for <paramref name="flow"/> and <paramref name="role"/>.</summary>
    /// <param name="flow">Render or capture. <see cref="AudioDataFlow.All"/> is not supported.</param>
    /// <param name="role">The default endpoint role to follow.</param>
    /// <exception cref="ArgumentOutOfRangeException">The direction or role is invalid.</exception>
    /// <remarks>No endpoint is opened until the first call, so construction succeeds even when no endpoint exists.</remarks>
    public AudioDefaultEndpointSession(AudioDataFlow flow, AudioRole role)
        : this(flow, role, () => AudioEndpointNativeBackend.Open(flow, role), SubscribeToEndpointChanges) {
    }

    internal AudioDefaultEndpointSession(
        AudioDataFlow flow,
        AudioRole role,
        Func<IAudioEndpointBackend> open,
        Func<EventHandler<AudioEndpointChangedEventArgs>, IDisposable> subscribe) {
        if (flow != AudioDataFlow.Render && flow != AudioDataFlow.Capture) {
            throw new ArgumentOutOfRangeException(nameof(flow), "A default endpoint requires render or capture.");
        }
        AudioService.ToNativeRole(role);
        Flow = flow;
        Role = role;
        _open = open ?? throw new ArgumentNullException(nameof(open));
        _subscribe = subscribe ?? throw new ArgumentNullException(nameof(subscribe));
        _thread = new AudioComThread($"DesktopManager default {flow} endpoint");
        try {
            _thread.Invoke(() => {
                EnsureSubscribed();
                return true;
            });
        } catch {
            _thread.Dispose();
            throw;
        }
    }

    /// <summary>Gets the endpoint direction this session follows.</summary>
    public AudioDataFlow Flow { get; }

    /// <summary>Gets the default role this session follows.</summary>
    public AudioRole Role { get; }

    /// <summary>Gets whether an active default endpoint can be opened now. Returns false after disposal.</summary>
    public bool IsAvailable {
        get {
            try {
                return Run(_ => true, out _, out _);
            } catch (ObjectDisposedException) {
                return false;
            }
        }
    }

    /// <summary>Reads master volume, mute, and the optional meter from the current default endpoint.</summary>
    /// <returns>The reading, or <see cref="DesktopOperationStatus.Unavailable"/> when no default endpoint is available.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    public AudioEndpointLevelResult GetState() {
        return Run(backend => backend.GetState(), out AudioEndpointLevelInfo? level, out string reason)
            ? new AudioEndpointLevelResult(level!)
            : new AudioEndpointLevelResult(DesktopOperationStatus.Unavailable, reason);
    }

    /// <summary>Sets the current default endpoint's master volume from 0 through 100.</summary>
    /// <param name="percent">A finite percentage from 0 through 100.</param>
    /// <returns>Success, or <see cref="DesktopOperationStatus.Unavailable"/> when no default endpoint is available.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The percentage is outside the supported range.</exception>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    public DesktopOperationResult SetVolume(float percent) {
        AudioEndpointSession.ValidateVolume(percent);
        return Apply(backend => backend.SetVolume(percent / 100f), "volume");
    }

    /// <summary>Sets the current default endpoint's master mute state.</summary>
    /// <param name="muted">The explicit mute state.</param>
    /// <returns>Success, or <see cref="DesktopOperationStatus.Unavailable"/> when no default endpoint is available.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    public DesktopOperationResult SetMute(bool muted) => Apply(backend => backend.SetMute(muted), "mute state");

    /// <summary>Releases the endpoint, stops following the default, and ends the session's thread. Repeated calls have no effect.</summary>
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }
        try {
            _thread.Invoke(() => {
                CloseBackend();
                IDisposable? subscription = _subscription;
                _subscription = null;
                subscription?.Dispose();
                return true;
            });
        } finally {
            _thread.Dispose();
        }
    }

    private static IDisposable SubscribeToEndpointChanges(EventHandler<AudioEndpointChangedEventArgs> handler) {
        var watcher = new AudioEndpointWatcher();
        watcher.Changed += handler;
        return watcher;
    }

    private DesktopOperationResult Apply(Action<IAudioEndpointBackend> change, string setting) {
        bool applied = Run(backend => {
            change(backend);
            return true;
        }, out _, out string reason);
        return applied
            ? new DesktopOperationResult(DesktopOperationStatus.Success, $"The default audio endpoint {setting} was set.")
            : new DesktopOperationResult(DesktopOperationStatus.Unavailable, reason);
    }

    private void OnEndpointChanged(object? sender, AudioEndpointChangedEventArgs change) {
        if (AudioEndpointAvailability.IsDefaultChange(change, Flow, Role)) {
            Volatile.Write(ref _defaultChanged, 1);
        }
    }
}
