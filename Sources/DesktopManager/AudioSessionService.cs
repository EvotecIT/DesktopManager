using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>Reads and controls application audio sessions on the default multimedia render endpoint.</summary>
/// <remarks>
/// Call <see cref="GetApplications"/> to refresh the session cache, then use <see cref="GetPeaks"/>
/// for frequent meter reads without device enumeration. Volume and mute changes affect all cached
/// sessions in the selected application. Refresh to discover new sessions or a changed default endpoint.
/// Serialize calls and disposal on the consumer's COM-initialized thread. No UI filtering is applied.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class AudioSessionService : IDisposable {
    private readonly IAudioSessionBackend _backend;
    private readonly List<IAudioApplicationSession> _sessions = new();
    private readonly Dictionary<string, List<IAudioApplicationSession>> _controls = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>Creates a service. Native sessions are opened by the first refresh.</summary>
    /// <exception cref="PlatformNotSupportedException">The current platform is not Windows.</exception>
    public AudioSessionService() : this(new AudioSessionNativeBackend()) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            throw new PlatformNotSupportedException("Core Audio requires Windows.");
        }
    }

    internal AudioSessionService(IAudioSessionBackend backend) {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    /// <summary>Refreshes cached controls and returns immutable application readings.</summary>
    /// <returns>
    /// Sessions grouped by case-insensitive process name, with system sounds grouped separately.
    /// Active applications sort first, then by name, with system sounds last. Expired or unreadable
    /// sessions and processes whose names cannot be resolved are skipped.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="COMException">The default endpoint or session enumerator cannot be opened.</exception>
    public IReadOnlyList<AudioApplicationInfo> GetApplications() {
        ThrowIfDisposed();
        ReleaseSessions();
        var readings = new Dictionary<string, List<AudioApplicationSessionState>>(StringComparer.OrdinalIgnoreCase);
        try {
            _sessions.AddRange(_backend.OpenSessions());
            for (int index = 0; index < _sessions.Count;) {
                IAudioApplicationSession session = _sessions[index];
                AudioApplicationSessionState? state;
                try {
                    state = session.Read();
                } catch (Exception ex) when (IsSessionUnavailable(ex)) {
                    RemoveSession(index);
                    continue;
                }
                if (state == null || (!state.IsSystem && string.IsNullOrEmpty(state.ProcessName))) {
                    RemoveSession(index);
                    continue;
                }
                string key = state.IsSystem ? "system" : state.ProcessName;
                if (!_controls.TryGetValue(key, out List<IAudioApplicationSession>? controls)) {
                    _controls.Add(key, controls = new List<IAudioApplicationSession>());
                    readings.Add(key, new List<AudioApplicationSessionState>());
                }
                controls.Add(session);
                readings[key].Add(state);
                index++;
            }
            AudioApplicationInfo[] applications = readings.Select(pair => CreateApplication(pair.Key, pair.Value))
                .OrderBy(application => application.IsSystem)
                .ThenByDescending(application => application.IsActive)
                .ThenBy(application => application.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            return Array.AsReadOnly(applications);
        } catch {
            ReleaseSessions();
            throw;
        }
    }

    /// <summary>Reads the loudest peak for each cached application without enumerating devices or sessions.</summary>
    /// <returns>A read-only snapshot, with expired and unreadable sessions removed from the cache.</returns>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    public IReadOnlyDictionary<string, float> GetPeaks() {
        ThrowIfDisposed();
        var peaks = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, List<IAudioApplicationSession>> group in _controls) {
            float peak = 0f;
            VisitSessions(group.Value, session => peak = Math.Max(peak, session.GetPeak()));
            if (group.Value.Count > 0) {
                peaks.Add(group.Key, peak);
            }
        }
        return new ReadOnlyDictionary<string, float>(peaks);
    }

    /// <summary>Sets every cached session in the application to the requested percentage.</summary>
    /// <param name="key">A key returned by <see cref="GetApplications"/>. An absent key has no effect.</param>
    /// <param name="percent">A finite percentage from 0 through 100.</param>
    /// <remarks>Expired sessions are skipped. Native write errors propagate; earlier writes may have completed.</remarks>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="ArgumentException">The application key is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The percentage is outside the supported range.</exception>
    public void SetVolume(string key, float percent) {
        ThrowIfDisposed();
        ValidateKey(key);
        AudioEndpointSession.ValidateVolume(percent);
        if (_controls.TryGetValue(key, out List<IAudioApplicationSession>? controls)) {
            VisitSessions(controls, session => session.SetVolume(percent / 100f), propagateFailure: true);
        }
    }

    /// <summary>Sets every cached session in the application to the explicit mute state.</summary>
    /// <param name="key">A key returned by <see cref="GetApplications"/>. An absent key has no effect.</param>
    /// <param name="muted">The explicit mute state.</param>
    /// <remarks>Expired sessions are skipped. Native write errors propagate; earlier writes may have completed.</remarks>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="ArgumentException">The application key is empty.</exception>
    public void SetMute(string key, bool muted) {
        ThrowIfDisposed();
        ValidateKey(key);
        if (_controls.TryGetValue(key, out List<IAudioApplicationSession>? controls)) {
            VisitSessions(controls, session => session.SetMute(muted), propagateFailure: true);
        }
    }

    /// <summary>Releases cached native sessions. Repeated calls have no effect.</summary>
    public void Dispose() {
        if (!_disposed) {
            _disposed = true;
            ReleaseSessions();
            _backend.Dispose();
        }
    }

    private static AudioApplicationInfo CreateApplication(string key, List<AudioApplicationSessionState> sessions) {
        AudioApplicationSessionState first = sessions.FirstOrDefault(session => session.IsActive) ?? sessions[0];
        string name = first.IsSystem ? "System sounds" :
            !string.IsNullOrWhiteSpace(first.DisplayName) && !first.DisplayName!.StartsWith("@", StringComparison.Ordinal)
                ? first.DisplayName
                : char.ToUpperInvariant(first.ProcessName[0]) + first.ProcessName.Substring(1);
        return new AudioApplicationInfo(key, name, first.ProcessId, sessions.Max(session => session.VolumePercent),
            sessions.All(session => session.IsMuted), sessions.Max(session => session.PeakValue),
            sessions.Any(session => session.IsActive), first.IsSystem);
    }

    private void VisitSessions(List<IAudioApplicationSession> controls, Action<IAudioApplicationSession> action, bool propagateFailure = false) {
        for (int index = controls.Count - 1; index >= 0; index--) {
            IAudioApplicationSession session = controls[index];
            try {
                if (!session.IsExpired) {
                    action(session);
                    continue;
                }
            } catch (Exception ex) when (IsSessionUnavailable(ex)) {
                RemoveControl(controls, index, session);
                if (propagateFailure) {
                    throw;
                }
                continue;
            }
            RemoveControl(controls, index, session);
        }
    }

    private void RemoveControl(List<IAudioApplicationSession> controls, int index, IAudioApplicationSession session) {
        controls.RemoveAt(index);
        _sessions.Remove(session);
        session.Dispose();
    }

    private void RemoveSession(int index) {
        IAudioApplicationSession session = _sessions[index];
        _sessions.RemoveAt(index);
        session.Dispose();
    }

    private void ReleaseSessions() {
        _controls.Clear();
        foreach (IAudioApplicationSession session in _sessions) {
            session.Dispose();
        }
        _sessions.Clear();
    }

    private static bool IsSessionUnavailable(Exception exception) =>
        exception is COMException || exception is InvalidCastException || exception is IOException;

    private static void ValidateKey(string key) {
        if (string.IsNullOrWhiteSpace(key)) {
            throw new ArgumentException("An application key is required.", nameof(key));
        }
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(AudioSessionService));
        }
    }
}
