using System;
using System.Collections.Generic;

namespace DesktopManager;

/// <summary>
/// Represents bounded plain-text content, selection, caret, and search evidence for a control.
/// </summary>
public sealed class DesktopControlTextObservation {
    /// <summary>Gets or sets the bounded provider text exactly as observed.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets text normalized to line-feed line endings with embedded nulls removed.</summary>
    public string NormalizedValue { get; set; } = string.Empty;

    /// <summary>Gets or sets a display-safe representation that escapes non-printing control characters.</summary>
    public string EscapedValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider source used for the text.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the returned value is a prefix of a longer provider value.</summary>
    public bool IsTruncated { get; set; }

    /// <summary>Gets or sets whether the complete content was available within the configured bound.</summary>
    public bool IsComplete { get; set; }

    /// <summary>Gets or sets whether the requested literal was found.</summary>
    public bool? ContainsExpected { get; set; }

    /// <summary>Gets or sets the literal associated with provider-side containment evidence.</summary>
    public string? ExpectedText { get; set; }

    /// <summary>Gets or sets whether provider-side containment evidence used case-insensitive matching.</summary>
    public bool ExpectedTextIgnoreCase { get; set; }

    /// <summary>Gets or sets whether the provider found the literal outside the returned prefix.</summary>
    public bool MatchFoundBeyondObservedPrefix { get; set; }

    /// <summary>Gets or sets matches whose offsets are known inside the returned prefix.</summary>
    public IReadOnlyList<DesktopTextMatch> Matches { get; set; } = Array.Empty<DesktopTextMatch>();

    /// <summary>Gets or sets the currently selected text ranges.</summary>
    public IReadOnlyList<string> SelectedText { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets selected ranges with offsets when the provider can resolve them safely.</summary>
    public IReadOnlyList<DesktopTextRangeObservation> SelectionRanges { get; set; } = Array.Empty<DesktopTextRangeObservation>();

    /// <summary>Gets or sets whether every provider selection range was returned completely.</summary>
    public bool AreSelectionRangesComplete { get; set; } = true;

    /// <summary>Gets or sets the provider selection mode.</summary>
    public string SupportedSelection { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the caret belongs to the active text control.</summary>
    public bool? IsCaretActive { get; set; }

    /// <summary>Gets or sets bounded text surrounding the caret when available.</summary>
    public string CaretContext { get; set; } = string.Empty;

    /// <summary>Gets or sets the zero-based caret offset, or null when the provider cannot resolve it within the observation bound.</summary>
    public int? CaretOffset { get; set; }

    /// <summary>Gets or sets the active input-method composition text.</summary>
    public string ActiveComposition { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the complete active input-method composition was available.</summary>
    public bool IsActiveCompositionComplete { get; set; } = true;

    /// <summary>Gets or sets the active input-method conversion target.</summary>
    public string ConversionTarget { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the complete input-method conversion target was available.</summary>
    public bool IsConversionTargetComplete { get; set; } = true;

    /// <summary>Gets or sets a SHA-256 fingerprint of complete non-password content.</summary>
    public string ContentFingerprint { get; set; } = string.Empty;

    /// <summary>Gets or sets a SHA-256 token covering complete content plus known selection and caret coordinates.</summary>
    public string EditContextFingerprint { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the value contains non-printing control characters.</summary>
    public bool HasNonPrintingCharacters { get; set; }
}

/// <summary>Represents a bounded text range and its known document coordinates.</summary>
public sealed class DesktopTextRangeObservation {
    /// <summary>Gets or sets the zero-based document offset, or null when it is outside the observation bound.</summary>
    public int? Offset { get; set; }

    /// <summary>Gets or sets the observed range length.</summary>
    public int Length { get; set; }

    /// <summary>Gets or sets the bounded range text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the returned range text is truncated.</summary>
    public bool IsTruncated { get; set; }
}

/// <summary>
/// Describes a text match whose position is known in the returned observation prefix.
/// </summary>
public sealed class DesktopTextMatch {
    /// <summary>Gets or sets the zero-based match offset.</summary>
    public int Offset { get; set; }

    /// <summary>Gets or sets the match length.</summary>
    public int Length { get; set; }

    /// <summary>Gets or sets bounded context surrounding the match.</summary>
    public string Context { get; set; } = string.Empty;
}
