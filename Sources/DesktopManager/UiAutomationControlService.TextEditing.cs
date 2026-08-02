using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    /// <summary>
    /// Pastes text into the current selection or caret after resolving and focusing the exact UI Automation element.
    /// </summary>
    internal UiAutomationTextEditAttempt TryPasteTextAtSelection(
        WindowInfo window,
        WindowControlInfo control,
        string value,
        bool ensureForegroundWindow,
        bool selectCaretRange,
        bool deleteSelectionWhenEmpty,
        string expectedEditContextFingerprint,
        string expectedContentFingerprint,
        int? expectedCaretOffset,
        int maxTextLength) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }

        if (string.IsNullOrWhiteSpace(expectedEditContextFingerprint)) {
            throw new ArgumentException("An edit context fingerprint is required.", nameof(expectedEditContextFingerprint));
        }

        if (string.IsNullOrWhiteSpace(expectedContentFingerprint)) {
            throw new ArgumentException("A content fingerprint is required.", nameof(expectedContentFingerprint));
        }

        if (selectCaretRange && !expectedCaretOffset.HasValue) {
            throw new ArgumentException("An expected caret offset is required for insertion.", nameof(expectedCaretOffset));
        }

        ValidateTextReadLength(maxTextLength);

        return RunInSta(service => service.TryPasteTextAtSelectionCore(
            window,
            control,
            value,
            ensureForegroundWindow,
            selectCaretRange,
            deleteSelectionWhenEmpty,
            expectedEditContextFingerprint,
            expectedContentFingerprint,
            expectedCaretOffset,
            maxTextLength), window.Handle, isMutation: true);
    }

    private UiAutomationTextEditAttempt TryPasteTextAtSelectionCore(
        WindowInfo window,
        WindowControlInfo control,
        string value,
        bool ensureForegroundWindow,
        bool selectCaretRange,
        bool deleteSelectionWhenEmpty,
        string expectedEditContextFingerprint,
        string expectedContentFingerprint,
        int? expectedCaretOffset,
        int maxTextLength) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return UiAutomationTextEditAttempt.Failed("control-not-found");
        }

        object? current;
        try {
            current = element.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
        } catch {
            return UiAutomationTextEditAttempt.Failed("provider-unavailable");
        }

        if (current == null || !TryReadPasswordState(current, out bool? isPassword) || isPassword != false) {
            return UiAutomationTextEditAttempt.Failed("password-state-unavailable");
        }

        if (!TryPrepareForegroundAndFocus(
                ensureForegroundWindow,
                () => WindowActivationService.TryPrepareWindowForAutomation(window.Handle),
                () => TryPatternAction(element, "System.Windows.Automation.ScrollItemPattern", "ScrollIntoView"),
                () => TrySetFocus(element),
                out bool foregroundPreparationFailed)) {
            return UiAutomationTextEditAttempt.Failed(foregroundPreparationFailed ? "foreground-failed" : "focus-failed");
        }

        if (MonitorNativeMethods.GetForegroundWindow() != window.Handle) {
            return UiAutomationTextEditAttempt.Failed("foreground-required");
        }

        DesktopControlTextObservation currentContext = ReadCurrentEditContext(element, maxTextLength);
        string currentEditContextFingerprint = currentContext.EditContextFingerprint;
        if (string.IsNullOrWhiteSpace(currentEditContextFingerprint) ||
            !string.Equals(currentEditContextFingerprint, expectedEditContextFingerprint, StringComparison.OrdinalIgnoreCase)) {
            return UiAutomationTextEditAttempt.Failed("edit-context-changed", currentEditContextFingerprint);
        }

        if (selectCaretRange) {
            if (!expectedCaretOffset.HasValue || !TrySelectCurrentCaret(element)) {
                return UiAutomationTextEditAttempt.Failed("caret-unavailable");
            }

            DesktopControlTextObservation collapsedContext = ReadCurrentEditContext(element, maxTextLength);
            if (!IsExpectedCollapsedCaret(collapsedContext, expectedContentFingerprint, expectedCaretOffset.Value)) {
                return UiAutomationTextEditAttempt.Failed("edit-context-changed", collapsedContext.EditContextFingerprint);
            }
        }

        if (!IsTextMutationAllowed(element)) {
            return UiAutomationTextEditAttempt.Failed("password-state-unavailable");
        }

        if (value.Length == 0) {
            if (deleteSelectionWhenEmpty) {
                if (!IsExpectedForegroundInputTarget(window, control, element)) {
                    return UiAutomationTextEditAttempt.Failed("input-target-changed");
                }

                KeyboardInputService.SendToForeground(VirtualKey.VK_DELETE);
            }

            return UiAutomationTextEditAttempt.Succeeded();
        }

        if (!IsTextMutationAllowed(element)) {
            return UiAutomationTextEditAttempt.Failed("password-state-unavailable");
        }

        if (!IsExpectedForegroundInputTarget(window, control, element)) {
            return UiAutomationTextEditAttempt.Failed("input-target-changed");
        }

        KeyboardInputService.SendTextToForeground(value);
        return UiAutomationTextEditAttempt.Succeeded();
    }

    internal static bool TryPrepareForegroundAndFocus(
        bool ensureForegroundWindow,
        Func<bool> prepareForeground,
        Action prepareElement,
        Func<bool> focusElement,
        out bool foregroundPreparationFailed) {
        foregroundPreparationFailed = false;
        if (ensureForegroundWindow && !prepareForeground()) {
            foregroundPreparationFailed = true;
            return false;
        }

        prepareElement();
        return focusElement();
    }

    private bool? TryReadResolvedPasswordStateCore(WindowInfo window, WindowControlInfo control) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        return match.Element == null ? null : TryReadElementPasswordState(match.Element);
    }

    internal static bool IsTextMutationAllowed(object element) {
        return element != null && TryReadElementPasswordState(element) == false;
    }

    private static bool IsExpectedForegroundInputTarget(
        WindowInfo window,
        WindowControlInfo control,
        object element) {
        try {
            object? current = element.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
            bool? hasKeyboardFocus = current == null
                ? null
                : ReadObservationBoolean(current, "HasKeyboardFocus", new List<string>());
            int nativeHandle = current == null ? 0 : ReadInt32(current, "NativeWindowHandle");
            return MatchesForegroundInputTarget(
                window.Handle,
                control.Handle,
                MonitorNativeMethods.GetForegroundWindow(),
                WindowActivationService.GetFocusedControlHandle(window.Handle),
                hasKeyboardFocus,
                nativeHandle);
        } catch {
            return false;
        }
    }

    internal static bool MatchesForegroundInputTarget(
        IntPtr expectedWindowHandle,
        IntPtr expectedControlHandle,
        IntPtr foregroundWindowHandle,
        IntPtr focusedControlHandle,
        bool? hasKeyboardFocus,
        int elementNativeHandle) {
        if (expectedWindowHandle == IntPtr.Zero ||
            foregroundWindowHandle != expectedWindowHandle ||
            hasKeyboardFocus != true) {
            return false;
        }

        if (expectedControlHandle == IntPtr.Zero) {
            return true;
        }

        return new IntPtr(elementNativeHandle) == expectedControlHandle &&
            focusedControlHandle == expectedControlHandle;
    }

    private static bool? TryReadElementPasswordState(object element) {
        try {
            object? current = element.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
            return current != null && TryReadPasswordState(current, out bool? isPassword)
                ? isPassword
                : null;
        } catch {
            return null;
        }
    }

    internal static bool IsExpectedCollapsedCaret(
        DesktopControlTextObservation observation,
        string expectedContentFingerprint,
        int expectedCaretOffset) {
        if (!observation.IsComplete ||
            !observation.AreSelectionRangesComplete ||
            !observation.IsActiveCompositionComplete ||
            !observation.IsConversionTargetComplete ||
            !string.Equals(observation.ContentFingerprint, expectedContentFingerprint, StringComparison.OrdinalIgnoreCase) ||
            observation.CaretOffset != expectedCaretOffset) {
            return false;
        }

        foreach (DesktopTextRangeObservation range in observation.SelectionRanges) {
            if (range.Length != 0 || range.Offset != expectedCaretOffset) {
                return false;
            }
        }

        return true;
    }

    private DesktopControlTextObservation ReadCurrentEditContext(object element, int maxTextLength) {
        var errors = new System.Collections.Generic.List<string>();
        Dictionary<string, object> patterns = ReadObservationPatterns(element, errors);
        var options = new DesktopControlObservationOptions {
            MaxTextLength = maxTextLength,
            IncludeTextRanges = true,
            IncludeSemanticState = false
        };
        return ReadControlTextObservation(element, patterns, options, errors);
    }

    private bool TrySelectCurrentCaret(object element) {
        try {
            Type? textPattern2Type = _automationClientAssembly?.GetType("System.Windows.Automation.TextPattern2", throwOnError: false);
            object? pattern = textPattern2Type == null ? null : GetCurrentPattern(element, textPattern2Type);
            MethodInfo? getCaretRange = pattern?.GetType().GetMethod("GetCaretRange");
            if (getCaretRange == null) {
                return TrySelectCollapsedTextSelection(element);
            }

            object?[] arguments = { false };
            object? caretRange = getCaretRange.Invoke(pattern, arguments);
            MethodInfo? select = caretRange?.GetType().GetMethod("Select", Type.EmptyTypes);
            if (caretRange == null || select == null) {
                return false;
            }

            select.Invoke(caretRange, null);
            return true;
        } catch {
            return TrySelectCollapsedTextSelection(element);
        }
    }

    private bool TrySelectCollapsedTextSelection(object element) {
        try {
            Type? textPatternType = _automationClientAssembly?.GetType("System.Windows.Automation.TextPattern", throwOnError: false);
            object? pattern = textPatternType == null ? null : GetCurrentPattern(element, textPatternType);
            object? result = pattern?.GetType().GetMethod("GetSelection", Type.EmptyTypes)?.Invoke(pattern, null);
            if (result is not IEnumerable ranges) {
                return false;
            }

            object? collapsedRange = null;
            int count = 0;
            foreach (object? range in ranges) {
                if (range == null) {
                    continue;
                }

                count++;
                if (count > 1 || ReadTextRange(range, 1, out bool truncated).Length != 0 || truncated) {
                    return false;
                }

                collapsedRange = range;
            }

            MethodInfo? select = collapsedRange?.GetType().GetMethod("Select", Type.EmptyTypes);
            if (count != 1 || select == null) {
                return false;
            }

            select.Invoke(collapsedRange, null);
            return true;
        } catch {
            return false;
        }
    }
}

internal sealed class UiAutomationTextEditAttempt {
    internal bool Applied { get; private set; }
    internal string FailureCode { get; private set; } = string.Empty;
    internal string ObservedEditContextFingerprint { get; private set; } = string.Empty;
    internal string ObservedContentFingerprint { get; private set; } = string.Empty;

    internal static UiAutomationTextEditAttempt Succeeded() {
        return new UiAutomationTextEditAttempt { Applied = true };
    }

    internal static UiAutomationTextEditAttempt Failed(
        string failureCode,
        string? observedEditContextFingerprint = null,
        string? observedContentFingerprint = null) {
        return new UiAutomationTextEditAttempt {
            FailureCode = failureCode,
            ObservedEditContextFingerprint = observedEditContextFingerprint ?? string.Empty,
            ObservedContentFingerprint = observedContentFingerprint ?? string.Empty
        };
    }
}
