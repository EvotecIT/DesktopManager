using DesktopManager.App.Core;

namespace DesktopManager.App;

internal sealed class WindowRuleOption {
    public WindowRuleOption(WindowLayoutProfileDefinition layout, WindowRuleDefinition rule) {
        LayoutId = layout.Id;
        RuleId = rule.Id;
        DisplayName = $"{layout.Name}: {rule.Name}";
        string titlePattern = rule.Match?.TitlePattern ?? "<missing title match>";
        string processPattern = rule.Match?.ProcessNamePattern ?? "<missing process match>";
        string placement = rule.Action?.Placement ?? "<missing action>";
        Details = $"{(rule.Enabled ? "Enabled" : "Disabled")} | {titlePattern} | {processPattern} | {placement}";
        IsEnabled = rule.Enabled;
    }

    public string LayoutId { get; }

    public string RuleId { get; }

    public string DisplayName { get; }

    public string Details { get; }

    public bool IsEnabled { get; }
}
