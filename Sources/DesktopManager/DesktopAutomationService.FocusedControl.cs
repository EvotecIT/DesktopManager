using System;
using System.Diagnostics;

namespace DesktopManager;

public sealed partial class DesktopAutomationService {
    private const int DefaultNativeTextReadTimeoutMilliseconds = 1000;

    /// <summary>
    /// Observes the currently focused control for the first matching window when it can be resolved.
    /// </summary>
    public DesktopFocusedControlObservation? GetFocusedControlObservation(WindowQueryOptions options) {
        return GetFocusedControlObservation(options, 2048, null);
    }

    /// <summary>
    /// Observes the currently focused control and bounds any UI Automation document text returned.
    /// </summary>
    /// <param name="options">Window selection criteria.</param>
    /// <param name="maxObservedTextLength">Maximum number of text characters to return.</param>
    /// <param name="expectedText">Optional text to search for across the complete provider document range.</param>
    /// <returns>The focused-control observation when available; otherwise null.</returns>
    public DesktopFocusedControlObservation? GetFocusedControlObservation(WindowQueryOptions options, int maxObservedTextLength, string? expectedText = null) {
        return GetFocusedControlObservation(options, maxObservedTextLength, expectedText, getRemainingProviderTimeoutMilliseconds: null);
    }

    private DesktopFocusedControlObservation? GetFocusedControlObservation(
        WindowQueryOptions options,
        int maxObservedTextLength,
        string? expectedText,
        Func<int>? getRemainingProviderTimeoutMilliseconds) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (maxObservedTextLength < 1 || maxObservedTextLength > DesktopTextObservationOptions.MaximumTextLength) {
            throw new ArgumentOutOfRangeException(nameof(maxObservedTextLength), $"maxObservedTextLength must be between 1 and {DesktopTextObservationOptions.MaximumTextLength}.");
        }

        WindowInfo? window = TryResolveSingleWindow(options);
        if (window == null) {
            return null;
        }

        IntPtr focusedHandle = WindowActivationService.GetFocusedControlHandle(window.Handle);
        MonitorNativeMethods.GetWindowThreadProcessId(window.Handle, out uint windowProcessId);
        bool isCurrentProcessWindow = windowProcessId == (uint)Process.GetCurrentProcess().Id;
        int providerTimeoutMilliseconds = getRemainingProviderTimeoutMilliseconds?.Invoke() ??
            UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds;
        UiAutomationFocusedControlResult? automationResult = providerTimeoutMilliseconds <= 0
            ? null
            : isCurrentProcessWindow
                ? _uiAutomationControlService.TryGetFocusedControlOnCurrentThread(
                    window.Handle,
                    focusedHandle,
                    maxObservedTextLength,
                    expectedText,
                    providerTimeoutMilliseconds)
                : _uiAutomationControlService.TryGetFocusedControl(
                    window.Handle,
                    focusedHandle,
                    maxObservedTextLength,
                    expectedText,
                    providerTimeoutMilliseconds);
        WindowControlInfo? control = automationResult?.Control;
        if (control != null) {
            control.ParentWindowHandle = window.Handle;
        } else if (focusedHandle != IntPtr.Zero && focusedHandle != window.Handle) {
            control = new ControlEnumerator().GetControlMetadata(window.Handle, focusedHandle);
        }

        if (control == null) {
            return null;
        }

        UiAutomationTextReadResult? automationText = automationResult?.Text;
        bool isPassword = automationText?.IsPassword == true || control.IsPassword == true;
        bool canAccessText = automationText != null
            ? !automationText.IsPassword
            : control.IsPassword == false;
        string liveText = string.Empty;
        bool nativeTextTruncated = false;
        bool nativeTextAvailable = false;
        IntPtr nativeTextHandle = ResolveNativeTextHandle(automationResult, focusedHandle);
        if (canAccessText && automationText == null && nativeTextHandle != IntPtr.Zero) {
            nativeTextAvailable = TryReadFocusedNativeText(
                control,
                maxObservedTextLength,
                getRemainingProviderTimeoutMilliseconds,
                out liveText,
                out nativeTextTruncated);
        }
        string controlText = !canAccessText
            ? string.Empty
            : nativeTextAvailable
                ? liveText
                : control.Text;
        string valueSource = string.Empty;
        string value = !canAccessText
            ? string.Empty
            : automationText == null && nativeTextAvailable
                ? liveText
                : ResolveFocusedValue(automationText, control, liveText, out valueSource);
        if (automationText == null && nativeTextAvailable) {
            valueSource = "native.windowText";
        }
        if (isPassword) {
            valueSource = "uia.password";
        }

        return new DesktopFocusedControlObservation {
            WindowHandle = window.Handle,
            WindowTitle = window.Title,
            FocusedHandle = ResolveFocusedControlHandle(automationResult, focusedHandle),
            ClassName = control.ClassName,
            AutomationId = control.AutomationId,
            ControlType = control.ControlType,
            Text = controlText,
            Value = value,
            ValueSource = valueSource,
            IsValueTruncated = automationText?.IsTruncated == true || nativeTextTruncated,
            ContainsExpected = canAccessText
                ? automationText?.ContainsExpected ?? ResolveExpectedTextMatch(expectedText, liveText, value)
                : null,
            IsPassword = isPassword ? true : control.IsPassword,
            IsKeyboardFocusable = control.IsKeyboardFocusable,
            IsEnabled = control.IsEnabled
        };
    }

    internal static bool TryReadFocusedNativeText(
        WindowControlInfo control,
        int maxObservedTextLength,
        Func<int>? getRemainingProviderTimeoutMilliseconds,
        out string value,
        out bool isTruncated) {
        int nativeTimeoutMilliseconds = getRemainingProviderTimeoutMilliseconds?.Invoke() ??
            DefaultNativeTextReadTimeoutMilliseconds;
        return WindowControlService.TryGetControlText(
            control,
            maxObservedTextLength,
            nativeTimeoutMilliseconds,
            out value,
            out isTruncated);
    }

    internal static IntPtr ResolveFocusedControlHandle(UiAutomationFocusedControlResult? automationResult, IntPtr nativeFocusedHandle) {
        return automationResult != null
            ? automationResult.Control?.Handle ?? IntPtr.Zero
            : nativeFocusedHandle;
    }

    internal static IntPtr ResolveNativeTextHandle(UiAutomationFocusedControlResult? automationResult, IntPtr nativeFocusedHandle) {
        return automationResult != null
            ? automationResult.Control?.Handle ?? IntPtr.Zero
            : nativeFocusedHandle;
    }

    internal static string ResolveFocusedValue(
        UiAutomationTextReadResult? automationText,
        WindowControlInfo? control,
        string liveText,
        out string valueSource) {
        if (automationText != null) {
            valueSource = automationText.Source;
            return automationText.Value;
        }

        if (!string.IsNullOrEmpty(liveText)) {
            valueSource = "native.windowText";
            return liveText;
        }

        string? controlValue = control?.Value;
        if (!string.IsNullOrEmpty(controlValue)) {
            valueSource = "native.windowText";
            return controlValue!;
        }

        valueSource = string.Empty;
        return string.Empty;
    }

    internal static bool? ResolveExpectedTextMatch(string? expectedText, params string?[] candidateValues) {
        if (string.IsNullOrEmpty(expectedText)) {
            return null;
        }

        foreach (string? candidateValue in candidateValues) {
            if (candidateValue?.IndexOf(expectedText, StringComparison.Ordinal) >= 0) {
                return true;
            }
        }

        return null;
    }
}
