using System;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Serializes process-local Native Wi-Fi connection attempts and drains an unfinished attempt before retrying.
/// </summary>
internal sealed class NativeWifiConnectionCoordinator {
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private NativeWifiConnectionAttempt? _activeAttempt;

    internal Task<bool> WaitForTurnAsync(TimeSpan timeout, CancellationToken cancellationToken) {
        return _connectionGate.WaitAsync(timeout, cancellationToken);
    }

    internal void ReleaseTurn() {
        _connectionGate.Release();
    }

    internal async Task<bool> DrainAsync(TimeSpan timeout, CancellationToken cancellationToken) {
        NativeWifiConnectionAttempt? activeAttempt = Volatile.Read(ref _activeAttempt);
        if (activeAttempt == null) {
            return true;
        }

        using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task timeoutTask = Task.Delay(timeout, waitCancellation.Token);
        Task finished = await Task.WhenAny(activeAttempt.Completion, timeoutTask).ConfigureAwait(false);
        if (finished == activeAttempt.Completion) {
            waitCancellation.Cancel();
            Interlocked.CompareExchange(ref _activeAttempt, null, activeAttempt);
            try {
                await activeAttempt.Completion.ConfigureAwait(false);
            } catch {
                // A settled attempt is drained even when its notification could not be parsed.
            }
            return true;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return false;
    }

    internal NativeWifiConnectionAttempt Begin(DesktopWifiProfileInfo profile) {
        NativeWifiConnectionAttempt? activeAttempt = Volatile.Read(ref _activeAttempt);
        if (activeAttempt != null && activeAttempt.Completion.IsCompleted) {
            Interlocked.CompareExchange(ref _activeAttempt, null, activeAttempt);
            activeAttempt = Volatile.Read(ref _activeAttempt);
        }
        if (activeAttempt != null) {
            throw new InvalidOperationException(
                "A previous Native Wi-Fi connection attempt must finish before another attempt can start.");
        }

        var attempt = new NativeWifiConnectionAttempt(profile);
        attempt.Begin();
        if (Interlocked.CompareExchange(ref _activeAttempt, attempt, null) != null) {
            throw new InvalidOperationException(
                "A previous Native Wi-Fi connection attempt must finish before another attempt can start.");
        }

        return attempt;
    }

    internal void Observe(NativeWifiMethods.WlanNotificationData notification) {
        NativeWifiConnectionAttempt? activeAttempt = Volatile.Read(ref _activeAttempt);
        if (activeAttempt == null) {
            return;
        }

        activeAttempt.Observe(notification);
        if (activeAttempt.Completion.IsCompleted) {
            Interlocked.CompareExchange(ref _activeAttempt, null, activeAttempt);
        }
    }

    internal void Abandon(NativeWifiConnectionAttempt attempt) {
        if (attempt == null) {
            throw new ArgumentNullException(nameof(attempt));
        }

        Interlocked.CompareExchange(ref _activeAttempt, null, attempt);
    }
}
