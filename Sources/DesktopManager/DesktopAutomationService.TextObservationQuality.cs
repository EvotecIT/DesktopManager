using System;

namespace DesktopManager;

public sealed partial class DesktopAutomationService {
    internal static DesktopWindowTextObservation? SelectBetterTextObservation(
        DesktopWindowTextObservation? current,
        DesktopWindowTextObservation? candidate) {
        if (candidate == null) {
            return current;
        }

        if (current == null) {
            return candidate;
        }

        int currentScore = GetTextObservationQuality(current);
        int candidateScore = GetTextObservationQuality(candidate);
        return candidateScore >= currentScore ? candidate : current;
    }

    private static int GetTextObservationQuality(DesktopWindowTextObservation observation) {
        int score = observation.ContainsExpected == true ? 1000 : 0;
        if (!observation.IsTruncated) {
            score += 100;
        }

        if (observation.ControlHandle != IntPtr.Zero) {
            score += 20;
        }

        if (!string.Equals(observation.Source, "window.title", StringComparison.OrdinalIgnoreCase)) {
            score += 10;
        }

        score += Math.Min(observation.Value?.Length ?? 0, 9);
        return score;
    }
}
