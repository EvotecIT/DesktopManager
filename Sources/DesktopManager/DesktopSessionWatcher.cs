using System;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Observes meaningful changes to the current interactive session without polling idle-time changes.
/// </summary>
public sealed class DesktopSessionWatcher : IDisposable {
    [ThreadStatic]
    private static DesktopSessionWatcher? _activePollWatcher;

    private readonly Func<DesktopSessionInfo> _getCurrentSession;
    private readonly Timer _timer;
    private readonly object _sync = new();
    private DesktopSessionInfo _current;
    private int _activePolls;
    private bool _disposed;
    private bool _disposeCompleted;

    /// <summary>Initializes and starts a session watcher.</summary>
    /// <param name="pollInterval">The state refresh interval. The default is two seconds.</param>
    public DesktopSessionWatcher(TimeSpan? pollInterval = null)
        : this(new DesktopSessionService().GetCurrentSession, pollInterval) {
    }

    internal DesktopSessionWatcher(Func<DesktopSessionInfo> getCurrentSession, TimeSpan? pollInterval = null) {
        _getCurrentSession = getCurrentSession ?? throw new ArgumentNullException(nameof(getCurrentSession));
        TimeSpan interval = pollInterval ?? TimeSpan.FromSeconds(2);
        if (interval <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(pollInterval));
        }

        _current = _getCurrentSession();
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
        lock (_sync) {
            if (!_disposed) {
                _disposed = true;
                _timer.Dispose();
            }

            if (ReferenceEquals(_activePollWatcher, this)) {
                return;
            }

            while (!_disposeCompleted) {
                if (_activePolls == 0) {
                    _disposeCompleted = true;
                    System.Threading.Monitor.PulseAll(_sync);
                    break;
                }

                System.Threading.Monitor.Wait(_sync);
            }
        }
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
        lock (_sync) {
            if (_disposed) {
                return;
            }

            _activePolls++;
        }

        DesktopSessionWatcher? previousActiveWatcher = _activePollWatcher;
        _activePollWatcher = this;
        try {
            DesktopSessionInfo next;
            try {
                next = _getCurrentSession();
            } catch (Exception ex) {
                DesktopManagerDiagnostics.Report($"Desktop session polling failed: {ex.Message}");
                return;
            }

            EventHandler<DesktopSessionChangedEventArgs>? handler = null;
            DesktopSessionChangedEventArgs? args = null;
            lock (_sync) {
                if (_disposed) {
                    return;
                }

                DesktopSessionInfo previous = _current;
                _current = next;

                if (HasMeaningfulChange(previous, next)) {
                    handler = Changed;
                    args = new DesktopSessionChangedEventArgs(previous, next);
                }
            }

            handler?.Invoke(this, args!);
        } finally {
            _activePollWatcher = previousActiveWatcher;
            lock (_sync) {
                _activePolls--;
                if (_disposed && _activePolls == 0) {
                    _disposeCompleted = true;
                    System.Threading.Monitor.PulseAll(_sync);
                }
            }
        }
    }
}
