#if NET8_0_OR_GREATER && WINDOWS
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace DesktopManager;

// Typed SDK projections keep WinRT activation and event marshaling compatible with Native AOT.
[SupportedOSPlatform("windows10.0.17763.0")]
internal sealed class MediaSessionServiceWindowsBackend : IMediaSessionBackend {
    private readonly object _sync = new();
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private long _version;
    private bool _disposed;

    private MediaSessionServiceWindowsBackend(GlobalSystemMediaTransportControlsSessionManager manager) {
        _manager = manager;
    }

    internal static async Task<IMediaSessionBackend?> CreateAsync() {
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().ConfigureAwait(false);
        var backend = new MediaSessionServiceWindowsBackend(manager);
        try {
            lock (backend._sync) {
                manager.CurrentSessionChanged += backend.OnCurrentSessionChanged;
                backend.Attach(manager.GetCurrentSession());
            }
            return backend;
        } catch {
            backend.Dispose();
            throw;
        }
    }

    public event EventHandler? Changed;

    public long Version {
        get {
            lock (_sync) {
                return _version;
            }
        }
    }

    public async Task<MediaSessionServiceData?> GetCurrentAsync() {
        GlobalSystemMediaTransportControlsSession? session;
        long version;
        lock (_sync) {
            ThrowIfDisposed();
            Attach(_manager.GetCurrentSession());
            session = _session;
            version = _version;
        }
        if (session == null) {
            return null;
        }

        var properties = await session.TryGetMediaPropertiesAsync().AsTask().ConfigureAwait(false);
        MediaSessionServiceData data;
        lock (_sync) {
            ThrowIfDisposed();
            if (version != _version) {
                throw new InvalidOperationException("The media session changed during the read; refresh again.");
            }
            var playback = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();
            data = new MediaSessionServiceData {
                Title = properties.Title ?? string.Empty,
                Artist = properties.Artist ?? string.Empty,
                AlbumArtist = properties.AlbumArtist ?? string.Empty,
                SourceAppId = session.SourceAppUserModelId ?? string.Empty,
                IsPlaying = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                Position = timeline.Position,
                Duration = timeline.EndTime - timeline.StartTime,
                PositionUpdated = timeline.LastUpdatedTime,
                Version = version
            };
        }

        // A broken or missing thumbnail must not hide otherwise useful playback metadata.
        if (properties.Thumbnail != null) {
            try {
                var nativeStream = await properties.Thumbnail.OpenReadAsync().AsTask().ConfigureAwait(false);
                try {
                    data.Artwork = nativeStream.AsStreamForRead();
                } catch {
                    nativeStream.Dispose();
                    throw;
                }
            } catch (Exception ex) {
                DesktopManagerDiagnostics.Report($"Media artwork could not be opened: {ex.Message}");
            }
        }
        return data;
    }

    public async Task<bool?> SendAsync(MediaSessionServiceCommand command) {
        Task<bool> pending;
        lock (_sync) {
            ThrowIfDisposed();
            Attach(_manager.GetCurrentSession());
            if (_session == null) {
                return null;
            }
            // Dispatch while holding the session gate; later callbacks cannot redirect an in-flight command.
            pending = command switch {
                MediaSessionServiceCommand.TogglePlayPause => _session.TryTogglePlayPauseAsync().AsTask(),
                MediaSessionServiceCommand.Next => _session.TrySkipNextAsync().AsTask(),
                MediaSessionServiceCommand.Previous => _session.TrySkipPreviousAsync().AsTask(),
                _ => throw new ArgumentOutOfRangeException(nameof(command))
            };
        }
        return await pending.ConfigureAwait(false);
    }

    private void Attach(GlobalSystemMediaTransportControlsSession? session) {
        if (ReferenceEquals(session, _session)) {
            return;
        }
        DetachSession();
        _session = session;
        _version++;
        if (session != null) {
            try {
                session.MediaPropertiesChanged += OnSessionChanged;
                session.PlaybackInfoChanged += OnSessionChanged;
                session.TimelinePropertiesChanged += OnSessionChanged;
            } catch {
                DetachSession();
                throw;
            }
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) {
        try {
            lock (_sync) {
                if (_disposed || !ReferenceEquals(sender, _manager)) {
                    return;
                }
                Attach(sender.GetCurrentSession());
                _version++;
            }
            Changed?.Invoke(this, EventArgs.Empty);
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"Media session notification failed: {ex.Message}");
        }
    }

    private void OnSessionChanged(GlobalSystemMediaTransportControlsSession sender, object args) {
        try {
            lock (_sync) {
                if (_disposed || !ReferenceEquals(sender, _session)) {
                    return;
                }
                _version++;
            }
            Changed?.Invoke(this, EventArgs.Empty);
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"Media metadata notification failed: {ex.Message}");
        }
    }

    private void DetachSession() {
        var session = _session;
        _session = null;
        if (session != null) {
            Unsubscribe(() => session.MediaPropertiesChanged -= OnSessionChanged);
            Unsubscribe(() => session.PlaybackInfoChanged -= OnSessionChanged);
            Unsubscribe(() => session.TimelinePropertiesChanged -= OnSessionChanged);
        }
    }

    private static void Unsubscribe(Action remove) {
        try {
            remove();
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"Media subscription could not be removed: {ex.Message}");
        }
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(MediaSessionService));
        }
    }

    public void Dispose() {
        lock (_sync) {
            if (_disposed) {
                return;
            }
            _disposed = true;
            _version++;
            Changed = null;
            Unsubscribe(() => _manager.CurrentSessionChanged -= OnCurrentSessionChanged);
            DetachSession();
        }
    }
}
#endif
