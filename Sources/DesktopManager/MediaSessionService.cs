using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>Reads and controls the current Windows SMTC session. Construction does not access Windows media APIs.</summary>
/// <remarks>Modern Windows targets use the typed Windows SDK projection. Other targets report Unsupported.</remarks>
public sealed class MediaSessionService : IDisposable {
    private const int MaximumArtworkBytes = 16 * 1024 * 1024;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _operations = new(1, 1);
    private readonly Func<Task<IMediaSessionBackend?>> _createBackend;
    private IMediaSessionBackend? _backend;
    private bool _initialized;
    private bool _active;
    private bool _disposed;

    /// <summary>Creates a service that initializes its native manager on the first read or transport request.</summary>
    public MediaSessionService() : this(CreateBackendAsync) { }

    internal MediaSessionService(Func<Task<IMediaSessionBackend?>> createBackend) {
        _createBackend = createBackend ?? throw new ArgumentNullException(nameof(createBackend));
    }

    /// <summary>Signals that clients should refresh. Callbacks may run on a Windows callback thread; subscriber failures are isolated.</summary>
    public event EventHandler? Changed;

    /// <summary>Reads metadata and encoded artwork without changing playback. Success with null means no active session.</summary>
    public Task<MediaSessionResult> GetCurrentAsync() => ExecuteAsync(null);

    /// <summary>Requests play/pause on the current session. Success means the source accepted the request.</summary>
    public async Task<DesktopOperationResult> TogglePlayPauseAsync() => await ExecuteAsync(MediaSessionServiceCommand.TogglePlayPause).ConfigureAwait(false);

    /// <summary>Requests the next item on the current session.</summary>
    public async Task<DesktopOperationResult> NextAsync() => await ExecuteAsync(MediaSessionServiceCommand.Next).ConfigureAwait(false);

    /// <summary>Requests the previous item on the current session.</summary>
    public async Task<DesktopOperationResult> PreviousAsync() => await ExecuteAsync(MediaSessionServiceCommand.Previous).ConfigureAwait(false);

    private async Task<MediaSessionResult> ExecuteAsync(MediaSessionServiceCommand? command) {
        await _operations.WaitAsync().ConfigureAwait(false);
        try {
            lock (_sync) {
                if (_disposed) {
                    return DisposedResult();
                }
                _active = true;
            }

            if (!_initialized) {
                IMediaSessionBackend? created = await _createBackend().ConfigureAwait(false);
                lock (_sync) {
                    _backend = created;
                    _initialized = true;
                    if (!_disposed && created != null) {
                        created.Changed += OnChanged;
                    }
                }
            }

            lock (_sync) {
                if (_disposed) {
                    return DisposedResult();
                }
            }
            IMediaSessionBackend? backend = _backend;
            if (backend == null) {
                return new MediaSessionResult(DesktopOperationStatus.Unsupported,
                    "SMTC requires a modern Windows-specific DesktopManager target and Windows 10 version 1809 or later.");
            }

            MediaSessionResult result;
            if (command.HasValue) {
                Task<bool?> pending;
                lock (_sync) {
                    if (_disposed) {
                        return DisposedResult();
                    }
                    pending = backend.SendAsync(command.Value);
                }
                bool? accepted = await pending.ConfigureAwait(false);
                result = accepted == null
                    ? new MediaSessionResult(DesktopOperationStatus.Unavailable, "No media session is active.")
                    : new MediaSessionResult(accepted.Value ? DesktopOperationStatus.Success : DesktopOperationStatus.Failed,
                        accepted.Value ? "The media source accepted the request." : "The media source rejected the request.");
            } else {
                using MediaSessionServiceData? data = await backend.GetCurrentAsync().ConfigureAwait(false);
                MediaSessionInfo? session = data == null ? null : new MediaSessionInfo(
                    data.Title, string.IsNullOrWhiteSpace(data.Artist) ? data.AlbumArtist : data.Artist,
                    data.SourceAppId, MediaSourceNameResolver.Resolve(data.SourceAppId), await ReadArtworkAsync(data.Artwork).ConfigureAwait(false),
                    data.IsPlaying, data.Position, data.Duration < TimeSpan.Zero ? TimeSpan.Zero : data.Duration, data.PositionUpdated);
                result = data != null && data.Version != backend.Version
                    ? new MediaSessionResult(DesktopOperationStatus.Unavailable, "The media session changed during the read; refresh again.")
                    : new MediaSessionResult(DesktopOperationStatus.Success, session == null ? "No media session is active." : "Media session read.", session);
            }
            lock (_sync) {
                return _disposed ? DisposedResult() : result;
            }
        } catch (PlatformNotSupportedException ex) {
            lock (_sync) {
                return _disposed ? DisposedResult() : new MediaSessionResult(DesktopOperationStatus.Unsupported, ex.Message);
            }
        } catch (Exception ex) {
            lock (_sync) {
                return _disposed ? DisposedResult() : new MediaSessionResult(DesktopOperationStatus.Failed, ex.Message);
            }
        } finally {
            IMediaSessionBackend? retired = null;
            lock (_sync) {
                _active = false;
                if (_disposed) {
                    retired = _backend;
                    _backend = null;
                }
            }
            Retire(retired);
            _operations.Release();
        }
    }

