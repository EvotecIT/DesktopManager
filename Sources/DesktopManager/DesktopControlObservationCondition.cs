using System;

namespace DesktopManager;

/// <summary>
/// Defines semantic state that must be satisfied by a generic control observation.
/// </summary>
public sealed class DesktopControlObservationCondition {
    /// <summary>Gets or sets literal text that must be present.</summary>
    public string? ExpectedText { get; set; }

    /// <summary>Gets or sets whether text matching ignores case.</summary>
    public bool IgnoreCase { get; set; }

    /// <summary>Gets or sets whether complete text is required.</summary>
    public bool? IsTextComplete { get; set; }

    /// <summary>Gets or sets whether text truncated by the configured observation limit is required.</summary>
    public bool? IsTextTruncated { get; set; }

    /// <summary>Gets or sets the required enabled state.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets the required focused state.</summary>
    public bool? IsFocused { get; set; }

    /// <summary>Gets or sets the required checked state.</summary>
    public bool? IsChecked { get; set; }

    /// <summary>Gets or sets the required selection-item state.</summary>
    public bool? IsSelected { get; set; }

    /// <summary>Gets or sets the required expand/collapse state.</summary>
    public string? ExpandCollapseState { get; set; }

    /// <summary>Gets or sets the minimum acceptable numeric range value.</summary>
    public double? MinimumRangeValue { get; set; }

    /// <summary>Gets or sets the maximum acceptable numeric range value.</summary>
    public double? MaximumRangeValue { get; set; }

    /// <summary>Determines whether an observation satisfies this condition.</summary>
    public bool Matches(DesktopControlObservation observation) {
        if (observation == null) {
            throw new ArgumentNullException(nameof(observation));
        }

        if (!string.IsNullOrEmpty(ExpectedText)) {
            StringComparison comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            bool visibleMatch = observation.Text.Value.IndexOf(ExpectedText!, comparison) >= 0;
            bool matchingProviderEvidence = observation.Text.ContainsExpected == true &&
                observation.Text.ExpectedTextIgnoreCase == IgnoreCase &&
                string.Equals(observation.Text.ExpectedText, ExpectedText, comparison);
            if (!visibleMatch && !matchingProviderEvidence) {
                return false;
            }
        }

        return Matches(IsTextComplete, observation.Text.IsComplete) &&
            Matches(IsTextTruncated, observation.Text.IsTruncated) &&
            Matches(IsEnabled, observation.IsEnabled) &&
            Matches(IsFocused, observation.IsFocused) &&
            Matches(IsChecked, observation.IsChecked) &&
            Matches(IsSelected, observation.Selection.IsSelected) &&
            (string.IsNullOrWhiteSpace(ExpandCollapseState) || string.Equals(ExpandCollapseState, observation.ExpandCollapseState, StringComparison.OrdinalIgnoreCase)) &&
            (!MinimumRangeValue.HasValue || observation.Range.Value >= MinimumRangeValue) &&
            (!MaximumRangeValue.HasValue || observation.Range.Value <= MaximumRangeValue);
    }

    private static bool Matches(bool? expected, bool? actual) {
        return !expected.HasValue || actual.HasValue && expected.Value == actual.Value;
    }
}
