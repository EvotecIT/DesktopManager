using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

public partial class WindowManager {
    private static readonly string[] TitleSeparators = { " - ", " — ", " – " };

    /// <summary>Plans one-to-one layout matches without moving windows. Ambiguous targets are skipped.</summary>
    /// <remarks>A captured application ID must match a verified live ID. Unavailable identity is not treated as a match.</remarks>
    public WindowLayoutPlan PlanLayout(WindowLayout layout) {
        if (layout == null) { throw new ArgumentNullException(nameof(layout)); }
        return WindowDpiContext.Execute(() => {
            WindowPosition[] current = GetWindows().Select(GetWindowPosition).ToArray();
            if (layout.Windows != null && layout.Windows.Any(window => window != null && !string.IsNullOrEmpty(window.ExplorerFolderPath))) {
                Dictionary<IntPtr, string> folders = WorkspaceExplorerFolders.Read();
                foreach (WindowPosition window in current) {
                    if (folders.TryGetValue(window.Handle, out string? folder)) { window.ExplorerFolderPath = folder; }
                }
            }
            return BuildLayoutPlan(layout, current);
        });
    }

    /// <summary>
    /// Matches saved windows to live ones. Exact matches (same process and title, then same title and application)
    /// are made first for every entry. Entries still unmatched, typically after a reboot or once a browser or editor
    /// has changed its title, then fall back to the same application (process name and window class), narrowed by the
    /// title's application suffix when several windows qualify. A fallback match is made only when the live window is
    /// the sole candidate and no other unmatched entry could also claim it.
    /// </summary>
    internal static WindowLayoutPlan BuildLayoutPlan(WindowLayout layout, IReadOnlyList<WindowPosition> current) {
        if (layout.Windows == null) { throw new ArgumentException("Layout does not contain a window collection.", nameof(layout)); }
        var remaining = current.ToList();
        var matches = new WindowLayoutMatch?[layout.Windows.Count];
        var pending = new List<int>();
        for (int index = 0; index < layout.Windows.Count; index++) {
            WindowPosition saved = layout.Windows[index];
            if (saved == null) { throw new ArgumentException("Layout contains a null window entry.", nameof(layout)); }
            if (saved.ProcessId == 0 || string.IsNullOrEmpty(saved.Title) ||
                    (long)saved.Right - saved.Left <= 0 || (long)saved.Right - saved.Left > int.MaxValue ||
                    (long)saved.Bottom - saved.Top <= 0 || (long)saved.Bottom - saved.Top > int.MaxValue ||
                    (saved.State.HasValue && saved.State != WindowState.Normal &&
                        saved.State != WindowState.Minimize && saved.State != WindowState.Maximize)) {
                matches[index] = new WindowLayoutMatch(saved, null, WindowLayoutMatchStatus.Invalid, 0);
            } else {
                pending.Add(index);
            }
        }

        // Reserve every candidate at a stronger identity level before a weaker match can claim it.
        foreach (WindowLayoutMatchMethod method in new[] {
                WindowLayoutMatchMethod.ProcessAndTitle, WindowLayoutMatchMethod.Title, WindowLayoutMatchMethod.Application }) {
            var candidates = pending.ToDictionary(index => index, index => {
                WindowPosition saved = layout.Windows[index];
                if (method == WindowLayoutMatchMethod.Application) { return FindApplicationCandidates(saved, remaining); }
                return remaining.Where(window => window.Title == saved.Title && IdentityMatches(saved, window) &&
                    (method == WindowLayoutMatchMethod.ProcessAndTitle ? window.ProcessId == saved.ProcessId : HasApplicationIdentity(saved))).ToList();
            });
            foreach (int index in pending) {
                List<WindowPosition> choices = candidates[index];
                if (choices.Count == 0 && method != WindowLayoutMatchMethod.Application) { continue; }
                bool contested = choices.Count == 1 && candidates.Any(other => other.Key != index && other.Value.Contains(choices[0]));
                matches[index] = contested
                    ? new WindowLayoutMatch(layout.Windows[index], null, WindowLayoutMatchStatus.Ambiguous, choices.Count)
                    : Decide(layout.Windows[index], choices, method);
            }
            foreach (WindowPosition claimed in candidates.Values.SelectMany(choices => choices).Distinct()) { remaining.Remove(claimed); }
            pending = pending.Where(index => matches[index] == null).ToList();
        }
        return new WindowLayoutPlan(matches.Select(match => match!).ToList().AsReadOnly());
    }

