using System;

namespace DesktopManager;

/// <summary>
/// Describes the observed result of connecting to a saved Windows Wi-Fi profile.
/// </summary>
public sealed class DesktopWifiConnectionResult {
    internal DesktopWifiConnectionResult(
        DesktopWifiProfileInfo profile,
        DesktopWifiConnectionOutcome outcome,
        uint reasonCode,
        string? reason) {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Outcome = outcome;
        ReasonCode = reasonCode;
        Reason = reason;
    }

    /// <summary>Gets the saved profile Windows was asked to connect.</summary>
    public DesktopWifiProfileInfo Profile { get; }

    /// <summary>Gets the observed completion outcome.</summary>
    public DesktopWifiConnectionOutcome Outcome { get; }

    /// <summary>Gets the Windows WLAN reason code, or zero when none was reported.</summary>
    public uint ReasonCode { get; }

    /// <summary>Gets the Windows-provided reason text or a timeout explanation.</summary>
    public string? Reason { get; }

    /// <summary>Gets whether Windows reported a successful completed connection.</summary>
    public bool Succeeded => Outcome == DesktopWifiConnectionOutcome.Connected;

    internal string BuildFailureMessage() {
        string? reason = Reason;
        string detail = reason == null || reason.Trim().Length == 0
            ? $"WLAN reason code {ReasonCode}."
            : reason;
        return Outcome == DesktopWifiConnectionOutcome.TimedOut
            ? $"Timed out waiting for Wi-Fi profile '{Profile.Name}' to finish connecting. {detail}"
            : $"Windows failed to connect Wi-Fi profile '{Profile.Name}'. {detail}";
    }
}
