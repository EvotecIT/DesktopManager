#if NET8_0_OR_GREATER
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace DesktopManager;

// Native vtable order follows Windows SDK UIAutomationClient.h; IUnknown slots are generated.
// Reserved entries retain unused vtable slots and must never be invoked.
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee" )]
internal partial interface IUIAutomation {
    void Reserved0();
    void Reserved1();
    IUIAutomationElement GetRootElement();
    IUIAutomationElement ElementFromHandle(IntPtr hwnd);
    void Reserved4();
    IUIAutomationElement GetFocusedElement();
    void Reserved6();
    void Reserved7();
    void Reserved8();
    void Reserved9();
    void Reserved10();
    IUIAutomationTreeWalker GetControlViewWalker();
    void Reserved12();
    IUIAutomationTreeWalker GetRawViewWalker();
    void Reserved14();
    void Reserved15();
    void Reserved16();
    void Reserved17();
    IUIAutomationCondition CreateTrueCondition();
    void Reserved19();
    IUIAutomationCondition CreatePropertyCondition(int id, UiaVariant value);
    void Reserved21();
    void Reserved22();
    void Reserved23();
    void Reserved24();
    void Reserved25();
    void Reserved26();
    void Reserved27();
    void Reserved28();
    void AddAutomationEventHandler(int id, IUIAutomationElement element, int scope, IntPtr cache, IUiaEventHandler handler);
    void RemoveAutomationEventHandler(int id, IUIAutomationElement element, IUiaEventHandler handler);
    void AddPropertyChangedEventHandlerNativeArray(IUIAutomationElement element, int scope, IntPtr cache, IUiaPropertyHandler handler, IntPtr properties, int count);
    void Reserved32();
    void RemovePropertyChangedEventHandler(IUIAutomationElement element, IUiaPropertyHandler handler);
    void AddStructureChangedEventHandler(IUIAutomationElement element, int scope, IntPtr cache, IUiaStructureHandler handler);
    void RemoveStructureChangedEventHandler(IUIAutomationElement element, IUiaStructureHandler handler);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e" )]
internal partial interface IUIAutomationElement {
    void SetFocus();
    IntPtr GetRuntimeId();
    IUIAutomationElement? FindFirst(int scope, IUIAutomationCondition condition);
    IUIAutomationElementArray FindAll(int scope, IUIAutomationCondition condition);
    void Reserved4();
    void Reserved5();
    void Reserved6();
    void Reserved7();
    void GetCurrentPropertyValueEx(int id, int ignoreDefaultValue, out UiaVariant value);
    void Reserved9();
    void Reserved10();
    void Reserved11();
    void Reserved12();
    IntPtr GetCurrentPattern(int id);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("352ffba8-0973-437c-a61f-f64cafd81df9" )]
internal partial interface IUIAutomationCondition {
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("14314595-b4bc-4055-95f2-58f2e42c9855" )]
internal partial interface IUIAutomationElementArray {
    int GetLength();
    IUIAutomationElement GetElement(int index);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("4042c624-389c-4afc-a630-9df854a541fc" )]
internal partial interface IUIAutomationTreeWalker {
    IUIAutomationElement? GetParentElement(IUIAutomationElement element);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("a543cc6a-f4ae-494b-8239-c814481187a8" )]
internal partial interface IUIAutomationTextRange {
    IUIAutomationTextRange Clone();
    void Reserved1();
    void Reserved2();
    void ExpandToEnclosingUnit(int unit);
    void Reserved4();
    IUIAutomationTextRange? FindText([MarshalAs(UnmanagedType.BStr)] string text, int backward, int ignoreCase);
    void Reserved6();
    void Reserved7();
    void Reserved8();
    [return: MarshalAs(UnmanagedType.BStr)] string GetText(int maxLength);
    void Reserved10();
    void Reserved11();
    void MoveEndpointByRange(int endpoint, IUIAutomationTextRange range, int targetEndpoint);
    void Select();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("ce4ae76a-e717-4c98-81ea-47371d028eb6" )]
internal partial interface IUIAutomationTextRangeArray {
    int GetLength();
    IUIAutomationTextRange GetElement(int index);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("32eba289-3583-42c9-9c59-3b6d9a1e9b6a" )]
internal partial interface IUIAutomationTextPattern {
    void Reserved0();
    void Reserved1();
    IUIAutomationTextRangeArray GetSelection();
    void Reserved3();
    IUIAutomationTextRange GetDocumentRange();
    int GetSupportedTextSelection();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("17E21576-996C-4870-99D9-BFF323380C06" )]
internal partial interface IUIAutomationTextEditPattern {
    void BaseReserved0();
    void BaseReserved1();
    IUIAutomationTextRangeArray GetSelection();
    void BaseReserved3();
    IUIAutomationTextRange GetDocumentRange();
    int GetSupportedTextSelection();
    IUIAutomationTextRange? GetActiveComposition();
    IUIAutomationTextRange? GetConversionTarget();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("506a921a-fcc9-409f-b23b-37eb74106872" )]
internal partial interface IUIAutomationTextPattern2 {
    void BaseReserved0();
    void BaseReserved1();
    IUIAutomationTextRangeArray GetSelection();
    void BaseReserved3();
    IUIAutomationTextRange GetDocumentRange();
    int GetSupportedTextSelection();
    void Reserved0();
    IUIAutomationTextRange? GetCaretRange(out int active);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("fb377fbe-8ea6-46d5-9c73-6499642d3059" )]
internal partial interface IUIAutomationInvokePattern {
    void Invoke();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("a94cd8b1-0844-4cd6-9d2d-640537ab39e9" )]
internal partial interface IUIAutomationValuePattern {
    void SetValue([MarshalAs(UnmanagedType.BStr)] string value);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("94cf8058-9b8d-4ab9-8bfd-4cd0a33c8c70" )]
internal partial interface IUIAutomationTogglePattern {
    void Toggle();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("619be086-1f4e-4ee4-bafa-210128738730" )]
internal partial interface IUIAutomationExpandCollapsePattern {
    void Expand();
    void Collapse();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("a8efa66a-0fda-421a-9194-38021f3578ea" )]
internal partial interface IUIAutomationSelectionItemPattern {
    void Select();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("b488300f-d015-4f19-9c29-bb595e3645ef" )]
internal partial interface IUIAutomationScrollItemPattern {
    void ScrollIntoView();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("6ba3d7a6-04cf-4f11-8793-a8d1cde9969f" )]
internal partial interface IUIAutomationVirtualizedItemPattern {
    void Realize();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("828055ad-355b-4435-86d5-3b51c14a9b1b" )]
internal partial interface IUIAutomationLegacyIAccessiblePattern {
    void Reserved0();
    void DoDefaultAction();
    void SetValue([MarshalAs(UnmanagedType.LPWStr)] string value);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("5ed5202e-b2ac-47a6-b638-4b0bf140d78e" )]
internal partial interface IUIAutomationSelectionPattern {
    IUIAutomationElementArray GetCurrentSelection();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("620e691c-ea96-4710-a850-754b24ce2417" )]
internal partial interface IUIAutomationTablePattern {
    IUIAutomationElementArray GetCurrentRowHeaders();
    IUIAutomationElementArray GetCurrentColumnHeaders();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("0b964eb3-ef2e-4464-9c79-61d61737a27e" )]
internal partial interface IUIAutomationTableItemPattern {
    IUIAutomationElementArray GetCurrentRowHeaderItems();
    IUIAutomationElementArray GetCurrentColumnHeaderItems();
}

#endif
