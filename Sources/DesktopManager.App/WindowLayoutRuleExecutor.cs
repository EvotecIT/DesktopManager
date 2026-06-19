using DesktopManager.App.Core;

namespace DesktopManager.App;

internal sealed class WindowLayoutRuleExecutor {
    private readonly global::DesktopManager.WindowManager _windowManager;
    private readonly global::DesktopManager.WindowPlacementService _placementService;

    public WindowLayoutRuleExecutor()
        : this(new global::DesktopManager.WindowManager(), new global::DesktopManager.WindowPlacementService()) {
    }

    private WindowLayoutRuleExecutor(
        global::DesktopManager.WindowManager windowManager,
        global::DesktopManager.WindowPlacementService placementService) {
        _windowManager = windowManager;
        _placementService = placementService;
    }

    public WindowLayoutRuleExecutionResult ApplyRules(IEnumerable<WindowLayoutProfileDefinition>? layouts) {
        WindowLayoutRuleExecutionResult result = new();
        if (layouts == null || !layouts.Any(layout => layout.Enabled && layout.Rules != null && layout.Rules.Any(rule => rule.Enabled))) {
            result.Messages.Add("No enabled layout rules are configured.");
            return result;
        }

        IReadOnlyList<global::DesktopManager.WindowInfo> windows = _windowManager.GetWindows(new global::DesktopManager.WindowQueryOptions {
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = false,
            IncludeEmptyTitles = false,
            IsVisible = true
        });
        result.WindowsScanned = windows.Count;

        foreach (global::DesktopManager.WindowInfo window in windows) {
            WindowRuleObservation observation = CreateObservation(window);
            WindowRuleEvaluation evaluation = WindowRuleEvaluator.Evaluate(layouts, observation);
            if (!evaluation.Matched || evaluation.Request == null || evaluation.Rule == null) {
                continue;
            }

            result.Matches++;
            try {
                global::DesktopManager.WindowPlacementResult placement = _placementService.Apply(evaluation.Request);
                if (placement.Verified) {
                    result.Applied++;
                    result.Messages.Add($"Applied '{evaluation.Rule.Name}' to '{placement.Window.Title}'.");
                } else {
                    result.Failed++;
                    result.Messages.Add($"Rule '{evaluation.Rule.Name}' ran for '{window.Title}', but verification failed.");
                }
            } catch (Exception ex) {
                result.Failed++;
                result.Messages.Add($"Rule '{evaluation.Rule.Name}' failed for '{window.Title}': {ex.Message}");
            }
        }

        if (result.Matches == 0) {
            result.Messages.Add("No visible windows matched enabled layout rules.");
        }

        return result;
    }

    private WindowRuleObservation CreateObservation(global::DesktopManager.WindowInfo window) {
        global::DesktopManager.WindowProcessInfo process = _windowManager.GetWindowProcessInfo(window);
        return new WindowRuleObservation {
            Handle = window.Handle,
            Title = window.Title,
            ProcessName = process.ProcessName,
            ProcessPath = process.ProcessPath ?? string.Empty
        };
    }
}
