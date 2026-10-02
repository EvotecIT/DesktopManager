using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

public partial class WindowManager {
    /// <summary>Plans one-to-one layout matches without moving windows. Ambiguous targets are skipped.</summary>
    public WindowLayoutPlan PlanLayout(WindowLayout layout) {
        if (layout == null) { throw new ArgumentNullException(nameof(layout)); }
        return BuildLayoutPlan(layout, GetWindows().Select(GetWindowPosition).ToArray());
    }

    internal static WindowLayoutPlan BuildLayoutPlan(WindowLayout layout, IReadOnlyList<WindowPosition> current) {
        if (layout.Windows == null) { throw new ArgumentException("Layout does not contain a window collection.", nameof(layout)); }
        var remaining = current.ToList();
        var matches = new List<WindowLayoutMatch>();
        foreach (WindowPosition saved in layout.Windows) {
            if (saved == null) { throw new ArgumentException("Layout contains a null window entry.", nameof(layout)); }
            if (saved.ProcessId == 0 || string.IsNullOrEmpty(saved.Title) ||
                    (long)saved.Right - saved.Left <= 0 || (long)saved.Right - saved.Left > int.MaxValue ||
                    (long)saved.Bottom - saved.Top <= 0 || (long)saved.Bottom - saved.Top > int.MaxValue ||
                    (saved.State.HasValue && saved.State != WindowState.Normal &&
                        saved.State != WindowState.Minimize && saved.State != WindowState.Maximize)) {
                matches.Add(new WindowLayoutMatch(saved, null, WindowLayoutMatchStatus.Invalid, 0));
                continue;
            }
            List<WindowPosition> candidates = remaining.Where(window =>
                window.ProcessId == saved.ProcessId && window.Title == saved.Title && IdentityMatches(saved, window)).ToList();
            if (candidates.Count == 0 && !string.IsNullOrEmpty(saved.ProcessName) && !string.IsNullOrEmpty(saved.ClassName)) {
                candidates = remaining.Where(window => window.Title == saved.Title && IdentityMatches(saved, window)).ToList();
            }
            WindowLayoutMatchStatus status = candidates.Count == 1 ? WindowLayoutMatchStatus.Matched :
                candidates.Count == 0 ? WindowLayoutMatchStatus.Missing : WindowLayoutMatchStatus.Ambiguous;
            WindowPosition? target = status == WindowLayoutMatchStatus.Matched ? candidates[0] : null;
            matches.Add(new WindowLayoutMatch(saved, target, status, candidates.Count));
            if (target != null) { remaining.Remove(target); }
        }
        return new WindowLayoutPlan(matches.AsReadOnly());
    }

    private static bool IdentityMatches(WindowPosition saved, WindowPosition window) {
        return (string.IsNullOrEmpty(saved.ProcessName) || string.Equals(saved.ProcessName, window.ProcessName, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(saved.ClassName) || string.Equals(saved.ClassName, window.ClassName, StringComparison.Ordinal));
    }

    /// <summary>Applies uniquely matched layout entries and reports each result. Uncertain or missing targets are skipped.</summary>
    public IReadOnlyList<WindowLayoutApplyResult> ApplyLayout(WindowLayout layout) {
        WindowLayoutPlan plan = PlanLayout(layout);
        var results = new List<WindowLayoutApplyResult>();
        foreach (WindowLayoutMatch match in plan.Windows) {
            if (match.Window == null) {
                results.Add(new WindowLayoutApplyResult(match, false, null));
                continue;
            }
            try {
                WindowPosition live = GetWindowPosition(match.Window);
                if (live.ProcessId != match.Window.ProcessId || !IdentityMatches(match.Window, live) ||
                        WindowTextHelper.GetWindowText(live.Handle) != match.Window.Title) {
                    throw new InvalidOperationException("The target window identity changed after layout planning.");
                }
                WindowPosition saved = match.SavedWindow;
                RestoreWindow(live);
                SetWindowRectangle(live, saved.Left, saved.Top, saved.Width, saved.Height);
                WindowPosition actual = GetWindowPosition(live);
                if (actual.Left != saved.Left || actual.Top != saved.Top || actual.Width != saved.Width || actual.Height != saved.Height) {
                    throw new InvalidOperationException("The target did not accept the requested bounds.");
                }
                if (saved.State == WindowState.Minimize) { MinimizeWindow(live); }
                else if (saved.State == WindowState.Maximize) { MaximizeWindow(live); }
                if (saved.State.HasValue && GetWindowPosition(live).State != saved.State) {
                    throw new InvalidOperationException("The target did not accept the requested window state.");
                }
                results.Add(new WindowLayoutApplyResult(match, true, null));
            } catch (Exception ex) {
                results.Add(new WindowLayoutApplyResult(match, false, ex.Message));
            }
        }
        return results.AsReadOnly();
    }
}
