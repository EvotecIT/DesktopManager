#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

// These fakes implement the actual provider contracts and run through the production adapters.
// They never create COM objects, windows or user input. Unused vtable entries fail if reached.
internal sealed class NativeUiaElementFixture : IUIAutomationElement {
    private readonly bool? _isPassword;
    private readonly string _name;
    private readonly bool _throwOnPasswordRead;
    internal int PasswordReadCount { get; private set; }
    internal int PatternReadCount { get; private set; }

    internal NativeUiaElementFixture(bool? isPassword = false, string name = "", bool throwOnPasswordRead = false) {
        _isPassword = isPassword;
        _name = name;
        _throwOnPasswordRead = throwOnPasswordRead;
    }

    public void GetCurrentPropertyValueEx(int id, int ignoreDefaultValue, out UiaVariant value) {
        value = default;
        if (id == 30019) {
            PasswordReadCount++;
            if (_throwOnPasswordRead) { throw new InvalidOperationException("Provider unavailable"); }
            if (_isPassword.HasValue) { value.Type = 11; value.Data.Integer = _isPassword.Value ? -1 : 0; }
        } else if (id == 30005) {
            value.Type = 8;
            value.Data.Pointer = Marshal.StringToBSTR(_name);
        }
    }

    public IntPtr GetCurrentPattern(int id) { PatternReadCount++; return IntPtr.Zero; }
    public void SetFocus() => throw new NotSupportedException();
    public IntPtr GetRuntimeId() => throw new NotSupportedException();
    public IUIAutomationElement? FindFirst(int scope, IUIAutomationCondition condition) => throw new NotSupportedException();
    public IUIAutomationElementArray FindAll(int scope, IUIAutomationCondition condition) => throw new NotSupportedException();
    public void Reserved4() => throw new NotSupportedException();
    public void Reserved5() => throw new NotSupportedException();
    public void Reserved6() => throw new NotSupportedException();
    public void Reserved7() => throw new NotSupportedException();
    public void Reserved9() => throw new NotSupportedException();
    public void Reserved10() => throw new NotSupportedException();
    public void Reserved11() => throw new NotSupportedException();
    public void Reserved12() => throw new NotSupportedException();
}

internal sealed class NativeUiaTextRangeFixture : IUIAutomationTextRange {
    private readonly string _document;
    internal int LastMaxLength { get; private set; }
    internal NativeUiaTextRangeFixture(string document) { _document = document; }

    public string GetText(int maxLength) {
        LastMaxLength = maxLength;
        return maxLength < 0 || _document.Length <= maxLength ? _document : _document.Substring(0, maxLength);
    }

    public IUIAutomationTextRange? FindText(string text, int backward, int ignoreCase) {
        return _document.IndexOf(text, ignoreCase != 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0
            ? new NativeUiaTextRangeFixture(text) : null;
    }

    public IUIAutomationTextRange Clone() => new NativeUiaTextRangeFixture(_document);
    public void MoveEndpointByRange(int endpoint, IUIAutomationTextRange range, int targetEndpoint) => throw new NotSupportedException();
    public void ExpandToEnclosingUnit(int unit) => throw new NotSupportedException();
    public void Select() => throw new NotSupportedException();
    public void Reserved1() => throw new NotSupportedException();
    public void Reserved2() => throw new NotSupportedException();
    public void Reserved4() => throw new NotSupportedException();
    public void Reserved6() => throw new NotSupportedException();
    public void Reserved7() => throw new NotSupportedException();
    public void Reserved8() => throw new NotSupportedException();
    public void Reserved10() => throw new NotSupportedException();
    public void Reserved11() => throw new NotSupportedException();
}

internal sealed class NativeUiaTextPatternFixture : IUIAutomationTextPattern {
    private readonly IUIAutomationTextRangeArray _selection;
    internal NativeUiaTextPatternFixture(params string[] selections) { _selection = new Ranges(selections); }
    internal UiaPattern Wrap() => new(new UiaElement(new NativeUiaElementFixture()), 10014, this);
    public IUIAutomationTextRangeArray GetSelection() => _selection;
    public IUIAutomationTextRange GetDocumentRange() => new NativeUiaTextRangeFixture(string.Empty);
    public int GetSupportedTextSelection() => throw new NotSupportedException();
    public void Reserved0() => throw new NotSupportedException();
    public void Reserved1() => throw new NotSupportedException();
    public void Reserved3() => throw new NotSupportedException();

    private sealed class Ranges : IUIAutomationTextRangeArray {
        private readonly string[] _values;
        internal Ranges(string[] values) { _values = values; }
        public int GetLength() => _values.Length;
        public IUIAutomationTextRange GetElement(int index) => new NativeUiaTextRangeFixture(_values[index]);
    }
}

internal sealed class NativeUiaCollectionPatternFixture : IUIAutomationSelectionPattern, IUIAutomationTablePattern, IUIAutomationTableItemPattern {
    internal UiaPattern Wrap(int patternId) => new(new UiaElement(new NativeUiaElementFixture()), patternId, this);
    public IUIAutomationElementArray GetCurrentSelection() => new Elements("selected");
    public IUIAutomationElementArray GetCurrentRowHeaders() => new Elements("row");
    public IUIAutomationElementArray GetCurrentColumnHeaders() => new Elements("column");
    public IUIAutomationElementArray GetCurrentRowHeaderItems() => new Elements("row-item");
    public IUIAutomationElementArray GetCurrentColumnHeaderItems() => new Elements("column-item");

    private sealed class Elements : IUIAutomationElementArray {
        private readonly string _name;
        internal Elements(string name) { _name = name; }
        public int GetLength() => 1;
        public IUIAutomationElement GetElement(int index) => index == 0 ? new NativeUiaElementFixture(name: _name) : throw new ArgumentOutOfRangeException(nameof(index));
    }
}
#endif
