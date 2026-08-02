using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace DesktopManager;

[SupportedOSPlatform("windows")]
public static partial class WindowControlService {
    private const int ComboBoxError = -1;
    private const uint WmGetTextLength = 0x000E;
    private const uint CbGetCount = 0x0146;
    private const uint CbGetCurSel = 0x0147;
    private const uint CbGetLbText = 0x0148;
    private const uint CbGetLbTextLen = 0x0149;
    private const uint CbSetCurSel = 0x014E;
    private const int CbnSelChange = 1;

    /// <summary>Returns whether the control exposes a native single-selection list such as a combo box.</summary>
    public static bool SupportsSelection(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Handle == IntPtr.Zero) {
            return false;
        }

        return control.ClassName.IndexOf("combobox", StringComparison.OrdinalIgnoreCase) >= 0 ||
            string.Equals(control.ControlType, "ComboBox", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Retrieves the selected index for a combo-box-style control.</summary>
    public static int GetSelectedIndex(WindowControlInfo control) {
        if (!TryGetSelectedIndex(control, (int)MessageTimeoutMilliseconds, out int selectedIndex)) {
            throw new TimeoutException($"The selected index did not respond within {MessageTimeoutMilliseconds}ms.");
        }

        return selectedIndex;
    }

    internal static bool TryGetSelectedIndex(WindowControlInfo control, int timeoutMilliseconds, out int selectedIndex) {
        ValidateSelectionControl(control);
        selectedIndex = -1;
        if (!TrySendMessageForResult(
                control.Handle,
                CbGetCurSel,
                IntPtr.Zero,
                IntPtr.Zero,
                timeoutMilliseconds,
                out IntPtr result)) {
            return false;
        }

        int value = unchecked((int)result.ToInt64());
        selectedIndex = value == ComboBoxError ? -1 : value;
        return true;
    }

    /// <summary>Retrieves the selected item text when available, otherwise the live combo-box text.</summary>
    public static string GetSelectedValue(WindowControlInfo control) {
        return GetSelectedValueCore(control, maxTextLength: null, out _);
    }

    internal static string GetSelectedValue(WindowControlInfo control, int maxTextLength) {
        return GetSelectedValue(control, maxTextLength, out _);
    }

    internal static string GetSelectedValue(WindowControlInfo control, int maxTextLength, out bool isTruncated) {
        return GetSelectedValueCore(control, maxTextLength, out isTruncated);
    }

    private static string GetSelectedValueCore(WindowControlInfo control, int? maxTextLength, out bool isTruncated) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        isTruncated = false;
        if (control.IsPassword != false) {
            return string.Empty;
        }

        ValidateSelectionControl(control);
        int boundedLength = maxTextLength ?? DesktopTextObservationOptions.MaximumTextLength;
        if (!TryGetSelectedValue(
                control,
                boundedLength,
                (int)MessageTimeoutMilliseconds,
                out string value,
                out isTruncated)) {
            throw new TimeoutException($"The selected value did not respond within {MessageTimeoutMilliseconds}ms.");
        }

        return value;
    }

