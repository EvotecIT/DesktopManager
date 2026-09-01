#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Preserves the radio API on the portable target without embedding legacy WinRT metadata.
/// </summary>
[SupportedOSPlatform("windows10.0.14393.0")]
public sealed class RadioService : IDisposable {
    private bool _disposed;

    /// <summary>Raised when an observed Windows radio changes state.</summary>
    public event EventHandler<DesktopRadioStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Gets a current snapshot of all radios exposed to this process.
    /// </summary>
    /// <param name="cancellationToken">A token checked before reporting platform support.</param>
    /// <returns>A task that reports that the portable target cannot access WinRT radios.</returns>
    public async Task<IReadOnlyList<DesktopRadioInfo>> GetRadiosAsync(CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        throw CreatePlatformException();
    }

    /// <summary>
    /// Applies an explicit state to radios matching a kind and optional Windows-provided name.
    /// </summary>
    /// <param name="kind">The radio technology to select.</param>
    /// <param name="state">The explicit On or Off state to request.</param>
    /// <param name="name">An optional exact radio name.</param>
    /// <param name="cancellationToken">A token checked before reporting platform support.</param>
    /// <returns>A task that reports that the portable target cannot access WinRT radios.</returns>
    public async Task<IReadOnlyList<DesktopRadioSetResult>> SetRadioStateAsync(
        DesktopRadioKind kind,
        DesktopRadioState state,
        string? name = null,
        CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        if (state != DesktopRadioState.On && state != DesktopRadioState.Off) {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Only On and Off can be requested.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        throw CreatePlatformException();
    }

    /// <summary>Reports that radio observation requires a Windows-specific target.</summary>
    /// <param name="cancellationToken">A token checked before reporting platform support.</param>
    /// <returns>A task that reports that the portable target cannot access WinRT radios.</returns>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        throw CreatePlatformException();
    }

    /// <summary>Stops radio state observation.</summary>
    public void StopMonitoring() {
    }

    /// <inheritdoc/>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
    }

    /// <summary>Notifies each radio-state subscriber independently so one host callback cannot block the others.</summary>
    internal void NotifyStateChanged(DesktopRadioStateChangedEventArgs args) {
        EventHandler<DesktopRadioStateChangedEventArgs>? handlers = StateChanged;
        if (handlers == null) {
            return;
        }

        foreach (EventHandler<DesktopRadioStateChangedEventArgs> handler in handlers.GetInvocationList()) {
            try {
                handler(this, args);
            } catch (Exception ex) {
                DesktopManagerDiagnostics.Report($"Desktop radio notification handler failed: {ex.Message}");
            }
        }
    }

    private static PlatformNotSupportedException CreatePlatformException() {
        return new PlatformNotSupportedException(
            "Windows radio APIs require a Windows-specific DesktopManager target, such as net8.0-windows10.0.19041.0.");
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(RadioService));
        }
    }
}
#endif
