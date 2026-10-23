using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

public partial class WindowManager {
    /// <summary>Plans one-to-one layout matches without moving windows. Ambiguous targets are skipped.</summary>
    public WindowLayoutPlan PlanLayout(WindowLayout layout) {
        if (layout == null) { throw new ArgumentNullException(nameof(layout)); }
        WindowPosition[] current = GetWindows().Select(GetWindowPosition).ToArray();
        if (layout.Windows != null && layout.Windows.Any(window => window != null && !string.IsNullOrEmpty(window.ExplorerFolderPath))) {
            Dictionary<IntPtr, string> folders = WorkspaceExplorerFolders.Read();
            foreach (WindowPosition window in current) {
                if (folders.TryGetValue(window.Handle, out string? folder)) { window.ExplorerFolderPath = folder; }
            }
        }
        return BuildLayoutPlan(layout, current);
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
            (string.IsNullOrEmpty(saved.ClassName) || string.Equals(saved.ClassName, window.ClassName, StringComparison.Ordinal)) &&
            (string.IsNullOrEmpty(saved.ExplorerFolderPath) || string.Equals(saved.ExplorerFolderPath, window.ExplorerFolderPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Applies uniquely matched layout entries and reports each result. Uncertain or missing targets are skipped.</summary>
    public IReadOnlyList<WindowLayoutApplyResult> ApplyLayout(WindowLayout layout) {
        WindowLayoutPlan plan = PlanLayout(layout);
        var results = new List<WindowLayoutApplyResult>();
        foreach (WindowLayoutMatch match in plan.Windows) {
            results.Add(ApplyLayoutMatch(match));
        }
        return results.AsReadOnly();
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
