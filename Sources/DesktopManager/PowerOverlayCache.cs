using System;
using System.Diagnostics;

namespace DesktopManager;

/// <summary>Remembers the effective power mode overlay until something it depends on changes.</summary>
/// <remarks>
/// <c>PowerGetEffectiveOverlayScheme</c> takes around 100 ms. The overlay only changes with the active plan,
/// the power source, or the overlay chosen in Settings, which Windows stores under the power schemes registry
/// key. Callers supply a cheap stamp of those inputs; the overlay is read again when the stamp changes, when no
/// stamp is available, or after <see cref="MaximumAge"/> as a safety net.
/// </remarks>
internal sealed class PowerOverlayCache {
    internal static readonly TimeSpan MaximumAge = TimeSpan.FromMinutes(5);

    private readonly Func<PowerOverlayStamp?> _readStamp;
    private readonly Func<Guid?> _readOverlay;
    private readonly Func<TimeSpan> _clock;
    private readonly object _gate = new();
    private bool _hasValue;
    private Guid _scheme;
    private PowerOverlayStamp _stamp;
    private TimeSpan _readAt;
    private Guid? _overlay;

    internal PowerOverlayCache(Func<PowerOverlayStamp?> readStamp, Func<Guid?> readOverlay, Func<TimeSpan>? clock = null) {
        _readStamp = readStamp ?? throw new ArgumentNullException(nameof(readStamp));
        _readOverlay = readOverlay ?? throw new ArgumentNullException(nameof(readOverlay));
        _clock = clock ?? MonotonicNow;
    }

    internal Guid? Get(Guid activeScheme) {
        PowerOverlayStamp? stamp = _readStamp();
        TimeSpan now = _clock();
        if (stamp.HasValue) {
            lock (_gate) {
                if (_hasValue && _scheme == activeScheme && _stamp.Equals(stamp.Value) && now - _readAt < MaximumAge) {
                    return _overlay;
                }
            }
        }

        // Read outside the lock so a slow Windows call never blocks other callers. The stamp was taken first,
        // so a change racing with this read leaves an older stamp behind and the next call reads again.
        Guid? overlay = _readOverlay();
        if (stamp.HasValue) {
            lock (_gate) {
                _hasValue = true;
                _scheme = activeScheme;
                _stamp = stamp.Value;
                _readAt = now;
                _overlay = overlay;
            }
        }

        return overlay;
    }

    private static TimeSpan MonotonicNow() {
        return TimeSpan.FromSeconds(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
    }
}

/// <summary>The power schemes registry key's last write time and the power source, besides the active plan.</summary>
internal readonly struct PowerOverlayStamp : IEquatable<PowerOverlayStamp> {
    internal PowerOverlayStamp(long schemesLastWriteTime, byte acLineStatus, byte systemStatusFlag) {
        SchemesLastWriteTime = schemesLastWriteTime;
        AcLineStatus = acLineStatus;
        SystemStatusFlag = systemStatusFlag;
    }

    internal long SchemesLastWriteTime { get; }
    internal byte AcLineStatus { get; }
    internal byte SystemStatusFlag { get; }

    public bool Equals(PowerOverlayStamp other) =>
        SchemesLastWriteTime == other.SchemesLastWriteTime
        && AcLineStatus == other.AcLineStatus
        && SystemStatusFlag == other.SystemStatusFlag;

    public override bool Equals(object? obj) => obj is PowerOverlayStamp other && Equals(other);

    public override int GetHashCode() => SchemesLastWriteTime.GetHashCode() ^ (AcLineStatus << 8) ^ SystemStatusFlag;
}
