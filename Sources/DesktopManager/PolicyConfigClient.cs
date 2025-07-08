using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Wrapper around Core Audio policy configuration COM interfaces.
/// </summary>
public interface IPolicyConfigClient {
    /// <summary>Sets the default audio endpoint for the specified role.</summary>
    /// <param name="devID">Device identifier.</param>
    /// <param name="role">Audio role.</param>
    void SetDefaultEndpoint(string devID, ERole role);
}

[ComImport]
[Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
internal class _PolicyConfigClient {
}

/// <summary>
/// Implementation using the native policy configuration interfaces.
/// </summary>
public class PolicyConfigClient : IPolicyConfigClient {
    private readonly IPolicyConfig? _policyConfig;
    private readonly IPolicyConfigVista? _policyConfigVista;
    private readonly IPolicyConfig10? _policyConfig10;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyConfigClient"/> class.
    /// </summary>
    public PolicyConfigClient() {
        _policyConfig = new _PolicyConfigClient() as IPolicyConfig;
        if (_policyConfig != null) {
            return;
        }

        _policyConfigVista = new _PolicyConfigClient() as IPolicyConfigVista;
        if (_policyConfigVista != null) {
            return;
        }

        _policyConfig10 = new _PolicyConfigClient() as IPolicyConfig10;
    }

    /// <inheritdoc/>
    public void SetDefaultEndpoint(string devID, ERole role) {
        if (_policyConfig != null) {
            Marshal.ThrowExceptionForHR(_policyConfig.SetDefaultEndpoint(devID, role));
            return;
        }
        if (_policyConfigVista != null) {
            Marshal.ThrowExceptionForHR(_policyConfigVista.SetDefaultEndpoint(devID, role));
            return;
        }
        if (_policyConfig10 != null) {
            Marshal.ThrowExceptionForHR(_policyConfig10.SetDefaultEndpoint(devID, role));
        }
    }
}
