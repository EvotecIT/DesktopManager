#if NET8_0_OR_GREATER
using System;

namespace DesktopManager;

internal sealed class UiaPattern {
    private readonly UiaElement _element;
    private readonly int _id;
    private readonly object Native;
    internal UiaPattern(UiaElement element, int id, object native) { _element = element; _id = id; Native = native; }
    public UiaPattern Current => this;
    public string SupportedTextSelection => ((IUIAutomationTextPattern)Native).GetSupportedTextSelection() switch {
        0 => "None", 1 => "Single", 2 => "Multiple", _ => string.Empty
    };
    private object? Property(int id) => _element.Property(id);
    public object? Value => Property(_id == 10002 ? 30045 : _id == 10003 ? 30047 : 30093);
    public object? IsReadOnly => Property(_id == 10002 ? 30046 : 30048);
    public object? Minimum => Property(30049);
    public object? Maximum => Property(30050);
    public object? LargeChange => Property(30051);
    public object? SmallChange => Property(30052);
    public object? CanSelectMultiple => Property(30060);
    public object? IsSelectionRequired => Property(30061);
    public object? IsSelected => Property(30079);
    public object? HorizontalScrollPercent => Property(30053);
    public object? VerticalScrollPercent => Property(30055);
    public object? HorizontalViewSize => Property(30054);
    public object? VerticalViewSize => Property(30056);
    public object? HorizontallyScrollable => Property(30057);
    public object? VerticallyScrollable => Property(30058);
    public object? RowCount => Property(30062);
    public object? ColumnCount => Property(30063);
    public object? Row => Property(30064);
    public object? Column => Property(30065);
    public object? RowSpan => Property(30066);
    public object? ColumnSpan => Property(30067);
    public string ToggleState => Property(30086) is int state ? state switch { 0 => "Off", 1 => "On", _ => "Indeterminate" } : string.Empty;
    public string ExpandCollapseState => Property(30070) is int state ? state switch { 0 => "Collapsed", 1 => "Expanded", 2 => "PartiallyExpanded", _ => "LeafNode" } : string.Empty;
    public string RowOrColumnMajor => Property(30083) is int state ? state switch { 0 => "RowMajor", 1 => "ColumnMajor", _ => "Indeterminate" } : string.Empty;
    public void Invoke() => ((IUIAutomationInvokePattern)Native).Invoke();
    public void Select() => ((IUIAutomationSelectionItemPattern)Native).Select();
    public void Toggle() => ((IUIAutomationTogglePattern)Native).Toggle();
    public void Expand() => ((IUIAutomationExpandCollapsePattern)Native).Expand();
    public void Collapse() => ((IUIAutomationExpandCollapsePattern)Native).Collapse();
    public void ScrollIntoView() => ((IUIAutomationScrollItemPattern)Native).ScrollIntoView();
    public void Realize() => ((IUIAutomationVirtualizedItemPattern)Native).Realize();
    public void DoDefaultAction() => ((IUIAutomationLegacyIAccessiblePattern)Native).DoDefaultAction();
    public void SetValue(string value) {
        if (_id == 10018) { ((IUIAutomationLegacyIAccessiblePattern)Native).SetValue(value); }
        else { ((IUIAutomationValuePattern)Native).SetValue(value); }
    }
    public object GetSelection() => _id == 10014 || _id == 10024
        ? new UiaRanges(((IUIAutomationTextPattern)Native).GetSelection())
        : new UiaElements(((IUIAutomationSelectionPattern)Native).GetCurrentSelection());
    public UiaElements GetRowHeaders() => new(((IUIAutomationTablePattern)Native).GetCurrentRowHeaders());
    public UiaElements GetColumnHeaders() => new(((IUIAutomationTablePattern)Native).GetCurrentColumnHeaders());
    public UiaElements GetRowHeaderItems() => new(((IUIAutomationTableItemPattern)Native).GetCurrentRowHeaderItems());
    public UiaElements GetColumnHeaderItems() => new(((IUIAutomationTableItemPattern)Native).GetCurrentColumnHeaderItems());
    public UiaRange DocumentRange => new(((IUIAutomationTextPattern)Native).GetDocumentRange());
    public UiaRange? GetCaretRange(out bool isActive) {
        IUIAutomationTextRange? range = ((IUIAutomationTextPattern2)Native).GetCaretRange(out int active);
        isActive = active != 0;
        return range == null ? null : new UiaRange(range);
    }
    public UiaRange? GetActiveComposition() {
        IUIAutomationTextRange? range = ((IUIAutomationTextEditPattern)Native).GetActiveComposition();
        return range == null ? null : new UiaRange(range);
    }
    public UiaRange? GetConversionTarget() {
        IUIAutomationTextRange? range = ((IUIAutomationTextEditPattern)Native).GetConversionTarget();
        return range == null ? null : new UiaRange(range);
    }
}

internal sealed class UiaRange {
    private readonly IUIAutomationTextRange _native;
    internal UiaRange(IUIAutomationTextRange native) { _native = native; }
    public UiaRange Clone() => new(_native.Clone());
    public string GetText(int maxLength) => _native.GetText(maxLength);
    public UiaRange? FindText(string text, bool backward, bool ignoreCase) {
        IUIAutomationTextRange? found = _native.FindText(text, backward ? 1 : 0, ignoreCase ? 1 : 0);
        return found == null ? null : new UiaRange(found);
    }
    public void ExpandToEnclosingUnit(UiaTextUnit unit) => _native.ExpandToEnclosingUnit((int)unit);
    public void MoveEndpointByRange(UiaEndpoint endpoint, UiaRange range, UiaEndpoint targetEndpoint) => _native.MoveEndpointByRange((int)endpoint, range._native, (int)targetEndpoint);
    public void Select() => _native.Select();
}

internal sealed class UiaRanges : System.Collections.IEnumerable {
    private readonly IUIAutomationTextRangeArray _native;
    internal UiaRanges(IUIAutomationTextRangeArray native) { _native = native; }
    public System.Collections.IEnumerator GetEnumerator() {
        int count = _native.GetLength();
        for (int i = 0; i < count; i++) { yield return new UiaRange(_native.GetElement(i)); }
    }
}
#endif
