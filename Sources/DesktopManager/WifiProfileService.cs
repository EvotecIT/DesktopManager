using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Enumerates saved Windows Wi-Fi profiles and connects an exact saved profile without scanning nearby networks.
/// </summary>
[SupportedOSPlatform("windows6.0.6000.0")]
public sealed class WifiProfileService : IDisposable {
    private static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaximumConnectionTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
    private readonly IWifiProfileApi _api;
    private bool _disposed;

    /// <summary>Initializes a Native Wi-Fi profile service.</summary>
    public WifiProfileService()
        : this(new NativeWifiProfileApi()) {
    }

    internal WifiProfileService(IWifiProfileApi api) {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>
    /// Gets wireless LAN interfaces without querying location-sensitive current-connection details.
    /// </summary>
    /// <returns>The wireless LAN interface snapshots.</returns>
    public IReadOnlyList<DesktopWifiInterfaceInfo> GetInterfaces() {
        ThrowIfDisposed();
        return _api.GetInterfaces();
    }

    /// <summary>
    /// Gets saved Windows Wi-Fi profiles without scanning for nearby networks or returning profile credentials.
    /// </summary>
    /// <param name="interfaceId">An optional exact wireless LAN interface identifier.</param>
    /// <returns>The saved profile snapshots.</returns>
    public IReadOnlyList<DesktopWifiProfileInfo> GetProfiles(Guid? interfaceId = null) {
        ThrowIfDisposed();
        DesktopWifiInterfaceInfo[] interfaces = SelectInterfaces(interfaceId);
        return interfaces.SelectMany(_api.GetProfiles).ToArray();
    }

    /// <summary>
    /// Connects an exact saved Windows Wi-Fi profile without scanning for nearby networks.
    /// </summary>
    /// <param name="profileName">The case-sensitive saved Windows profile name.</param>
    /// <param name="interfaceId">An optional interface identifier used to disambiguate the same profile on multiple adapters.</param>
    /// <param name="timeout">How long to wait for exclusive access and a Windows ACM completion notification. The default is 30 seconds and the maximum is 2147483647 milliseconds. The Windows attempt can continue after a timeout, and a later same-process call waits for it to finish before starting another attempt. If Windows never reports completion, the retained notification handle is released after two minutes and later connection attempts require restarting the hosting process.</param>
    /// <param name="cancellationToken">Cancels waiting for completion; it does not cancel an attempt already accepted by Windows.</param>
    /// <returns>The observed connection result.</returns>
    public Task<DesktopWifiConnectionResult> ConnectProfileAsync(
        string profileName,
        Guid? interfaceId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(profileName)) {
            throw new ArgumentException("A saved Wi-Fi profile name is required.", nameof(profileName));
        }

        TimeSpan effectiveTimeout = timeout ?? DefaultConnectionTimeout;
        if (effectiveTimeout <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The connection timeout must be greater than zero.");
        }
        if (effectiveTimeout > MaximumConnectionTimeout) {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout,
                $"The connection timeout cannot exceed {MaximumConnectionTimeout}.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        DesktopWifiProfileInfo[] matches = GetProfiles(interfaceId)
            .Where(profile => string.Equals(profile.Name, profileName, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length == 0) {
            throw new InvalidOperationException(BuildMissingProfileMessage(profileName, interfaceId));
        }
        if (matches.Length > 1) {
            throw new InvalidOperationException(
                $"Saved Wi-Fi profile '{profileName}' exists on multiple interfaces. Specify an interface identifier.");
        }

        return _api.ConnectProfileAsync(matches[0], effectiveTimeout, cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _api.Dispose();
        _disposed = true;
    }

    private DesktopWifiInterfaceInfo[] SelectInterfaces(Guid? interfaceId) {
        DesktopWifiInterfaceInfo[] interfaces = _api.GetInterfaces()
            .Where(item => !interfaceId.HasValue || item.InterfaceId == interfaceId.Value)
            .ToArray();
        if (interfaceId.HasValue && interfaces.Length == 0) {
            throw new InvalidOperationException($"Wireless LAN interface '{interfaceId.Value}' was not found.");
        }

        return interfaces;
    }

    private static string BuildMissingProfileMessage(string profileName, Guid? interfaceId) {
        return interfaceId.HasValue
            ? $"Saved Wi-Fi profile '{profileName}' was not found on interface '{interfaceId.Value}'."
            : $"Saved Wi-Fi profile '{profileName}' was not found.";
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(WifiProfileService));
        }
    }
}
