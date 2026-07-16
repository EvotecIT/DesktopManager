using System;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Observes meaningful changes to the current interactive session without polling idle-time changes.
/// </summary>
public sealed class DesktopSessionWatcher : IDisposable {
    private readonly DesktopSessionService _service;
    private readonly Timer _timer;
    private readonly object _sync = new();
    private DesktopSessionInfo _current;
    private bool _disposed;

    /// <summary>Initializes and starts a session watcher.</summary>
    /// <param name="pollInterval">The state refresh interval. The default is two seconds.</param>
    public DesktopSessionWatcher(TimeSpan? pollInterval = null)
        : this(new DesktopSessionService(), pollInterval) {
    }

    internal DesktopSessionWatcher(DesktopSessionService service, TimeSpan? pollInterval = null) {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        TimeSpan interval = pollInterval ?? TimeSpan.FromSeconds(2);
        if (interval <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(pollInterval));
        }

        _current = _service.GetCurrentSession();
        _timer = new Timer(Poll, null, interval, interval);
    }

    /// <summary>Raised when session identity, connection, protocol, remote, or lock state changes.</summary>
    public event EventHandler<DesktopSessionChangedEventArgs>? Changed;

    /// <summary>Gets the most recently observed session snapshot.</summary>
    public DesktopSessionInfo Current {
        get {
            lock (_sync) {
                return _current;
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _timer.Dispose();
        _disposed = true;
    }

    internal static bool HasMeaningfulChange(DesktopSessionInfo previous, DesktopSessionInfo current) {
        return previous.SessionId != current.SessionId ||
            !string.Equals(previous.UserName, current.UserName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(previous.DomainName, current.DomainName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(previous.ClientName, current.ClientName, StringComparison.OrdinalIgnoreCase) ||
            previous.ConnectState != current.ConnectState ||
            previous.Protocol != current.Protocol ||
            previous.IsRemote != current.IsRemote ||
            previous.IsLocked != current.IsLocked;
    }

    private void Poll(object? state) {
        DesktopSessionInfo next;
        try {
            next = _service.GetCurrentSession();
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"Desktop session polling failed: {ex.Message}");
            return;
        }

        DesktopSessionInfo previous;
        lock (_sync) {
            previous = _current;
            _current = next;
        }

        if (HasMeaningfulChange(previous, next)) {
            Changed?.Invoke(this, new DesktopSessionChangedEventArgs(previous, next));
        }
    }
}
