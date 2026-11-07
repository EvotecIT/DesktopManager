using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager;

/// <summary>Remembers each direction's default endpoint IDs until Core Audio reports an endpoint change.</summary>
/// <remarks>
/// Asking Core Audio for the three default roles costs roughly 10 to 35 ms, while the defaults change only when
/// a device is added, removed, changes state or is made default. An endpoint watcher invalidates the cache
/// synchronously from its callback. Without a watcher every call queries again, and entries also expire after
/// <see cref="MaximumAge"/> in case Windows drops the registration, for example when the audio service restarts.
/// </remarks>
internal sealed class AudioDefaultEndpointCache {
    internal static readonly TimeSpan MaximumAge = TimeSpan.FromSeconds(30);

    private readonly Func<Action<AudioEndpointChangeKind>, IDisposable> _subscribe;
    private readonly Func<TimeSpan> _clock;
    private readonly object _gate = new();
    private readonly Entry?[] _entries = new Entry?[2];
    private IDisposable? _subscription;
    private int _subscriptionState;
    private int _version;

    internal AudioDefaultEndpointCache(Func<Action<AudioEndpointChangeKind>, IDisposable> subscribe, Func<TimeSpan>? clock = null) {
        _subscribe = subscribe ?? throw new ArgumentNullException(nameof(subscribe));
        _clock = clock ?? MonotonicNow;
    }

    internal IReadOnlyDictionary<AudioRole, string> Get(AudioDataFlow dataFlow, Func<IReadOnlyDictionary<AudioRole, string>> query) {
        if (!EnsureSubscribed()) {
            return query();
        }

        int index = dataFlow == AudioDataFlow.Capture ? 1 : 0;
        int version = Volatile.Read(ref _version);
        TimeSpan now = _clock();
        lock (_gate) {
            Entry? entry = _entries[index];
            if (entry != null && entry.Version == version && now - entry.ReadAt < MaximumAge) {
                return entry.Defaults;
            }
        }

        IReadOnlyDictionary<AudioRole, string> defaults = query();
        lock (_gate) {
            // A change reported while querying makes this answer suspect; keep it for this caller only.
            if (Volatile.Read(ref _version) == version) {
                _entries[index] = new Entry(version, now, defaults);
            }
        }
        return defaults;
    }

    internal void OnEndpointChanged(AudioEndpointChangeKind kind) {
        if (kind != AudioEndpointChangeKind.PropertyChanged) {
            Interlocked.Increment(ref _version);
        }
    }

    private bool EnsureSubscribed() {
        int state = Volatile.Read(ref _subscriptionState);
        if (state != 0) {
            return state > 0;
        }

        lock (_gate) {
            if (_subscriptionState == 0) {
                try {
                    _subscription = _subscribe(OnEndpointChanged);
                    Volatile.Write(ref _subscriptionState, 1);
                } catch (Exception ex) when (ex is COMException || ex is InvalidCastException || ex is UnauthorizedAccessException) {
                    DesktopManagerDiagnostics.Report($"Audio endpoint notifications are unavailable, so default endpoints are not cached: {ex.Message}");
                    Volatile.Write(ref _subscriptionState, -1);
                }
            }
            return _subscriptionState > 0;
        }
    }

    private static TimeSpan MonotonicNow() {
        return TimeSpan.FromSeconds(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
    }

    private sealed class Entry {
        internal Entry(int version, TimeSpan readAt, IReadOnlyDictionary<AudioRole, string> defaults) {
            Version = version;
            ReadAt = readAt;
            Defaults = defaults;
        }

        internal int Version { get; }
        internal TimeSpan ReadAt { get; }
        internal IReadOnlyDictionary<AudioRole, string> Defaults { get; }
    }
}
