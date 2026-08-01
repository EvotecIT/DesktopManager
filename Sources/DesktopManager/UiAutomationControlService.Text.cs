using System;
using System.Reflection;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    private const int MaximumAutomationParentDepth = 128;

    /// <summary>
    /// Resolves the UI Automation focused element for a target window and reads its text without
    /// requesting an unbounded cross-process document range.
    /// </summary>
    public UiAutomationFocusedControlResult? TryGetFocusedControl(IntPtr windowHandle, IntPtr focusedHandle, int maxLength, string? expectedText = null) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        ValidateTextReadLength(maxLength);
        if (!IsAvailable) {
            return null;
        }

        return RunInSta(service => service.TryGetFocusedControlCore(windowHandle, focusedHandle, maxLength, expectedText), windowHandle);
    }

    /// <summary>
    /// Resolves focus for a same-process window while allowing a WPF dispatcher to service provider calls.
    /// </summary>
    internal UiAutomationFocusedControlResult? TryGetFocusedControlOnCurrentThread(IntPtr windowHandle, IntPtr focusedHandle, int maxLength, string? expectedText = null) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        ValidateTextReadLength(maxLength);
        if (!IsAvailable) {
            return null;
        }

        return RunInSta(service => service.TryGetFocusedControlCore(windowHandle, focusedHandle, maxLength, expectedText), windowHandle);
    }

    /// <summary>
    /// Reads text from a previously resolved UI Automation control using a bounded provider call.
    /// </summary>
    public UiAutomationTextReadResult? TryReadText(WindowInfo window, WindowControlInfo control, int maxLength, string? expectedText = null) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        ValidateTextReadLength(maxLength);
        if (!IsAvailable || (control.Source != WindowControlSource.UiAutomation && !control.HasUiAutomationIdentity)) {
            return null;
        }

        return RunInSta(service => service.TryReadTextCore(window, control, maxLength, expectedText), window.Handle);
    }

    internal static UiAutomationTextReadResult CreateBoundedTextResult(string value, string source, int maxLength, string? expectedText, bool? containsExpected = null) {
        return CreateBoundedTextResult(value, source, maxLength, expectedText, ignoreCase: false, containsExpected: containsExpected);
    }

    internal static UiAutomationTextReadResult CreateBoundedTextResult(
        string value,
        string source,
        int maxLength,
        string? expectedText,
        bool ignoreCase,
        bool? containsExpected = null) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }

        if (string.IsNullOrWhiteSpace(source)) {
            throw new ArgumentException("source is required.", nameof(source));
        }

        ValidateTextReadLength(maxLength);
        bool isTruncated = value.Length > maxLength;
        string observedValue = isTruncated ? value.Substring(0, maxLength) : value;
        bool? observedContainsExpected = string.IsNullOrEmpty(expectedText)
            ? null
            : containsExpected == true || value.IndexOf(expectedText, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0
                ? true
                : null;

        return new UiAutomationTextReadResult {
            Value = observedValue,
            Source = source,
            IsTruncated = isTruncated,
            ContainsExpected = observedContainsExpected
        };
    }

    internal static UiAutomationTextReadResult ReadTextPatternRange(object documentRange, int maxLength, string? expectedText) {
        return ReadTextPatternRange(documentRange, maxLength, expectedText, ignoreCase: false);
    }

    internal static UiAutomationTextReadResult ReadTextPatternRange(object documentRange, int maxLength, string? expectedText, bool ignoreCase) {
        if (documentRange == null) {
            throw new ArgumentNullException(nameof(documentRange));
        }

        ValidateTextReadLength(maxLength);
        int providerLimit = maxLength == int.MaxValue ? int.MaxValue : maxLength + 1;
        MethodInfo? getTextMethod = documentRange.GetType().GetMethod("GetText", new[] { typeof(int) });
        string value = getTextMethod?.Invoke(documentRange, new object[] { providerLimit }) as string ?? string.Empty;
        bool? containsExpected = null;
        if (!string.IsNullOrEmpty(expectedText)) {
            if (value.IndexOf(expectedText, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0) {
                containsExpected = true;
            } else {
                MethodInfo? findTextMethod = documentRange.GetType().GetMethod("FindText", new[] { typeof(string), typeof(bool), typeof(bool) });
                containsExpected = findTextMethod?.Invoke(documentRange, new object[] { expectedText!, false, ignoreCase }) != null
                    ? true
                    : null;
            }
        }

        return CreateBoundedTextResult(value, "uia.textPattern", maxLength, expectedText, ignoreCase, containsExpected);
    }

    private UiAutomationFocusedControlResult? TryGetFocusedControlCore(IntPtr windowHandle, IntPtr focusedHandle, int maxLength, string? expectedText) {
        object? element = TryGetAutomationFocusedElement();
        bool belongsToWindow = element != null && BelongsToWindow(element, windowHandle);
        bool isRootWindow = element != null && belongsToWindow && IsRootWindowElement(element, windowHandle);
        if (element == null || !belongsToWindow || isRootWindow) {
            element = ResolveFocusedElementCandidate(
                element,
                belongsToWindow,
                isRootWindow,
                TryFindFocusedDescendant(windowHandle));
        }

        if (element == null || !BelongsToWindow(element, windowHandle)) {
            element = null;
            if (focusedHandle != IntPtr.Zero &&
                focusedHandle != windowHandle &&
                TryResolveRootElement(focusedHandle, out object? handleElement) &&
                handleElement != null &&
                BelongsToWindow(handleElement, windowHandle) &&
                !IsRootWindowElement(handleElement, windowHandle)) {
                element = handleElement;
            }
        }

        if (element == null) {
            return null;
        }

        WindowControlInfo? control;
        try {
            control = CreateControlInfo(element, readValue: false);
        } catch {
            return null;
        }

        if (control == null) {
            return null;
        }

        return new UiAutomationFocusedControlResult {
            Control = control,
            Text = ReadElementText(element, maxLength, expectedText)
        };
    }

    internal static object? ResolveFocusedElementCandidate(
        object? focusedElement,
        bool belongsToWindow,
        bool isRootWindow,
        object? focusedDescendant) {
        return focusedElement != null && belongsToWindow && !isRootWindow
            ? focusedElement
            : focusedDescendant;
    }

    private UiAutomationTextReadResult? TryReadTextCore(WindowInfo window, WindowControlInfo control, int maxLength, string? expectedText) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        return match.Element == null ? null : ReadElementText(match.Element, maxLength, expectedText);
    }

    private UiAutomationTextReadResult? ReadElementText(object element, int maxLength, string? expectedText, bool ignoreCase = false) {
        object? current;
        try {
            current = element.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
        } catch {
            return null;
        }

        if (current == null) {
            return null;
        }

        if (!TryReadPasswordState(current, out bool? isPassword)) {
            return null;
        }

        if (isPassword == true) {
            return new UiAutomationTextReadResult {
                Source = "uia.password",
                IsPassword = true
            };
        }

        UiAutomationTextReadResult? textPatternValue = ReadTextPatternValue(element, maxLength, expectedText, ignoreCase);
        if (textPatternValue != null) {
            return textPatternValue;
        }

        string? valuePatternValue = ReadPatternValue(element, "System.Windows.Automation.ValuePattern");
        if (IsPatternResultAvailable(valuePatternValue)) {
            return CreateBoundedTextResult(valuePatternValue!, "uia.valuePattern", maxLength, expectedText, ignoreCase);
        }

        string? rangeValue = ReadPatternValue(element, "System.Windows.Automation.RangeValuePattern");
        if (rangeValue != null) {
            return CreateBoundedTextResult(rangeValue, "uia.rangeValuePattern", maxLength, expectedText, ignoreCase);
        }

        string? legacyValue = ReadPatternValue(element, "System.Windows.Automation.LegacyIAccessiblePattern");
        if (legacyValue != null) {
            return CreateBoundedTextResult(legacyValue, "uia.legacyValuePattern", maxLength, expectedText, ignoreCase);
        }

        return null;
    }

    internal static bool IsPatternResultAvailable(string? value) {
        return value != null;
    }

    internal static bool TryReadPasswordState(object current, out bool? isPassword) {
        try {
            isPassword = ReadNullableBoolean(current, "IsPassword");
            return isPassword.HasValue;
        } catch {
            isPassword = null;
            return false;
        }
    }

    private UiAutomationTextReadResult? ReadTextPatternValue(object element, int maxLength, string? expectedText, bool ignoreCase) {
        try {
            Type? textPatternType = _automationClientAssembly?.GetType("System.Windows.Automation.TextPattern", throwOnError: false);
            if (textPatternType == null) {
                return null;
            }

            object? pattern = GetCurrentPattern(element, textPatternType);
            object? documentRange = pattern?.GetType().GetProperty("DocumentRange", BindingFlags.Public | BindingFlags.Instance)?.GetValue(pattern);
            if (documentRange == null) {
                return null;
            }

            return ReadTextPatternRange(documentRange, maxLength, expectedText, ignoreCase);
        } catch {
            return null;
        }
    }

    private object? TryGetAutomationFocusedElement() {
        try {
            return _automationElementType?.GetProperty("FocusedElement", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        } catch {
            return null;
        }
    }

    private object? TryFindFocusedDescendant(IntPtr windowHandle) {
        if (!TryResolveRootElement(windowHandle, out object? rootElement) || rootElement == null) {
            return null;
        }

        return TryFindElementByProperty(rootElement, "HasKeyboardFocusProperty", true, out object? focusedElement)
            ? focusedElement
            : null;
    }

    private static bool IsRootWindowElement(object element, IntPtr windowHandle) {
        try {
            object? current = element.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
            int nativeHandle = current == null ? 0 : ReadInt32(current, "NativeWindowHandle");
            return nativeHandle != 0 && new IntPtr(nativeHandle) == windowHandle;
        } catch {
            return false;
        }
    }

    private bool BelongsToWindow(object element, IntPtr windowHandle) {
        object? current = element;
        object? walker = _automationClientAssembly?.GetType("System.Windows.Automation.TreeWalker", throwOnError: false)?
            .GetProperty("RawViewWalker", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        MethodInfo? getParentMethod = walker?.GetType().GetMethod("GetParent", new[] { _automationElementType! });

        for (int depth = 0; current != null && depth < MaximumAutomationParentDepth; depth++) {
            try {
                object? currentProperties = current.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(current);
                int nativeHandle = currentProperties == null ? 0 : ReadInt32(currentProperties, "NativeWindowHandle");
                if (nativeHandle != 0) {
                    IntPtr candidateHandle = new(nativeHandle);
                    IntPtr candidateRoot = MonitorNativeMethods.GetAncestor(candidateHandle, MonitorNativeMethods.GA_ROOT);
                    if (candidateHandle == windowHandle || candidateRoot == windowHandle) {
                        return true;
                    }
                }

                current = getParentMethod?.Invoke(walker, new[] { current });
            } catch {
                break;
            }
        }

        return IsFocusedElementInsideForegroundWindow(element, windowHandle);
    }

    private static bool IsFocusedElementInsideForegroundWindow(object element, IntPtr windowHandle) {
        if (MonitorNativeMethods.GetForegroundWindow() != windowHandle ||
            !MonitorNativeMethods.GetWindowRect(windowHandle, out RECT windowRect)) {
            return false;
        }

        MonitorNativeMethods.GetWindowThreadProcessId(windowHandle, out uint windowProcessId);
        try {
            object? current = element.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
            if (current == null || ReadInt32(current, "ProcessId") != (int)windowProcessId) {
                return false;
            }

            (int left, int top, int width, int height) = ReadBounds(current);
            int right = left + width;
            int bottom = top + height;
            return width > 0 && height > 0 &&
                right > windowRect.Left && left < windowRect.Right &&
                bottom > windowRect.Top && top < windowRect.Bottom;
        } catch {
            return false;
        }
    }

    private static void ValidateTextReadLength(int maxLength) {
        if (maxLength < 1 || maxLength > DesktopTextObservationOptions.MaximumTextLength) {
            throw new ArgumentOutOfRangeException(nameof(maxLength), $"maxLength must be between 1 and {DesktopTextObservationOptions.MaximumTextLength}.");
        }
    }
}
