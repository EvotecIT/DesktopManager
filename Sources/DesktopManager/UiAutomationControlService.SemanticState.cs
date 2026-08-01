using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    internal const int MaximumSemanticCollectionItems = 256;
    private static readonly IReadOnlyList<(string TypeName, string Name)> ObservationPatternCatalog = new[] {
        ("System.Windows.Automation.InvokePattern", "Invoke"),
        ("System.Windows.Automation.ValuePattern", "Value"),
        ("System.Windows.Automation.RangeValuePattern", "RangeValue"),
        ("System.Windows.Automation.TogglePattern", "Toggle"),
        ("System.Windows.Automation.ExpandCollapsePattern", "ExpandCollapse"),
        ("System.Windows.Automation.SelectionPattern", "Selection"),
        ("System.Windows.Automation.SelectionItemPattern", "SelectionItem"),
        ("System.Windows.Automation.ScrollPattern", "Scroll"),
        ("System.Windows.Automation.ScrollItemPattern", "ScrollItem"),
        ("System.Windows.Automation.GridPattern", "Grid"),
        ("System.Windows.Automation.GridItemPattern", "GridItem"),
        ("System.Windows.Automation.TablePattern", "Table"),
        ("System.Windows.Automation.TableItemPattern", "TableItem"),
        ("System.Windows.Automation.TextPattern", "Text"),
        ("System.Windows.Automation.TextPattern2", "Text2"),
        ("System.Windows.Automation.TextEditPattern", "TextEdit"),
        ("System.Windows.Automation.ItemContainerPattern", "ItemContainer"),
        ("System.Windows.Automation.VirtualizedItemPattern", "VirtualizedItem"),
        ("System.Windows.Automation.WindowPattern", "Window"),
        ("System.Windows.Automation.TransformPattern", "Transform"),
        ("System.Windows.Automation.DockPattern", "Dock"),
        ("System.Windows.Automation.MultipleViewPattern", "MultipleView"),
        ("System.Windows.Automation.LegacyIAccessiblePattern", "LegacyIAccessible")
    };

    private Dictionary<string, object> ReadObservationPatterns(object element, List<string> errors) {
        var patterns = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach ((string typeName, string name) in ObservationPatternCatalog) {
            try {
                Type? patternType = _automationClientAssembly?.GetType(typeName, throwOnError: false);
                object? pattern = patternType == null ? null : GetCurrentPattern(element, patternType);
                if (pattern != null) {
                    patterns[name] = pattern;
                }
            } catch (Exception ex) {
                AddObservationError(errors, $"pattern.{name}", ex);
            }
        }

        return patterns;
    }

    private static DesktopControlCapabilities CreateObservationCapabilities(
        WindowControlInfo control,
        Dictionary<string, object> patterns,
        bool canAccessText) {
        var names = new List<string>(patterns.Keys);
        names.Sort(StringComparer.OrdinalIgnoreCase);
        bool valuePatternWritable = true;
        if (patterns.TryGetValue("Value", out object? valuePattern)) {
            valuePatternWritable = ReadPatternCurrentBoolean(
                valuePattern,
                "IsReadOnly",
                new List<string>(),
                "value.isReadOnly") == false;
        }

        return new DesktopControlCapabilities {
            Patterns = names,
            CanReadText = canAccessText && (patterns.ContainsKey("Text") || patterns.ContainsKey("Value") || patterns.ContainsKey("RangeValue") || patterns.ContainsKey("LegacyIAccessible")),
            CanReadTextSelection = canAccessText && (patterns.ContainsKey("Text") || patterns.ContainsKey("Text2")),
            CanSetValue = canAccessText && valuePatternWritable &&
                (patterns.ContainsKey("Value") || patterns.ContainsKey("LegacyIAccessible") || control.Handle != IntPtr.Zero),
            CanInvoke = patterns.ContainsKey("Invoke") || patterns.ContainsKey("LegacyIAccessible"),
            CanToggle = patterns.ContainsKey("Toggle"),
            CanSelect = patterns.ContainsKey("Selection") || patterns.ContainsKey("SelectionItem"),
            CanExpandCollapse = patterns.ContainsKey("ExpandCollapse"),
            CanReadRange = patterns.ContainsKey("RangeValue"),
            CanScroll = patterns.ContainsKey("Scroll") || patterns.ContainsKey("ScrollItem"),
            CanReadGrid = patterns.ContainsKey("Grid") || patterns.ContainsKey("GridItem"),
            CanReadTable = patterns.ContainsKey("Table") || patterns.ContainsKey("TableItem"),
            CanRealizeVirtualizedItem = patterns.ContainsKey("VirtualizedItem"),
            SupportsBackgroundClick = control.SupportsBackgroundClick,
            SupportsBackgroundText = canAccessText && control.SupportsBackgroundText,
            SupportsBackgroundKeys = control.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = control.SupportsForegroundInputFallback
        };
    }

    private void PopulateSemanticState(
        DesktopControlObservation observation,
        Dictionary<string, object> patterns,
        int maxTextLength,
        List<string> errors) {
        if (patterns.TryGetValue("Toggle", out object? toggle)) {
            observation.IsChecked = ReadToggleState(toggle, errors);
        }

        if (patterns.TryGetValue("ExpandCollapse", out object? expandCollapse)) {
            observation.ExpandCollapseState = ReadPatternCurrentString(expandCollapse, "ExpandCollapseState", errors, "expandCollapse.state");
        }

        if (patterns.TryGetValue("Value", out object? valuePattern)) {
            observation.IsReadOnly = ReadPatternCurrentBoolean(valuePattern, "IsReadOnly", errors, "value.isReadOnly");
            if (observation.IsReadOnly != false) {
                observation.Capabilities.CanSetValue = false;
            }
        }

        var textBudget = new SemanticTextBudget(maxTextLength, MaximumSemanticCollectionItems);
        PopulateSelectionState(observation.Selection, patterns, textBudget, errors);
        PopulateRangeState(observation.Range, patterns, errors);
        PopulateScrollState(observation.Scroll, patterns, errors);
        PopulateGridState(observation.Grid, patterns, textBudget, errors);
    }

    private void PopulateSelectionState(
        DesktopControlSelectionObservation selection,
        Dictionary<string, object> patterns,
        SemanticTextBudget textBudget,
        List<string> errors) {
        if (patterns.TryGetValue("Selection", out object? selectionPattern)) {
            selection.CanSelectMultiple = ReadPatternCurrentBoolean(selectionPattern, "CanSelectMultiple", errors, "selection.canSelectMultiple");
            selection.IsSelectionRequired = ReadPatternCurrentBoolean(selectionPattern, "IsSelectionRequired", errors, "selection.isRequired");
            BoundedLabelResult labels = ReadElementLabels(
                InvokePatternMethod(selectionPattern, "GetCurrentSelection", errors, "selection.items"),
                textBudget,
                errors,
                "selection.items");
            selection.Items = labels.Values;
            selection.IsTruncated = labels.IsTruncated;
            if (selection.Items.Count == 0 && !selection.IsTruncated) {
                labels = ReadElementLabels(
                    InvokePatternMethod(selectionPattern, "GetSelection", errors, "selection.items"),
                    textBudget,
                    errors,
                    "selection.items");
                selection.Items = labels.Values;
                selection.IsTruncated = labels.IsTruncated;
            }
        }

        if (patterns.TryGetValue("SelectionItem", out object? selectionItemPattern)) {
            selection.IsSelected = ReadPatternCurrentBoolean(selectionItemPattern, "IsSelected", errors, "selectionItem.isSelected");
        }
    }

    private static void PopulateRangeState(
        DesktopControlRangeObservation range,
        Dictionary<string, object> patterns,
        List<string> errors) {
        if (!patterns.TryGetValue("RangeValue", out object? pattern)) {
            return;
        }

        range.Value = ReadPatternCurrentDouble(pattern, "Value", errors, "range.value");
        range.Minimum = ReadPatternCurrentDouble(pattern, "Minimum", errors, "range.minimum");
        range.Maximum = ReadPatternCurrentDouble(pattern, "Maximum", errors, "range.maximum");
        range.SmallChange = ReadPatternCurrentDouble(pattern, "SmallChange", errors, "range.smallChange");
        range.LargeChange = ReadPatternCurrentDouble(pattern, "LargeChange", errors, "range.largeChange");
        range.IsReadOnly = ReadPatternCurrentBoolean(pattern, "IsReadOnly", errors, "range.isReadOnly");
    }

    private static void PopulateScrollState(
        DesktopControlScrollObservation scroll,
        Dictionary<string, object> patterns,
        List<string> errors) {
        if (!patterns.TryGetValue("Scroll", out object? pattern)) {
            return;
        }

        scroll.HorizontallyScrollable = ReadPatternCurrentBoolean(pattern, "HorizontallyScrollable", errors, "scroll.horizontalAvailable");
        scroll.VerticallyScrollable = ReadPatternCurrentBoolean(pattern, "VerticallyScrollable", errors, "scroll.verticalAvailable");
        scroll.HorizontalPercent = ReadPatternCurrentDouble(pattern, "HorizontalScrollPercent", errors, "scroll.horizontalPercent");
        scroll.VerticalPercent = ReadPatternCurrentDouble(pattern, "VerticalScrollPercent", errors, "scroll.verticalPercent");
        scroll.HorizontalViewSize = ReadPatternCurrentDouble(pattern, "HorizontalViewSize", errors, "scroll.horizontalViewSize");
        scroll.VerticalViewSize = ReadPatternCurrentDouble(pattern, "VerticalViewSize", errors, "scroll.verticalViewSize");
    }

    private void PopulateGridState(
        DesktopControlGridObservation grid,
        Dictionary<string, object> patterns,
        SemanticTextBudget textBudget,
        List<string> errors) {
        if (patterns.TryGetValue("Grid", out object? gridPattern)) {
            grid.RowCount = ReadPatternCurrentInt32(gridPattern, "RowCount", errors, "grid.rowCount");
            grid.ColumnCount = ReadPatternCurrentInt32(gridPattern, "ColumnCount", errors, "grid.columnCount");
        }

        if (patterns.TryGetValue("GridItem", out object? gridItemPattern)) {
            grid.Row = ReadPatternCurrentInt32(gridItemPattern, "Row", errors, "gridItem.row");
            grid.Column = ReadPatternCurrentInt32(gridItemPattern, "Column", errors, "gridItem.column");
            grid.RowSpan = ReadPatternCurrentInt32(gridItemPattern, "RowSpan", errors, "gridItem.rowSpan");
            grid.ColumnSpan = ReadPatternCurrentInt32(gridItemPattern, "ColumnSpan", errors, "gridItem.columnSpan");
        }

        if (patterns.TryGetValue("Table", out object? tablePattern)) {
            grid.RowOrColumnMajor = ReadPatternCurrentString(tablePattern, "RowOrColumnMajor", errors, "table.order");
            BoundedLabelResult rowHeaders = ReadElementLabels(
                InvokePatternMethod(tablePattern, "GetCurrentRowHeaders", errors, "table.rowHeaders"),
                textBudget,
                errors,
                "table.rowHeaders");
            BoundedLabelResult columnHeaders = ReadElementLabels(
                InvokePatternMethod(tablePattern, "GetCurrentColumnHeaders", errors, "table.columnHeaders"),
                textBudget,
                errors,
                "table.columnHeaders");
            grid.RowHeaders = rowHeaders.Values;
            grid.ColumnHeaders = columnHeaders.Values;
            grid.IsTruncated = rowHeaders.IsTruncated || columnHeaders.IsTruncated;
        }

        if (patterns.TryGetValue("TableItem", out object? tableItemPattern)) {
            BoundedLabelResult rowHeaders = ReadElementLabels(
                InvokePatternMethod(tableItemPattern, "GetCurrentRowHeaderItems", errors, "tableItem.rowHeaders"),
                textBudget,
                errors,
                "tableItem.rowHeaders");
            BoundedLabelResult columnHeaders = ReadElementLabels(
                InvokePatternMethod(tableItemPattern, "GetCurrentColumnHeaderItems", errors, "tableItem.columnHeaders"),
                textBudget,
                errors,
                "tableItem.columnHeaders");
            grid.RowHeaders = rowHeaders.Values;
            grid.ColumnHeaders = columnHeaders.Values;
            grid.IsTruncated = grid.IsTruncated || rowHeaders.IsTruncated || columnHeaders.IsTruncated;
        }
    }

    private static bool? ReadToggleState(object pattern, List<string> errors) {
        string state = ReadPatternCurrentString(pattern, "ToggleState", errors, "toggle.state");
        if (state.EndsWith("On", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        if (state.EndsWith("Off", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return null;
    }

    private BoundedLabelResult ReadElementLabels(
        object? values,
        SemanticTextBudget textBudget,
        List<string> errors,
        string scope) {
        if (values is not IEnumerable enumerable) {
            return new BoundedLabelResult();
        }

        var labels = new List<string>();
        bool isTruncated = false;
        foreach (object? item in enumerable) {
            if (item == null) {
                continue;
            }

            if (textBudget.RemainingItems <= 0 || textBudget.RemainingCharacters <= 0) {
                isTruncated = true;
                break;
            }

            textBudget.RemainingItems--;

            try {
                WindowControlInfo? info = CreateControlInfo(item, readValue: false);
                if (info == null || info.IsPassword != false) {
                    continue;
                }

                int allowedLength = Math.Min(4096, textBudget.RemainingCharacters);
                UiAutomationTextReadResult? text = ReadElementText(item, allowedLength, expectedText: null);
                string label = text?.Value ?? info.Text;
                if (!string.IsNullOrWhiteSpace(label)) {
                    if (text?.IsTruncated == true || label.Length > allowedLength) {
                        labels.Add(label.Length > allowedLength ? label.Substring(0, allowedLength) : label);
                        textBudget.RemainingCharacters -= Math.Min(label.Length, allowedLength);
                        isTruncated = true;
                        break;
                    }

                    labels.Add(label);
                    textBudget.RemainingCharacters -= label.Length;
                }
            } catch (Exception ex) {
                AddObservationError(errors, scope, ex);
            }
        }

        return new BoundedLabelResult {
            Values = labels,
            IsTruncated = isTruncated
        };
    }

    private sealed class SemanticTextBudget {
        internal SemanticTextBudget(int remainingCharacters, int remainingItems) {
            RemainingCharacters = remainingCharacters;
            RemainingItems = remainingItems;
        }

        internal int RemainingCharacters { get; set; }
        internal int RemainingItems { get; set; }
    }

    private sealed class BoundedLabelResult {
        internal IReadOnlyList<string> Values { get; set; } = Array.Empty<string>();
        internal bool IsTruncated { get; set; }
    }

    private static object? InvokePatternMethod(object pattern, string name, List<string> errors, string scope) {
        try {
            return pattern.GetType().GetMethod(name, Type.EmptyTypes)?.Invoke(pattern, null);
        } catch (Exception ex) {
            AddObservationError(errors, scope, ex);
            return null;
        }
    }

    private static object? ReadPatternCurrentValue(object pattern, string propertyName, List<string> errors, string scope) {
        try {
            object? current = pattern.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(pattern);
            return current?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(current);
        } catch (Exception ex) {
            AddObservationError(errors, scope, ex);
            return null;
        }
    }

    private static string ReadPatternCurrentString(object pattern, string propertyName, List<string> errors, string scope) {
        return ReadPatternCurrentValue(pattern, propertyName, errors, scope)?.ToString() ?? string.Empty;
    }

    private static bool? ReadPatternCurrentBoolean(object pattern, string propertyName, List<string> errors, string scope) {
        object? value = ReadPatternCurrentValue(pattern, propertyName, errors, scope);
        return value is bool result ? result : null;
    }

    private static int? ReadPatternCurrentInt32(object pattern, string propertyName, List<string> errors, string scope) {
        object? value = ReadPatternCurrentValue(pattern, propertyName, errors, scope);
        try {
            return value == null ? null : Convert.ToInt32(value);
        } catch (Exception ex) {
            AddObservationError(errors, scope, ex);
            return null;
        }
    }

    private static double? ReadPatternCurrentDouble(object pattern, string propertyName, List<string> errors, string scope) {
        object? value = ReadPatternCurrentValue(pattern, propertyName, errors, scope);
        try {
            return value == null ? null : Convert.ToDouble(value);
        } catch (Exception ex) {
            AddObservationError(errors, scope, ex);
            return null;
        }
    }
}
