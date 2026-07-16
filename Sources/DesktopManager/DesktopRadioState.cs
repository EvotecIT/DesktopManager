namespace DesktopManager;

/// <summary>
/// Describes the effective state of a Windows radio.
/// </summary>
public enum DesktopRadioState {
    /// <summary>The state is not recognized by this version of DesktopManager.</summary>
    Unknown = 0,
    /// <summary>The radio is enabled.</summary>
    On = 1,
    /// <summary>The radio is turned off.</summary>
    Off = 2,
    /// <summary>The radio is disabled by hardware, policy, or the operating system.</summary>
    Disabled = 3
}
