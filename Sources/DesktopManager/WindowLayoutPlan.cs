using System.Collections.Generic;

namespace DesktopManager;

/// <summary>Describes whether a saved window can be mapped safely to one live window.</summary>
public enum WindowLayoutMatchStatus {
    /// <summary>Exactly one unused live window matches.</summary>
    Matched,
    /// <summary>No eligible live window matches.</summary>
    Missing,
    /// <summary>Several live windows match and require a more specific identity.</summary>
    Ambiguous,
    /// <summary>The saved geometry or identity is invalid.</summary>
    Invalid
}

/// <summary>A saved position and its proposed live target. Planning does not move windows.</summary>
public sealed class WindowLayoutMatch {
    internal WindowLayoutMatch(WindowPosition saved, WindowPosition? window, WindowLayoutMatchStatus status, int candidateCount) {
        SavedWindow = saved;
        Window = window;
        Status = status;
        CandidateCount = candidateCount;
    }
    /// <summary>Gets the saved position.</summary>
    public WindowPosition SavedWindow { get; }
    /// <summary>Gets the unique proposed live target, if one exists.</summary>
    public WindowPosition? Window { get; }
    /// <summary>Gets the matching outcome.</summary>
    public WindowLayoutMatchStatus Status { get; }
    /// <summary>Gets the number of eligible candidates at planning time.</summary>
    public int CandidateCount { get; }
}

/// <summary>Contains the proposed matches for a window layout.</summary>
public sealed class WindowLayoutPlan {
    internal WindowLayoutPlan(IReadOnlyList<WindowLayoutMatch> windows) { Windows = windows; }
    /// <summary>Gets each saved window's proposed target or matching limitation.</summary>
    public IReadOnlyList<WindowLayoutMatch> Windows { get; }
}

/// <summary>Reports one window's layout application, including skipped matches and failures.</summary>
public sealed class WindowLayoutApplyResult {
    internal WindowLayoutApplyResult(WindowLayoutMatch match, bool applied, string? error) {
        Match = match;
        Applied = applied;
        Error = error;
    }
    /// <summary>Gets the matching decision used for this application.</summary>
    public WindowLayoutMatch Match { get; }
    /// <summary>Gets whether the position and state were applied.</summary>
    public bool Applied { get; }
    /// <summary>Gets a failure message, or null for applied and skipped entries.</summary>
    public string? Error { get; }
}
