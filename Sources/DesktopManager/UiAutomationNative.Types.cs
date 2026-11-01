#if NET8_0_OR_GREATER
namespace DesktopManager;

internal static partial class UiAutomationNative {
    internal static int? PatternId(string name) => name switch {
        "System.Windows.Automation.InvokePattern" => 10000,
        "System.Windows.Automation.ValuePattern" => 10002,
        "System.Windows.Automation.RangeValuePattern" => 10003,
        "System.Windows.Automation.TogglePattern" => 10015,
        "System.Windows.Automation.ExpandCollapsePattern" => 10005,
        "System.Windows.Automation.SelectionPattern" => 10001,
        "System.Windows.Automation.SelectionItemPattern" => 10010,
        "System.Windows.Automation.ScrollPattern" => 10004,
        "System.Windows.Automation.ScrollItemPattern" => 10017,
        "System.Windows.Automation.GridPattern" => 10006,
        "System.Windows.Automation.GridItemPattern" => 10007,
        "System.Windows.Automation.TablePattern" => 10012,
        "System.Windows.Automation.TableItemPattern" => 10013,
        "System.Windows.Automation.TextPattern" => 10014,
        "System.Windows.Automation.TextPattern2" => 10024,
        "System.Windows.Automation.TextEditPattern" => 10032,
        "System.Windows.Automation.ItemContainerPattern" => 10019,
        "System.Windows.Automation.VirtualizedItemPattern" => 10020,
        "System.Windows.Automation.WindowPattern" => 10009,
        "System.Windows.Automation.TransformPattern" => 10016,
        "System.Windows.Automation.DockPattern" => 10011,
        "System.Windows.Automation.MultipleViewPattern" => 10008,
        "System.Windows.Automation.LegacyIAccessiblePattern" => 10018,
        _ => null
    };
}
#endif
