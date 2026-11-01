#if NET8_0_OR_GREATER
using System;
using System.Collections;

namespace DesktopManager;

// Native values feed the existing matching, privacy and timeout logic through typed calls.
// Generated wrappers own their references and finalize them after cached aliases become unreachable.
internal static partial class UiAutomationNative {
    [ThreadStatic] private static object? _client;
    internal static IUIAutomation Client => (IUIAutomation)(_client ??=
        NativeComActivation.CreateInstance(new Guid("ff48dba4-60ef-4201-aa87-54103eef594e")));

    internal static object? GetPattern(IUIAutomationElement element, int id) {
        IntPtr pointer = element.GetCurrentPattern(id);
        if (pointer == IntPtr.Zero) { return null; }
        try { return NativeComActivation.FromPointer(pointer); }
        finally { System.Runtime.InteropServices.Marshal.Release(pointer); }
    }
}

[Flags]
internal enum UiaTreeScope { Element = 1, Children = 2, Descendants = 4, Subtree = 7 }
internal enum UiaTextUnit { Character, Format, Word, Line, Paragraph, Page, Document }
internal enum UiaEndpoint { Start, End }

internal class UiaCondition {
    internal IUIAutomationCondition? Native { get; }
    internal UiaCondition(IUIAutomationCondition? native) { Native = native; }
    public static readonly UiaCondition TrueCondition = new(null);
}


internal sealed class UiaPropertyCondition : UiaCondition {
    public UiaPropertyCondition(int property, object value) : base(Create(property, value)) { }
    private static IUIAutomationCondition Create(int property, object value) {
        var variant = new UiaVariant();
        if (value is int number) { variant.Type = 3; variant.Data.Integer = number; }
        else if (value is string text) { variant.Type = 8; variant.Data.Pointer = System.Runtime.InteropServices.Marshal.StringToBSTR(text); }
        else { throw new ArgumentException("Unsupported UIA search property value.", nameof(value)); }
        try { return UiAutomationNative.Client.CreatePropertyCondition(property, variant); }
        finally { UiaValues.VariantClear(ref variant); }
    }
}

internal sealed class UiaElement {
    internal IUIAutomationElement Native { get; }
    internal UiaElement(IUIAutomationElement native) { Native = native; }
    public UiaElement Current => this;
    public static UiaElement RootElement => new(UiAutomationNative.Client.GetRootElement());
    public static UiaElement FocusedElement => new(UiAutomationNative.Client.GetFocusedElement());
    public static UiaElement FromHandle(IntPtr handle) => new(UiAutomationNative.Client.ElementFromHandle(handle));
    public UiaElement? FindFirst(UiaTreeScope scope, UiaCondition condition) {
        IUIAutomationElement? result = Native.FindFirst((int)scope, ResolveCondition(condition));
        return result == null ? null : new UiaElement(result);
    }
    public UiaElements FindAll(UiaTreeScope scope, UiaCondition condition) => new(Native.FindAll((int)scope, ResolveCondition(condition)));
    private static IUIAutomationCondition ResolveCondition(UiaCondition condition) => condition.Native ?? UiAutomationNative.Client.CreateTrueCondition();
    public int[] GetRuntimeId() => UiaValues.ReadRuntimeId(Native.GetRuntimeId());
    public void SetFocus() => Native.SetFocus();
    public UiaPattern? GetCurrentPattern(int id) {
        object? pattern = UiAutomationNative.GetPattern(Native, id);
        return pattern == null ? null : new UiaPattern(this, id, pattern);
    }
    internal object? Property(int id) {
        Native.GetCurrentPropertyValueEx(id, 1, out UiaVariant value);
        return UiaValues.Read(ref value);
    }
    public object? Name => Property(30005);
    public object? ClassName => Property(30012);
    public object? AutomationId => Property(30011);
    public object? FrameworkId => Property(30024);
    public object? ProcessId => Property(30002);
    public object? NativeWindowHandle => Property(30020);
    public object? IsPassword => Property(30019);
    public object? IsEnabled => Property(30010);
    public object? IsKeyboardFocusable => Property(30009);
    public object? HasKeyboardFocus => Property(30008);
    public object? IsOffscreen => Property(30022);
    public object? HelpText => Property(30013);
    public object? LocalizedControlType => Property(30004);
    public object? ItemStatus => Property(30026);
    public object? ItemType => Property(30021);
    public object? AccessKey => Property(30007);
    public object? AcceleratorKey => Property(30006);
    public object? AriaRole => Property(30101);
    public object? AriaProperties => Property(30102);
    public string ControlType => Property(30003) is int id && id >= 50000 && id < 50000 + ControlNames.Length ? ControlNames[id - 50000] : string.Empty;
    private static readonly string[] ControlNames = { "Button", "Calendar", "CheckBox", "ComboBox", "Edit", "Hyperlink", "Image", "ListItem", "List", "Menu", "MenuBar", "MenuItem", "ProgressBar", "RadioButton", "ScrollBar", "Slider", "Spinner", "StatusBar", "Tab", "TabItem", "Text", "ToolBar", "ToolTip", "Tree", "TreeItem", "Custom", "Group", "Thumb", "DataGrid", "DataItem", "Document", "SplitButton", "Window", "Pane", "Header", "HeaderItem", "Table", "TitleBar", "Separator", "SemanticZoom", "AppBar" };
    public UiaBounds? BoundingRectangle => Property(30001) is double[] values && values.Length == 4 ? new(values) : null;
}

internal sealed class UiaBounds {
    private readonly double[] _values;
    internal UiaBounds(double[] values) { _values = values; }
    public double Left => _values[0];
    public double Top => _values[1];
    public double Width => _values[2];
    public double Height => _values[3];
}

internal sealed class UiaElements : IEnumerable {
    private readonly IUIAutomationElementArray _native;
    internal UiaElements(IUIAutomationElementArray native) { _native = native; }
    public int Count => _native.GetLength();
    public UiaElement this[int index] => new(_native.GetElement(index));
    public IEnumerator GetEnumerator() {
        int count = Count;
        for (int i = 0; i < count; i++) { yield return this[i]; }
    }
}

internal sealed class UiaWalker {
    private readonly IUIAutomationTreeWalker _native;
    private UiaWalker(IUIAutomationTreeWalker native) { _native = native; }
    public static UiaWalker RawViewWalker => new(UiAutomationNative.Client.GetRawViewWalker());
    public static UiaWalker ControlViewWalker => new(UiAutomationNative.Client.GetControlViewWalker());
    public UiaElement? GetParent(UiaElement element) {
        IUIAutomationElement? parent = _native.GetParentElement(element.Native);
        return parent == null ? null : new UiaElement(parent);
    }
}
#endif