    private static async Task<byte[]?> ReadArtworkAsync(Stream? stream) {
        if (stream == null) {
            return null;
        }
        try {
            if (stream.CanSeek && stream.Length > MaximumArtworkBytes) {
                return null;
            }
            using var copy = new MemoryStream();
            var buffer = new byte[81920];
            while (true) {
                int count = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, MaximumArtworkBytes - (int)copy.Length + 1)).ConfigureAwait(false);
                if (count == 0) {
                    return copy.Length == 0 ? null : copy.ToArray();
                }
                if (copy.Length + count > MaximumArtworkBytes) {
                    return null;
                }
                copy.Write(buffer, 0, count);
            }
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"Media artwork could not be read: {ex.Message}");
            return null;
        }
    }

    private void OnChanged(object? sender, EventArgs args) {
        EventHandler? handlers;
        lock (_sync) {
            handlers = _disposed ? null : Changed;
        }
        if (handlers == null) {
            return;
        }
        foreach (EventHandler handler in handlers.GetInvocationList()) {
            lock (_sync) {
                if (_disposed) {
                    return;
                }
            }
            try {
                handler(this, EventArgs.Empty);
            } catch (Exception ex) {
                DesktopManagerDiagnostics.Report($"Media notification handler failed: {ex.Message}");
            }
        }
    }

    private static MediaSessionResult DisposedResult() => new(DesktopOperationStatus.Unavailable, "The media service has been disposed.");

    private void Retire(IMediaSessionBackend? backend) {
        if (backend == null) {
            return;
        }
        try {
            backend.Changed -= OnChanged;
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"Media subscription cleanup failed: {ex.Message}");
        } finally {
            try {
                backend.Dispose();
            } catch (Exception ex) {
                DesktopManagerDiagnostics.Report($"Media provider cleanup failed: {ex.Message}");
            }
        }
    }

    /// <summary>Stops observation. An in-flight operation retains its provider until completion and discards its result.</summary>
    public void Dispose() {
        IMediaSessionBackend? retired = null;
        lock (_sync) {
            _disposed = true;
            Changed = null;
            if (!_active) {
                retired = _backend;
                _backend = null;
            }
        }
        Retire(retired);
    }

    private static Task<IMediaSessionBackend?> CreateBackendAsync() {
#if NET8_0_OR_GREATER && WINDOWS
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) {
            return MediaSessionServiceWindowsBackend.CreateAsync();
        }
#endif
        return Task.FromResult<IMediaSessionBackend?>(null);
    }
}
