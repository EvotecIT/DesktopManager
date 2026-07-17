using System;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Serializes process-local Native Wi-Fi connection attempts and drains an unfinished attempt before retrying.
/// </summary>
internal sealed class NativeWifiConnectionCoordinator {
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private readonly object _sync = new();
    private NativeWifiConnectionAttempt? _activeAttempt;
    private string? _poisonReason;

    internal Task<bool> WaitForTurnAsync(TimeSpan timeout, CancellationToken cancellationToken) {
        return _connectionGate.WaitAsync(timeout, cancellationToken);
    }

    internal void ReleaseTurn() {
        _connectionGate.Release();
    }

    internal async Task<bool> DrainAsync(TimeSpan timeout, CancellationToken cancellationToken) {
        NativeWifiConnectionAttempt? activeAttempt;
        lock (_sync) {
            ThrowIfPoisoned();
            activeAttempt = _activeAttempt;
        }
        if (activeAttempt == null) {
            return true;
        }

        using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task timeoutTask = Task.Delay(timeout, waitCancellation.Token);
        Task finished = await Task.WhenAny(activeAttempt.Completion, timeoutTask).ConfigureAwait(false);
        if (finished == activeAttempt.Completion) {
            waitCancellation.Cancel();
            try {
                await activeAttempt.Completion.ConfigureAwait(false);
            } catch {
                // A settled attempt is drained even when its notification could not be parsed.
            }
            lock (_sync) {
                if (ReferenceEquals(_activeAttempt, activeAttempt)) {
                    _activeAttempt = null;
                }
                ThrowIfPoisoned();
            }
            return true;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return false;
    }

    internal NativeWifiConnectionAttempt Begin(DesktopWifiProfileInfo profile) {
        lock (_sync) {
            ThrowIfPoisoned();
            if (_activeAttempt != null && _activeAttempt.Completion.IsCompleted) {
                _activeAttempt = null;
            }
            if (_activeAttempt != null) {
                throw new InvalidOperationException(
                    "A previous Native Wi-Fi connection attempt must finish before another attempt can start.");
            }

            var attempt = new NativeWifiConnectionAttempt(profile);
            attempt.Begin();
            _activeAttempt = attempt;
            return attempt;
        }
    }

    internal void Observe(NativeWifiMethods.WlanNotificationData notification) {
        lock (_sync) {
            NativeWifiConnectionAttempt? activeAttempt = _activeAttempt;
            if (activeAttempt == null) {
                return;
            }

            activeAttempt.Observe(notification);
            if (activeAttempt.Completion.IsCompleted) {
                _activeAttempt = null;
            }
        }
    }

    internal void Abandon(NativeWifiConnectionAttempt attempt) {
        if (attempt == null) {
            throw new ArgumentNullException(nameof(attempt));
        }

        lock (_sync) {
            if (ReferenceEquals(_activeAttempt, attempt)) {
                _activeAttempt = null;
            }
        }
    }

    internal async Task QuarantineAsync(NativeWifiConnectionAttempt attempt, TimeSpan timeout) {
        if (attempt == null) {
            throw new ArgumentNullException(nameof(attempt));
        }
        if (timeout < TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        await Task.Delay(timeout).ConfigureAwait(false);
        lock (_sync) {
            if (!ReferenceEquals(_activeAttempt, attempt) || attempt.Completion.IsCompleted) {
                return;
            }

            _poisonReason =
                "Windows did not report completion for an earlier Wi-Fi connection attempt. " +
                "The retained notification handle was released; restart the hosting process before connecting another saved profile.";
            _activeAttempt = null;
            attempt.Expire(_poisonReason);
        }
    }

    private void ThrowIfPoisoned() {
        if (_poisonReason != null) {
            throw new InvalidOperationException(_poisonReason);
        }
    }
}
