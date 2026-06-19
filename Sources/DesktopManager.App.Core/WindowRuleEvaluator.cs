namespace DesktopManager.App.Core;

/// <summary>
/// Matches observed windows against saved layout rules and creates reusable placement requests.
/// </summary>
public static class WindowRuleEvaluator {
    /// <summary>
    /// Evaluates the first enabled matching rule in the enabled layout profiles.
    /// </summary>
    /// <param name="layouts">Layout profiles to inspect.</param>
    /// <param name="window">Observed window metadata.</param>
    /// <returns>A rule evaluation result.</returns>
    public static WindowRuleEvaluation Evaluate(
        IEnumerable<WindowLayoutProfileDefinition>? layouts,
        WindowRuleObservation window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (layouts == null) {
            return new WindowRuleEvaluation();
        }

        foreach (WindowLayoutProfileDefinition layout in layouts.Where(layout => layout.Enabled)) {
            foreach (WindowRuleDefinition rule in layout.Rules.Where(rule => rule.Enabled)) {
                if (!Matches(rule.Match, window)) {
                    continue;
                }

                return new WindowRuleEvaluation {
                    Matched = true,
                    Layout = layout,
                    Rule = rule,
                    Request = WindowRulePlacementRequestFactory.Create(rule.Action, window.Handle)
                };
            }
        }

        return new WindowRuleEvaluation();
    }

    /// <summary>
    /// Determines whether a window observation matches rule criteria.
    /// </summary>
    /// <param name="match">Rule match criteria.</param>
    /// <param name="window">Observed window metadata.</param>
    /// <returns>True when all non-empty patterns match.</returns>
    public static bool Matches(WindowRuleMatchDefinition match, WindowRuleObservation window) {
        if (match == null) {
            throw new ArgumentNullException(nameof(match));
        }

        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        return MatchesWildcard(window.Title, match.TitlePattern) &&
            MatchesWildcard(window.ProcessName, match.ProcessNamePattern) &&
            MatchesWildcard(window.ProcessPath, match.ProcessPathPattern);
    }

    private static bool MatchesWildcard(string value, string pattern) {
        if (string.IsNullOrWhiteSpace(pattern) || pattern == "*") {
            return true;
        }

        value ??= string.Empty;
        return MatchesWildcardCore(value.AsSpan(), pattern.Trim().AsSpan());
    }

    private static bool MatchesWildcardCore(ReadOnlySpan<char> value, ReadOnlySpan<char> pattern) {
        int valueIndex = 0;
        int patternIndex = 0;
        int starIndex = -1;
        int matchIndex = 0;

        while (valueIndex < value.Length) {
            if (patternIndex < pattern.Length &&
                (pattern[patternIndex] == '?' ||
                 char.ToUpperInvariant(pattern[patternIndex]) == char.ToUpperInvariant(value[valueIndex]))) {
                valueIndex++;
                patternIndex++;
            } else if (patternIndex < pattern.Length && pattern[patternIndex] == '*') {
                starIndex = patternIndex;
                matchIndex = valueIndex;
                patternIndex++;
            } else if (starIndex != -1) {
                patternIndex = starIndex + 1;
                matchIndex++;
                valueIndex = matchIndex;
            } else {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*') {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }
}
