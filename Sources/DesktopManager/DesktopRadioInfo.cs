using System;

namespace DesktopManager;

/// <summary>
/// Represents one radio exposed by the supported Windows radio API.
/// </summary>
public sealed class DesktopRadioInfo {
    /// <summary>Initializes a radio snapshot.</summary>
    /// <param name="name">The Windows-provided radio name.</param>
    /// <param name="kind">The radio technology.</param>
    /// <param name="state">The current effective state.</param>
    public DesktopRadioInfo(string name, DesktopRadioKind kind, DesktopRadioState state) {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Kind = kind;
        State = state;
    }

    /// <summary>Gets the Windows-provided radio name.</summary>
    public string Name { get; }

    /// <summary>Gets the radio technology.</summary>
    public DesktopRadioKind Kind { get; }

    /// <summary>Gets the effective radio state.</summary>
    public DesktopRadioState State { get; }
}
