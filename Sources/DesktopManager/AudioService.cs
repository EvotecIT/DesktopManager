using System;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Service to manage system audio devices.
/// </summary>
public class AudioService {
    private readonly IPolicyConfigClient _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioService"/> class.
    /// </summary>
    /// <param name="policy">Optional policy configuration client.</param>
    public AudioService(IPolicyConfigClient? policy = null) {
        _policy = policy ?? new PolicyConfigClient();
    }

    /// <summary>
    /// Sets the default audio device for all roles.
    /// </summary>
    /// <param name="deviceId">Identifier of the device.</param>
    [SupportedOSPlatform("windows")]
    public void SetDefaultAudioDevice(string deviceId) {
        if (deviceId == null) {
            throw new ArgumentNullException(nameof(deviceId));
        }

        _policy.SetDefaultEndpoint(deviceId, ERole.eConsole);
        _policy.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
        _policy.SetDefaultEndpoint(deviceId, ERole.eCommunications);
    }
}
