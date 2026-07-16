using System;

namespace DesktopManager;

/// <summary>
/// Provides a supported Windows radio state change snapshot.
/// </summary>
public sealed class DesktopRadioStateChangedEventArgs : EventArgs {
    /// <summary>Initializes the event data.</summary>
    /// <param name="radio">The radio snapshot after the change.</param>
    public DesktopRadioStateChangedEventArgs(DesktopRadioInfo radio) {
        Radio = radio ?? throw new ArgumentNullException(nameof(radio));
    }

    /// <summary>Gets the radio snapshot after the change.</summary>
    public DesktopRadioInfo Radio { get; }
}
