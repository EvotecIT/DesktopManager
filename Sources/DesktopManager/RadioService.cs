using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Radios;

namespace DesktopManager;

/// <summary>
/// Enumerates, controls, and observes individual radios through the supported Windows radio API.
/// </summary>
[SupportedOSPlatform("windows10.0.14393.0")]
public sealed class RadioService : IDisposable {
    private readonly List<Radio> _observedRadios = new();
    private bool _disposed;

    /// <summary>Raised when an observed Windows radio changes state.</summary>
    public event EventHandler<DesktopRadioStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Gets a current snapshot of all radios exposed to this process.
    /// </summary>
    /// <param name="cancellationToken">A token checked before and after the Windows operation.</param>
    /// <returns>The current radio snapshots.</returns>
    public async Task<IReadOnlyList<DesktopRadioInfo>> GetRadiosAsync(CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return radios.Select(ToInfo).ToArray();
    }

    /// <summary>
    /// Applies an explicit state to radios matching a kind and optional Windows-provided name.
    /// When no name is supplied, every radio of the requested kind is changed.
    /// </summary>
    /// <param name="kind">The radio technology to select.</param>
    /// <param name="state">The explicit On or Off state to request.</param>
    /// <param name="name">An optional exact radio name, compared case-insensitively.</param>
    /// <param name="cancellationToken">A token checked between Windows operations.</param>
    /// <returns>One result for each matching radio.</returns>
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
        IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
        Radio[] matches = radios
            .Where(radio => ToKind(radio.Kind) == kind)
            .Where(radio => string.IsNullOrWhiteSpace(name) || string.Equals(radio.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 0) {
            throw new InvalidOperationException(BuildNoMatchMessage(kind, name));
        }

        RadioAccessStatus access = await Radio.RequestAccessAsync();
        DesktopRadioAccessStatus mappedAccess = ToAccessStatus(access);
        var results = new List<DesktopRadioSetResult>(matches.Length);
        foreach (Radio radio in matches) {
            cancellationToken.ThrowIfCancellationRequested();
            bool accepted = false;
            DesktopRadioAccessStatus operationAccess = mappedAccess;
            if (access == RadioAccessStatus.Allowed) {
                RadioAccessStatus setStatus = await radio.SetStateAsync(ToWindowsState(state));
                operationAccess = ToAccessStatus(setStatus);
                accepted = setStatus == RadioAccessStatus.Allowed;
            }

            results.Add(new DesktopRadioSetResult(ToInfo(radio), operationAccess, accepted));
        }

        return results.ToArray();
    }

    /// <summary>
    /// Starts observing the radios currently exposed to this process.
    /// Call this method again after device arrival or removal to refresh subscriptions.
    /// </summary>
    /// <param name="cancellationToken">A token checked before and after enumeration.</param>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        StopMonitoring();
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Radio radio in radios) {
            radio.StateChanged += HandleStateChanged;
            _observedRadios.Add(radio);
        }
    }

    /// <summary>Stops radio state observation without disposing the service.</summary>
    public void StopMonitoring() {
        foreach (Radio radio in _observedRadios) {
            radio.StateChanged -= HandleStateChanged;
        }
        _observedRadios.Clear();
    }

    /// <inheritdoc/>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        StopMonitoring();
        _disposed = true;
    }

    internal static DesktopRadioKind ToKind(RadioKind kind) {
        return kind switch {
            RadioKind.WiFi => DesktopRadioKind.WiFi,
            RadioKind.MobileBroadband => DesktopRadioKind.MobileBroadband,
            RadioKind.Bluetooth => DesktopRadioKind.Bluetooth,
            RadioKind.FM => DesktopRadioKind.FM,
            _ => DesktopRadioKind.Other
        };
    }

    internal static DesktopRadioState ToState(RadioState state) {
        return state switch {
            RadioState.On => DesktopRadioState.On,
            RadioState.Off => DesktopRadioState.Off,
            RadioState.Disabled => DesktopRadioState.Disabled,
            _ => DesktopRadioState.Unknown
        };
    }

    internal static DesktopRadioAccessStatus ToAccessStatus(RadioAccessStatus status) {
        return status switch {
            RadioAccessStatus.Allowed => DesktopRadioAccessStatus.Allowed,
            RadioAccessStatus.DeniedByUser => DesktopRadioAccessStatus.DeniedByUser,
            RadioAccessStatus.DeniedBySystem => DesktopRadioAccessStatus.DeniedBySystem,
            _ => DesktopRadioAccessStatus.Unspecified
        };
    }

    private static RadioState ToWindowsState(DesktopRadioState state) {
        return state == DesktopRadioState.On ? RadioState.On : RadioState.Off;
    }

    private static DesktopRadioInfo ToInfo(Radio radio) {
        return new DesktopRadioInfo(radio.Name, ToKind(radio.Kind), ToState(radio.State));
    }

    private static string BuildNoMatchMessage(DesktopRadioKind kind, string? name) {
        return string.IsNullOrWhiteSpace(name)
            ? $"No {kind} radios were found."
            : $"No {kind} radio named '{name}' was found.";
    }

    private void HandleStateChanged(Radio sender, object args) {
        StateChanged?.Invoke(this, new DesktopRadioStateChangedEventArgs(ToInfo(sender)));
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(RadioService));
        }
    }
}