    internal static bool TryGetSelectedValue(
        WindowControlInfo control,
        int maxTextLength,
        int timeoutMilliseconds,
        out string value,
        out bool isTruncated) {
        ValidateTextLength(maxTextLength);
        ValidateSelectionControl(control);
        value = string.Empty;
        isTruncated = false;
        if (timeoutMilliseconds <= 0 || !RefreshNativeTextSafety(control)) {
            return false;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        if (!TryGetSelectedIndex(control, GetRemainingTimeout(stopwatch, timeoutMilliseconds), out int selectedIndex)) {
            return false;
        }

        return selectedIndex < 0
            ? TryGetControlText(control.Handle, maxTextLength, stopwatch, timeoutMilliseconds, out value, out isTruncated)
            : TryGetComboBoxItemText(
                control.Handle,
                selectedIndex,
                maxTextLength,
                stopwatch,
                timeoutMilliseconds,
                out value,
                out isTruncated,
                out _);
    }

    internal static bool TryGetControlText(
        WindowControlInfo control,
        int maxTextLength,
        int timeoutMilliseconds,
        out string value,
        out bool isTruncated) {
        ValidateTextLength(maxTextLength);
        ValidateSelectionControl(control);
        value = string.Empty;
        isTruncated = false;
        if (timeoutMilliseconds <= 0 || !RefreshNativeTextSafety(control)) {
            return false;
        }

        return TryGetControlText(
            control.Handle,
            maxTextLength,
            Stopwatch.StartNew(),
            timeoutMilliseconds,
            out value,
            out isTruncated);
    }

    /// <summary>Selects a combo-box-style item by its displayed text.</summary>
    public static void SetSelectedValue(WindowControlInfo control, string value) {
        SetSelectedValue(control, value, DesktopTextObservationOptions.MaximumTextLength);
    }

    internal static void SetSelectedValue(WindowControlInfo control, string value, int maxItemTextLength) {
        ValidateSelectionControl(control);
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        ValidateTextLength(maxItemTextLength);
        if (value.Length > maxItemTextLength) {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Selected values are limited to {maxItemTextLength} characters for this lookup.");
        }
        if (control.IsPassword != false) {
            throw new InvalidOperationException("The combo box text cannot be read because its password state is not known to be safe.");
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        if (!TrySendMessageForResult(
                control.Handle,
                CbGetCount,
                IntPtr.Zero,
                IntPtr.Zero,
                GetRemainingTimeout(stopwatch, (int)MessageTimeoutMilliseconds),
                out IntPtr itemCountResult)) {
            throw new TimeoutException($"The combo box item count did not respond within {MessageTimeoutMilliseconds}ms.");
        }

        int itemCount = unchecked((int)itemCountResult.ToInt64());
        if (itemCount == ComboBoxError) {
            throw new InvalidOperationException("The combo box item count could not be resolved.");
        }

        for (int index = 0; index < itemCount; index++) {
            if (!TryGetComboBoxItemText(
                    control.Handle,
                    index,
                    maxItemTextLength,
                    stopwatch,
                    (int)MessageTimeoutMilliseconds,
                    out string itemText,
                    out _,
                    out bool isOversized)) {
                if (isOversized) {
                    continue;
                }

                throw new TimeoutException($"The combo box items did not respond within {MessageTimeoutMilliseconds}ms.");
            }
            if (!string.Equals(itemText, value, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            SetSelectedIndex(control, index, stopwatch, (int)MessageTimeoutMilliseconds);
            return;
        }

        throw new InvalidOperationException($"The combo box does not contain an item named '{value}'.");
    }

    /// <summary>Selects a combo-box-style item by its zero-based index.</summary>
    public static void SetSelectedIndex(WindowControlInfo control, int index) {
        SetSelectedIndex(control, index, Stopwatch.StartNew(), (int)MessageTimeoutMilliseconds);
    }

    private static void SetSelectedIndex(
        WindowControlInfo control,
        int index,
        Stopwatch stopwatch,
        int timeoutMilliseconds) {
        ValidateSelectionControl(control);
        if (index < 0) {
            throw new ArgumentOutOfRangeException(nameof(index), "The selected index must be zero or greater.");
        }

        if (!TrySendMessageForResult(
                control.Handle,
                CbSetCurSel,
                new IntPtr(index),
                IntPtr.Zero,
                GetRemainingTimeout(stopwatch, timeoutMilliseconds),
                out IntPtr selectedIndexResult)) {
            throw new TimeoutException($"The combo box selection did not respond within {timeoutMilliseconds}ms.");
        }

        int result = unchecked((int)selectedIndexResult.ToInt64());
        if (result == ComboBoxError) {
            throw new InvalidOperationException($"The combo box does not expose an item at index {index}.");
        }

        NotifyParentSelectionChanged(control, GetRemainingTimeout(stopwatch, timeoutMilliseconds));
    }

    private static bool TryGetControlText(
        IntPtr handle,
        int maxTextLength,
        Stopwatch stopwatch,
        int timeoutMilliseconds,
        out string value,
        out bool isTruncated) {
        value = string.Empty;
        isTruncated = false;
        if (!TrySendMessageForResult(
                handle,
                WmGetTextLength,
                IntPtr.Zero,
                IntPtr.Zero,
                GetRemainingTimeout(stopwatch, timeoutMilliseconds),
                out IntPtr lengthResult)) {
            return false;
        }

        int reportedLength = Math.Max(0, unchecked((int)lengthResult.ToInt64()));
        int capacity = WindowTextHelper.GetBoundedTextCapacity(reportedLength, maxTextLength);
        var buffer = new StringBuilder(capacity);
        int remainingTimeoutMilliseconds = GetRemainingTimeout(stopwatch, timeoutMilliseconds);
        if (remainingTimeoutMilliseconds <= 0) {
            return false;
        }

        IntPtr sendResult = MonitorNativeMethods.SendMessageTimeout(
            handle,
            MonitorNativeMethods.WM_GETTEXT,
            new IntPtr(buffer.Capacity),
            buffer,
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            (uint)remainingTimeoutMilliseconds,
            out _);
        if (sendResult == IntPtr.Zero) {
            return false;
        }

        value = WindowTextHelper.CreateBoundedTextResult(buffer.ToString(), reportedLength, maxTextLength, out isTruncated);
        return true;
    }

    private static bool TryGetComboBoxItemText(
        IntPtr handle,
        int index,
        int maxTextLength,
        Stopwatch stopwatch,
        int timeoutMilliseconds,
        out string value,
        out bool isTruncated,
        out bool isOversized) {
        value = string.Empty;
        isTruncated = false;
        isOversized = false;
        if (!TrySendMessageForResult(
                handle,
                CbGetLbTextLen,
                new IntPtr(index),
                IntPtr.Zero,
                GetRemainingTimeout(stopwatch, timeoutMilliseconds),
                out IntPtr lengthResult)) {
            return false;
        }

        int itemTextLength = unchecked((int)lengthResult.ToInt64());
        if (itemTextLength < 0) {
            return false;
        }
        if (itemTextLength > maxTextLength) {
            isOversized = true;
            return false;
        }

        var buffer = new StringBuilder(itemTextLength + 1);
        int remainingTimeoutMilliseconds = GetRemainingTimeout(stopwatch, timeoutMilliseconds);
        if (remainingTimeoutMilliseconds <= 0) {
            return false;
        }

        IntPtr sendResult = MonitorNativeMethods.SendMessageTimeout(
            handle,
            CbGetLbText,
            new IntPtr(index),
            buffer,
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            (uint)remainingTimeoutMilliseconds,
            out IntPtr copiedResult);
        if (sendResult == IntPtr.Zero) {
            return false;
        }

        value = buffer.ToString();
        if (unchecked((int)copiedResult.ToInt64()) == ComboBoxError) {
            value = string.Empty;
            return false;
        }
        if (value.Length > maxTextLength) {
            isTruncated = true;
            value = value.Substring(0, maxTextLength);
        }

        return true;
    }

    private static int GetRemainingTimeout(Stopwatch stopwatch, int timeoutMilliseconds) {
        long remaining = timeoutMilliseconds - stopwatch.ElapsedMilliseconds;
        return remaining <= 0 ? 0 : (int)Math.Min(int.MaxValue, remaining);
    }

    private static void NotifyParentSelectionChanged(WindowControlInfo control, int timeoutMilliseconds) {
        if (timeoutMilliseconds <= 0) {
            throw new NativeTextMutationOutcomeUnknownException("WM_COMMAND/CBN_SELCHANGE", timeoutMilliseconds);
        }

        IntPtr parentHandle = control.ParentWindowHandle != IntPtr.Zero
            ? control.ParentWindowHandle
            : MonitorNativeMethods.GetParent(control.Handle);
        if (parentHandle == IntPtr.Zero) {
            return;
        }

        int controlId = control.Id != 0 ? control.Id : MonitorNativeMethods.GetDlgCtrlID(control.Handle);
        int wParam = unchecked((CbnSelChange << 16) | (controlId & 0xFFFF));
        IntPtr sendResult = MonitorNativeMethods.SendMessageTimeout(
            parentHandle,
            WmCommand,
            new IntPtr(wParam),
            control.Handle,
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            (uint)timeoutMilliseconds,
            out _);
        if (sendResult == IntPtr.Zero) {
            throw new NativeTextMutationOutcomeUnknownException("WM_COMMAND/CBN_SELCHANGE", timeoutMilliseconds);
        }
    }

    private static bool TrySendMessageForResult(
        IntPtr handle,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        int timeoutMilliseconds,
        out IntPtr messageResult) {
        messageResult = IntPtr.Zero;
        if (timeoutMilliseconds <= 0) {
            return false;
        }

        IntPtr sendResult = MonitorNativeMethods.SendMessageTimeout(
            handle,
            message,
            wParam,
            lParam,
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            (uint)timeoutMilliseconds,
            out messageResult);
        return sendResult != IntPtr.Zero;
    }

    private static void ValidateSelectionControl(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }
    }

    private static void ValidateTextLength(int maxTextLength) {
        if (maxTextLength < 1 || maxTextLength > DesktopTextObservationOptions.MaximumTextLength) {
            throw new ArgumentOutOfRangeException(nameof(maxTextLength), $"maxTextLength must be between 1 and {DesktopTextObservationOptions.MaximumTextLength}.");
        }
    }
}
