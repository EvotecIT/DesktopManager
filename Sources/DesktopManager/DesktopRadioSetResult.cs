using System;

namespace DesktopManager;

/// <summary>
/// Describes the result of applying a state to one supported Windows radio.
/// </summary>
public sealed class DesktopRadioSetResult {
    /// <summary>Initializes a radio state result.</summary>
    /// <param name="radio">The resulting radio snapshot.</param>
    /// <param name="accessStatus">The permission result returned by Windows.</param>
    /// <param name="accepted">Whether Windows accepted the requested change.</param>
    public DesktopRadioSetResult(DesktopRadioInfo radio, DesktopRadioAccessStatus accessStatus, bool accepted) {
        Radio = radio ?? throw new ArgumentNullException(nameof(radio));
        AccessStatus = accessStatus;
        Accepted = accepted;
    }

    /// <summary>Gets the resulting radio snapshot.</summary>
    public DesktopRadioInfo Radio { get; }

    /// <summary>Gets the permission result returned by Windows.</summary>
    public DesktopRadioAccessStatus AccessStatus { get; }

    /// <summary>Gets whether Windows accepted the requested change.</summary>
    public bool Accepted { get; }
}
