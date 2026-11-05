using System;
using System.Threading;

namespace DesktopManager;

// Endpoint resolution. Everything below runs on the session's COM thread.
public sealed partial class AudioDefaultEndpointSession {
    private bool Run<T>(Func<IAudioEndpointBackend, T> operation, out T? value, out string reason) {
        if (Volatile.Read(ref _disposed) != 0) {
            throw new ObjectDisposedException(nameof(AudioDefaultEndpointSession));
        }
        T? result = default;
        string unavailable = string.Empty;
        bool disposedMeanwhile = false;
        bool succeeded = _thread.Invoke(() => {
            // Dispose queues its cleanup on this thread too; work queued behind it must not reopen anything.
            if (Volatile.Read(ref _disposed) != 0) {
                disposedMeanwhile = true;
                return false;
            }
            // A failure can mean the default just moved; one reopen reaches the new default before Windows notifies.
            for (int attempt = 0; attempt < 2; attempt++) {
                IAudioEndpointBackend? backend = ResolveBackend(out unavailable);
                if (backend == null) {
                    return false;
                }
                try {
                    result = operation(backend);
                    return true;
                } catch (Exception exception) when (AudioEndpointAvailability.IsUnavailable(exception)) {
                    unavailable = Describe(exception);
                    CloseBackend();
                }
            }
            return false;
        });
        if (disposedMeanwhile) {
            throw new ObjectDisposedException(nameof(AudioDefaultEndpointSession));
        }
        value = result;
        reason = unavailable;
        return succeeded;
    }

    private IAudioEndpointBackend? ResolveBackend(out string reason) {
        reason = string.Empty;
        EnsureSubscribed();
        if (Interlocked.Exchange(ref _defaultChanged, 0) != 0 || _subscription == null) {
            CloseBackend();
        }
        if (_backend != null && !_backend.IsAvailable) {
            CloseBackend();
        }
        if (_backend != null) {
            return _backend;
        }
        IAudioEndpointBackend backend;
        try {
            backend = _open();
        } catch (Exception exception) when (AudioEndpointAvailability.IsUnavailable(exception)) {
            reason = Describe(exception);
            return null;
        }
        if (!backend.IsAvailable) {
            backend.Dispose();
            reason = $"The default {Flow} endpoint for the {Role} role is not active.";
            return null;
        }
        _backend = backend;
        return backend;
    }

    private void EnsureSubscribed() {
        if (_subscription != null) {
            return;
        }
        try {
            _subscription = _subscribe(OnEndpointChanged);
        } catch (Exception exception) when (exception is COMException || exception is UnauthorizedAccessException) {
            // Without notifications, each operation resolves the current default; registration retries on the next call.
            DesktopManagerDiagnostics.Report($"Audio default endpoint notifications are unavailable: {exception.Message}");
            return;
        }
        // Changes made while unsubscribed were missed; resolve the default again.
        Volatile.Write(ref _defaultChanged, 1);
    }

    private void CloseBackend() {
        IAudioEndpointBackend? backend = _backend;
        _backend = null;
        backend?.Dispose();
    }

    private string Describe(Exception exception) {
        return $"No default {Flow} endpoint for the {Role} role is available: {exception.Message} (0x{exception.HResult:X8})";
    }
}
