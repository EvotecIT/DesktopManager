using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

/// <summary>
/// Describes the result of applying a state to one supported Windows radio.
/// </summary>
public sealed class DesktopRadioSetResult {
    /// <summary>Initializes a radio state result.</summary>
    /// <param name="radio">The resulting radio snapshot.</param>
    /// <param name="accessStatus">The permission result returned by Windows.</param>
    /// <param name="accepted">Whether Windows accepted the requested change.</param>
    /// <param name="applied">Whether the effective radio state reached the requested state.</param>
    public DesktopRadioSetResult(
        DesktopRadioInfo radio,
        DesktopRadioAccessStatus accessStatus,
        bool accepted,
        bool applied) {
        Radio = radio ?? throw new ArgumentNullException(nameof(radio));
        AccessStatus = accessStatus;
        Accepted = accepted;
        Applied = applied;
    }

    /// <summary>Gets the resulting radio snapshot.</summary>
    public DesktopRadioInfo Radio { get; }

    /// <summary>Gets the permission result returned by Windows.</summary>
    public DesktopRadioAccessStatus AccessStatus { get; }

    /// <summary>Gets whether Windows accepted the requested change.</summary>
    public bool Accepted { get; }

    /// <summary>Gets whether the effective radio state reached the requested state.</summary>
    public bool Applied { get; }

    /// <summary>Builds the shared failure message for results whose requested states were not applied.</summary>
    /// <param name="results">The supported-radio mutation results to inspect.</param>
    /// <returns>A failure message, or <c>null</c> when every result was applied.</returns>
    internal static string? BuildUnappliedMessage(IReadOnlyList<DesktopRadioSetResult> results) {
        if (results == null) {
            throw new ArgumentNullException(nameof(results));
        }
        DesktopRadioSetResult[] failed = results.Where(item => !item.Applied).ToArray();
        if (failed.Length == 0) {
            return null;
        }

        string details = string.Join(", ", failed.Select(item =>
            $"{item.Radio.Name}: access {item.AccessStatus}, effective {item.Radio.State}"));
        return $"Windows did not apply one or more requested radio states. {details}";
    }
}