    private static WindowLayoutMatch Decide(WindowPosition saved, List<WindowPosition> candidates, WindowLayoutMatchMethod method) {
        WindowLayoutMatchStatus status = candidates.Count == 1 ? WindowLayoutMatchStatus.Matched :
            candidates.Count == 0 ? WindowLayoutMatchStatus.Missing : WindowLayoutMatchStatus.Ambiguous;
        return status == WindowLayoutMatchStatus.Matched
            ? new WindowLayoutMatch(saved, candidates[0], status, 1, method)
            : new WindowLayoutMatch(saved, null, status, candidates.Count);
    }

    private static List<WindowPosition> FindApplicationCandidates(WindowPosition saved, IReadOnlyList<WindowPosition> remaining) {
        if (!HasApplicationIdentity(saved)) {
            return new List<WindowPosition>();
        }

        List<WindowPosition> candidates = remaining.Where(window => IdentityMatches(saved, window)).ToList();
        string suffix = GetTitleSuffix(saved.Title);
        if (candidates.Count > 1 && suffix.Length > 0) {
            List<WindowPosition> narrowed = candidates.Where(window =>
                string.Equals(GetTitleSuffix(window.Title), suffix, StringComparison.OrdinalIgnoreCase)).ToList();
            if (narrowed.Count > 0) {
                candidates = narrowed;
            }
        }
        return candidates;
    }

    /// <summary>The application part browsers and editors keep at the end of a changing title, such as "Google Chrome".</summary>
    internal static string GetTitleSuffix(string? title) {
        if (string.IsNullOrEmpty(title)) {
            return string.Empty;
        }

        int best = -1;
        int separatorLength = 0;
        foreach (string separator in TitleSeparators) {
            int index = title!.LastIndexOf(separator, StringComparison.Ordinal);
            if (index > best) {
                best = index;
                separatorLength = separator.Length;
            }
        }
        return best < 0 ? string.Empty : title!.Substring(best + separatorLength).Trim();
    }

    private static bool HasApplicationIdentity(WindowPosition saved) {
        return !string.IsNullOrEmpty(saved.ProcessName) && !string.IsNullOrEmpty(saved.ClassName);
    }

    private static bool IdentityMatches(WindowPosition saved, WindowPosition window) {
        return (string.IsNullOrEmpty(saved.ProcessName) || string.Equals(saved.ProcessName, window.ProcessName, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(saved.ClassName) || string.Equals(saved.ClassName, window.ClassName, StringComparison.Ordinal)) &&
            WindowApplicationIdentity.Matches(saved.AppUserModelId, window.AppUserModelId) &&
            (string.IsNullOrEmpty(saved.ExplorerFolderPath) || string.Equals(saved.ExplorerFolderPath, window.ExplorerFolderPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Applies uniquely matched layout entries and reports each result. Uncertain or missing targets are skipped.</summary>
    public IReadOnlyList<WindowLayoutApplyResult> ApplyLayout(WindowLayout layout) {
        return WindowDpiContext.Execute(() => {
            WindowLayoutPlan plan = PlanLayout(layout);
            var results = new List<WindowLayoutApplyResult>();
            foreach (WindowLayoutMatch match in plan.Windows) {
                results.Add(ApplyLayoutMatch(match));
            }
            return (IReadOnlyList<WindowLayoutApplyResult>)results.AsReadOnly();
        });
    }

    // Recovery reuses the already computed one-to-one plan rather than rematching each entry.
    private WindowLayoutApplyResult ApplyLayoutMatch(WindowLayoutMatch match) {
        if (match.Window == null) { return new WindowLayoutApplyResult(match, false, null); }
        try {
                WindowPosition live = GetWindowPosition(match.Window);
                if (!string.IsNullOrEmpty(match.Window.ExplorerFolderPath) &&
                        WorkspaceExplorerFolders.Read().TryGetValue(live.Handle, out string? folder)) { live.ExplorerFolderPath = folder; }
                if (live.ProcessId != match.Window.ProcessId || !IdentityMatches(match.Window, live) ||
                        WindowTextHelper.GetWindowText(live.Handle) != match.Window.Title) {
                    throw new InvalidOperationException("The target window identity changed after layout planning.");
                }
                return WorkspaceWindowPlacement.Apply(this, match);
        } catch (Exception ex) {
            return new WindowLayoutApplyResult(match, false, ex.Message);
        }
    }
}
