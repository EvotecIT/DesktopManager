using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DesktopManager;

internal static class DesktopTextObservationBuilder {
    internal static DesktopControlTextObservation CreateRestricted(string source) {
        DesktopControlTextObservation observation = Create(
            string.Empty,
            source,
            isTruncated: false,
            expectedText: null,
            ignoreCase: false,
            maxMatches: 0,
            contextLength: 0);
        observation.IsComplete = false;
        observation.ContentFingerprint = string.Empty;
        return observation;
    }

    internal static DesktopControlTextObservation Create(
        string? value,
        string? source,
        bool isTruncated,
        string? expectedText,
        bool ignoreCase,
        int maxMatches,
        int contextLength,
        bool? containsExpected = null) {
        string observedValue = value ?? string.Empty;
        StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        IReadOnlyList<DesktopTextMatch> matches = FindMatches(observedValue, expectedText, comparison, maxMatches, contextLength);
        bool visibleContainsExpected = !string.IsNullOrEmpty(expectedText) &&
            observedValue.IndexOf(expectedText!, comparison) >= 0;
        bool? resolvedContainsExpected = string.IsNullOrEmpty(expectedText)
            ? null
            : containsExpected == true || visibleContainsExpected
                ? true
                : null;

        return new DesktopControlTextObservation {
            Value = observedValue,
            NormalizedValue = Normalize(observedValue),
            EscapedValue = EscapeNonPrinting(observedValue),
            Source = source ?? string.Empty,
            IsTruncated = isTruncated,
            IsComplete = !isTruncated,
            ContainsExpected = resolvedContainsExpected,
            ExpectedText = expectedText,
            ExpectedTextIgnoreCase = ignoreCase,
            MatchFoundBeyondObservedPrefix = containsExpected == true && !visibleContainsExpected,
            Matches = matches,
            ContentFingerprint = isTruncated ? string.Empty : CreateFingerprint(observedValue),
            HasNonPrintingCharacters = HasNonPrintingCharacters(observedValue)
        };
    }

    internal static string CreateFingerprint(string value) {
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
        var builder = new StringBuilder(hash.Length * 2);
        foreach (byte item in hash) {
            builder.Append(item.ToString("x2"));
        }

        return builder.ToString();
    }

    internal static string CreateEditContextFingerprint(DesktopControlTextObservation observation) {
        if (observation == null) {
            throw new ArgumentNullException(nameof(observation));
        }

        if (!observation.IsComplete || string.IsNullOrWhiteSpace(observation.ContentFingerprint)) {
            return string.Empty;
        }

        var context = new StringBuilder();
        context.Append("content:").Append(observation.ContentFingerprint);
        context.Append("|caret:").Append(observation.CaretOffset.HasValue ? observation.CaretOffset.Value.ToString(CultureInfo.InvariantCulture) : "-");
        context.Append("|selections:").Append(observation.SelectionRanges.Count);
        foreach (DesktopTextRangeObservation range in observation.SelectionRanges) {
            if (!range.Offset.HasValue || range.IsTruncated) {
                return string.Empty;
            }

            context.Append('|')
                .Append(range.Offset.Value.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(range.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(CreateFingerprint(range.Text));
        }

        context.Append("|composition:").Append(CreateFingerprint(observation.ActiveComposition));
        context.Append("|conversion:").Append(CreateFingerprint(observation.ConversionTarget));

        return CreateFingerprint(context.ToString());
    }

    internal static string Normalize(string value) {
        return (value ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("\0", string.Empty);
    }

    internal static string EscapeNonPrinting(string value) {
        var builder = new StringBuilder(value?.Length ?? 0);
        foreach (char character in value ?? string.Empty) {
            switch (character) {
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\0':
                    builder.Append("\\0");
                    break;
                default:
                    if (char.IsControl(character)) {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4"));
                    } else {
                        builder.Append(character);
                    }
                    break;
            }
        }

        return builder.ToString();
    }

    private static IReadOnlyList<DesktopTextMatch> FindMatches(
        string value,
        string? expectedText,
        StringComparison comparison,
        int maxMatches,
        int contextLength) {
        if (string.IsNullOrEmpty(expectedText) || maxMatches <= 0) {
            return Array.Empty<DesktopTextMatch>();
        }

        var matches = new List<DesktopTextMatch>();
        int searchOffset = 0;
        while (searchOffset <= value.Length - expectedText!.Length && matches.Count < maxMatches) {
            int offset = value.IndexOf(expectedText, searchOffset, comparison);
            if (offset < 0) {
                break;
            }

            int contextStart = Math.Max(0, offset - contextLength);
            int contextEnd = Math.Min(value.Length, offset + expectedText.Length + contextLength);
            matches.Add(new DesktopTextMatch {
                Offset = offset,
                Length = expectedText.Length,
                Context = value.Substring(contextStart, contextEnd - contextStart)
            });
            searchOffset = offset + Math.Max(1, expectedText.Length);
        }

        return matches;
    }

    private static bool HasNonPrintingCharacters(string value) {
        foreach (char character in value ?? string.Empty) {
            if (char.IsControl(character) && character != '\r' && character != '\n' && character != '\t') {
                return true;
            }
        }

        return false;
    }
}
