using DesktopManager.App.Core;

namespace DesktopManager.App;

internal sealed class WindowRuleOption {
    public WindowRuleOption(WindowLayoutProfileDefinition layout, WindowRuleDefinition rule) {
        LayoutId = layout.Id;
        RuleId = rule.Id;
        DisplayName = $"{layout.Name}: {rule.Name}";
        Details = $"{(rule.Enabled ? "Enabled" : "Disabled")} | {rule.Match.TitlePattern} | {rule.Match.ProcessNamePattern} | {rule.Action.Placement}";
        IsEnabled = rule.Enabled;
    }

    public string LayoutId { get; }

    public string RuleId { get; }

    public string DisplayName { get; }

    public string Details { get; }

    public bool IsEnabled { get; }
}
