using System;
using System.Runtime.Versioning;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Provides helper methods for interacting with window controls.
/// </summary>
[SupportedOSPlatform("windows")]
public static partial class WindowControlService {
    private const uint MessageTimeoutMilliseconds = 1000;
    private const uint WmCommand = 0x0111;
    private const long ButtonStyleMask = 0x0000000F;
    private const long ButtonStyleCheckBox = 0x00000002;
    private const long ButtonStyleAutoCheckBox = 0x00000003;
    private const long ButtonStyleRadioButton = 0x00000004;
    private const long ButtonStyleThreeState = 0x00000005;
    private const long ButtonStyleAutoThreeState = 0x00000006;
    private const long ButtonStyleAutoRadioButton = 0x00000009;

    /// <summary>Refreshes native class/style metadata immediately before a text read and clears stale values unless safety is explicit.</summary>
    internal static bool RefreshNativeTextSafety(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Handle == IntPtr.Zero) {
            control.IsPassword = null;
            control.Text = string.Empty;
            control.Value = string.Empty;
            return false;
        }

        if (control.ParentWindowHandle != IntPtr.Zero) {
            MonitorNativeMethods.GetWindowThreadProcessId(control.ParentWindowHandle, out uint parentProcessId);
            MonitorNativeMethods.GetWindowThreadProcessId(control.Handle, out uint controlProcessId);
            if (parentProcessId == 0 ||
                controlProcessId == 0 ||
                parentProcessId != controlProcessId ||
                MonitorNativeMethods.GetAncestor(control.Handle, MonitorNativeMethods.GA_ROOT) != control.ParentWindowHandle) {
                control.IsPassword = null;
                control.Text = string.Empty;
                control.Value = string.Empty;
                return false;
            }
        }

        StringBuilder classBuilder = new(256);
        int classNameLength = MonitorNativeMethods.GetClassName(
            control.Handle,
            classBuilder,
            classBuilder.Capacity);
        bool styleAvailable = MonitorNativeMethods.TryGetWindowLongPtr(
            control.Handle,
            MonitorNativeMethods.GWL_STYLE,
            out IntPtr stylePointer);
        control.ClassName = classBuilder.ToString();
        control.IsPassword = ControlEnumerator.ResolvePasswordState(
            control.ClassName,
            classNameLength,
            styleAvailable,
            stylePointer.ToInt64());
        if (control.IsPassword != false) {
            control.Text = string.Empty;
            control.Value = string.Empty;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Clicks the specified control.
    /// </summary>
    /// <param name="control">Control to click.</param>
    /// <param name="button">Mouse button to use.</param>
    public static void ControlClick(WindowControlInfo control, MouseButton button) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        // Try to use BM_CLICK first
        SendMessageWithTimeout(control.Handle, MonitorNativeMethods.BM_CLICK, 0, 0);

        RECT rect;
        if (MonitorNativeMethods.GetClientRect(control.Handle, out rect)) {
            int x = Math.Max(1, (rect.Right - rect.Left) / 2);
            int y = Math.Max(1, (rect.Bottom - rect.Top) / 2);
            SendMouseClick(control.Handle, button, x, y);
        } else {
            SendMouseClick(control.Handle, button, 0, 0);
        }
    }

    /// <summary>
    /// Retrieves the check state of a button control.
    /// </summary>
    /// <param name="control">Control to query.</param>
    /// <returns><c>true</c> if checked, <c>false</c> if unchecked, or null when indeterminate.</returns>
    public static bool? GetCheckState(WindowControlInfo control) {
        if (!TryGetCheckState(control, (int)MessageTimeoutMilliseconds, out bool? isChecked)) {
            throw new TimeoutException($"The check state did not respond within {MessageTimeoutMilliseconds}ms.");
        }

        return isChecked;
    }

    internal static bool TryGetCheckState(WindowControlInfo control, int timeoutMilliseconds, out bool? isChecked) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }
        if (timeoutMilliseconds <= 0) {
            isChecked = null;
            return false;
        }

        uint boundedTimeout = (uint)Math.Min(timeoutMilliseconds, (int)MessageTimeoutMilliseconds);
        IntPtr sendResult = MonitorNativeMethods.SendMessageTimeout(
            control.Handle,
            MonitorNativeMethods.BM_GETCHECK,
            IntPtr.Zero,
            IntPtr.Zero,
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            boundedTimeout,
            out IntPtr state);
        if (sendResult == IntPtr.Zero) {
            isChecked = null;
            return false;
        }

        long nativeState = state.ToInt64();
        isChecked = nativeState == 0
            ? false
            : nativeState == 1
                ? true
                : (bool?)null;
        return nativeState >= 0 && nativeState <= 2;
    }

    /// <summary>
    /// Returns whether the control exposes a native Win32 check state.
    /// </summary>
    /// <param name="control">Control to inspect.</param>
    /// <returns><c>true</c> when the control supports native check-state messages; otherwise <c>false</c>.</returns>
    public static bool SupportsCheckState(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Handle == IntPtr.Zero) {
            return false;
        }

        bool isButtonClass = string.Equals(control.ClassName, "Button", StringComparison.OrdinalIgnoreCase) ||
            control.ClassName.IndexOf("button", StringComparison.OrdinalIgnoreCase) >= 0;
        if (!isButtonClass) {
            return string.Equals(control.ControlType, "CheckBox", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(control.ControlType, "RadioButton", StringComparison.OrdinalIgnoreCase);
        }

        long style = MonitorNativeMethods.GetWindowLongPtr(control.Handle, MonitorNativeMethods.GWL_STYLE).ToInt64() & ButtonStyleMask;
        return style == ButtonStyleCheckBox ||
            style == ButtonStyleAutoCheckBox ||
            style == ButtonStyleRadioButton ||
            style == ButtonStyleThreeState ||
            style == ButtonStyleAutoThreeState ||
            style == ButtonStyleAutoRadioButton;
    }

    /// <summary>
    /// Sets the check state of a button control.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="check">Desired check state.</param>
    public static void SetCheckState(WindowControlInfo control, bool check) {   
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        bool? originalState = TryGetCheckState(control, (int)MessageTimeoutMilliseconds, out bool? currentState)
            ? currentState
            : throw new TimeoutException($"The check state did not respond within {MessageTimeoutMilliseconds}ms.");
        if (originalState == check) {
            return;
        }

        SendMessageWithTimeout(control.Handle, MonitorNativeMethods.BM_SETCHECK, check ? 1u : 0u, 0u);
        if (GetCheckStateForHandle(control.Handle) == check) {
            return;
        }

        // Some controls ignore BM_SETCHECK unless they are toggled through their standard click path.
        SendMessageWithTimeout(control.Handle, MonitorNativeMethods.BM_CLICK, 0u, 0u);
    }

    /// <summary>
    /// Sets control text without relying on foreground focus.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="text">Text to apply.</param>
    public static void SetText(WindowControlInfo control, string text) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }
        if (text.Length > DesktopTextObservationOptions.MaximumTextLength) {
            throw new ArgumentOutOfRangeException(
                nameof(text),
                $"Native text edits are limited to {DesktopTextObservationOptions.MaximumTextLength} characters.");
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        EnsureNativeTextMutationAllowed(control.Handle);

        if (!TrySendStringMessageWithTimeout(control.Handle, MonitorNativeMethods.WM_SETTEXT, IntPtr.Zero, text)) {
            throw new NativeTextMutationOutcomeUnknownException("WM_SETTEXT", MessageTimeoutMilliseconds);
        }

        if (ControlTextMatches(control.Handle, text)) {
            return;
        }

        EnsureNativeTextMutationAllowed(control.Handle);
        ReplaceAllText(control.Handle, text);
        if (!ControlTextMatches(control.Handle, text)) {
            throw new NativeTextMutationOutcomeUnknownException(
                "The native control accepted text mutation messages but the verified value differs from the requested value; the mutation outcome is unknown.");
        }
    }

    internal static bool TrySetTextIfUnchanged(
        WindowControlInfo control,
        string text,
        string expectedContentFingerprint,
        int maxTextLength,
        out string failureCode,
        out string observedContentFingerprint) {
        failureCode = string.Empty;
        observedContentFingerprint = string.Empty;
        if (control == null || control.Handle == IntPtr.Zero) {
            failureCode = "control-unavailable";
            return false;
        }

        EnsureNativeTextMutationAllowed(control.Handle);
        if (string.IsNullOrWhiteSpace(expectedContentFingerprint)) {
            SetText(control, text);
            return true;
        }

        var liveControl = new WindowControlInfo {
            Handle = control.Handle,
            IsPassword = false
        };
        if (!TryGetControlText(
                liveControl,
                maxTextLength,
                (int)MessageTimeoutMilliseconds,
                out string current,
                out bool isTruncated)) {
            failureCode = "native-read-timeout";
            return false;
        }
        if (isTruncated) {
            failureCode = "incomplete-precondition";
            return false;
        }

        observedContentFingerprint = DesktopTextObservationBuilder.CreateFingerprint(current);
        if (!string.Equals(observedContentFingerprint, expectedContentFingerprint, StringComparison.OrdinalIgnoreCase)) {
            failureCode = "content-changed";
            return false;
        }

        SetText(control, text);
        return true;
    }

    /// <summary>
    /// Sends key messages directly to a control without stealing focus.
    /// </summary>
    /// <param name="control">Target control.</param>
    /// <param name="keys">Keys to send.</param>
    public static void SendKeys(WindowControlInfo control, params VirtualKey[] keys) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        if (keys == null || keys.Length == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        var heldModifiers = new List<VirtualKey>();
        var printableBuffer = new StringBuilder();
        for (int index = 0; index < keys.Length; index++) {
            VirtualKey key = keys[index];
            bool hasTrailingKey = index < keys.Length - 1;
            if (IsModifierKey(key) && hasTrailingKey) {
                FlushPrintableBuffer(control.Handle, printableBuffer);
                SendMessageWithTimeout(control.Handle, MonitorNativeMethods.WM_KEYDOWN, (uint)key, 0);
                heldModifiers.Add(key);
                continue;
            }

            if (TryGetPrintableCharacter(key, heldModifiers.Count == 0, out char character)) {
                printableBuffer.Append(character);
            } else {
                FlushPrintableBuffer(control.Handle, printableBuffer);
                SendMessageWithTimeout(control.Handle, MonitorNativeMethods.WM_KEYDOWN, (uint)key, 0);
                SendMessageWithTimeout(control.Handle, MonitorNativeMethods.WM_KEYUP, (uint)key, 0);
            }

            ReleaseHeldModifiers(control.Handle, heldModifiers);
        }

        FlushPrintableBuffer(control.Handle, printableBuffer);
        ReleaseHeldModifiers(control.Handle, heldModifiers);
    }

    /// <summary>
    /// Enables or disables the specified control.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="enabled">True to enable the control; false to disable it.</param>
    public static void SetEnabled(WindowControlInfo control, bool enabled) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        MonitorNativeMethods.EnableWindow(control.Handle, enabled);
    }

    /// <summary>
    /// Shows or hides the specified control.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="visible">True to show the control; false to hide it.</param>
    public static void SetVisibility(WindowControlInfo control, bool visible) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        MonitorNativeMethods.ShowWindow(control.Handle, visible ? MonitorNativeMethods.SW_SHOW : MonitorNativeMethods.SW_HIDE);
    }

    private static void SendMouseClick(IntPtr handle, MouseButton button, int x, int y) {
        uint messageDown = button == MouseButton.Left ? MonitorNativeMethods.WM_LBUTTONDOWN : MonitorNativeMethods.WM_RBUTTONDOWN;
        uint messageUp = button == MouseButton.Left ? MonitorNativeMethods.WM_LBUTTONUP : MonitorNativeMethods.WM_RBUTTONUP;
        uint wParamDown = button == MouseButton.Left ? MonitorNativeMethods.MK_LBUTTON : MonitorNativeMethods.MK_RBUTTON;
        uint lParam = CreateLParam(x, y);

        SendMessageWithTimeout(handle, messageDown, wParamDown, lParam);
        SendMessageWithTimeout(handle, messageUp, 0, lParam);
    }

    private static uint CreateLParam(int x, int y) {
        return unchecked((uint)((y << 16) | (x & 0xFFFF)));
    }

    private static bool IsPrintableKey(VirtualKey key) {
        return (key >= VirtualKey.VK_SPACE && key <= VirtualKey.VK_Z) ||
            (key >= VirtualKey.VK_0 && key <= VirtualKey.VK_9);
    }

    internal static bool TryGetPrintableCharacter(VirtualKey key, bool noModifiersHeld, out char character) {
        character = '\0';
        if (!noModifiersHeld || !IsPrintableKey(key)) {
            return false;
        }

        if (key >= VirtualKey.VK_A && key <= VirtualKey.VK_Z) {
            character = (char)('A' + (key - VirtualKey.VK_A));
            return true;
        }

        if (key >= VirtualKey.VK_0 && key <= VirtualKey.VK_9) {
            character = (char)('0' + (key - VirtualKey.VK_0));
            return true;
        }

        if (key == VirtualKey.VK_SPACE) {
            character = ' ';
            return true;
        }

        return false;
    }

    private static bool IsModifierKey(VirtualKey key) {
        return key == VirtualKey.VK_CONTROL ||
            key == VirtualKey.VK_LCONTROL ||
            key == VirtualKey.VK_RCONTROL ||
            key == VirtualKey.VK_SHIFT ||
            key == VirtualKey.VK_LSHIFT ||
            key == VirtualKey.VK_RSHIFT ||
            key == VirtualKey.VK_MENU ||
            key == VirtualKey.VK_LMENU ||
            key == VirtualKey.VK_RMENU ||
            key == VirtualKey.VK_LWIN ||
            key == VirtualKey.VK_RWIN;
    }

    private static void ReleaseHeldModifiers(IntPtr handle, List<VirtualKey> heldModifiers) {
        for (int index = heldModifiers.Count - 1; index >= 0; index--) {
            SendMessageWithTimeout(handle, MonitorNativeMethods.WM_KEYUP, (uint)heldModifiers[index], 0);
        }

        heldModifiers.Clear();
    }

    private static void FlushPrintableBuffer(IntPtr handle, StringBuilder printableBuffer) {
        if (printableBuffer.Length == 0) {
            return;
        }

        ReplaceSelectedText(handle, printableBuffer.ToString(), appendToEnd: true);
        printableBuffer.Clear();
    }

    private static void ReplaceAllText(IntPtr handle, string text) {
        ReplaceSelectedText(handle, text, appendToEnd: false);
    }

    private static void ReplaceSelectedText(IntPtr handle, string text, bool appendToEnd) {
        EnsureNativeTextMutationAllowed(handle);
        uint start = appendToEnd ? unchecked((uint)0xFFFFFFFF) : 0u;
        uint end = unchecked((uint)0xFFFFFFFF);
        if (!TrySendMessageWithTimeout(handle, MonitorNativeMethods.EM_SETSEL, start, end)) {
            throw new NativeTextMutationOutcomeUnknownException("EM_SETSEL", MessageTimeoutMilliseconds);
        }
        EnsureNativeTextMutationAllowed(handle);
        if (!TrySendStringMessageWithTimeout(handle, MonitorNativeMethods.EM_REPLACESEL, new IntPtr(1), text)) {
            throw new NativeTextMutationOutcomeUnknownException("EM_REPLACESEL", MessageTimeoutMilliseconds);
        }
    }

    private static bool ControlTextMatches(IntPtr handle, string expectedText) {
        var control = new WindowControlInfo {
            Handle = handle,
            IsPassword = false
        };
        if (!TryGetControlText(
                control,
                Math.Max(1, expectedText.Length),
                (int)MessageTimeoutMilliseconds,
                out string currentText,
                out bool isTruncated)) {
            throw new NativeTextMutationOutcomeUnknownException("WM_GETTEXT", MessageTimeoutMilliseconds);
        }

        return !isTruncated && string.Equals(currentText, expectedText, StringComparison.Ordinal);
    }

    private static void EnsureNativeTextMutationAllowed(IntPtr handle) {
        StringBuilder classBuilder = new StringBuilder(256);
        int classNameLength = MonitorNativeMethods.GetClassName(handle, classBuilder, classBuilder.Capacity);
        if (classNameLength <= 0) {
            throw new InvalidOperationException("The live native control class could not be verified before editing.");
        }

        if (!MonitorNativeMethods.TryGetWindowLongPtr(
                handle,
                MonitorNativeMethods.GWL_STYLE,
                out IntPtr stylePointer)) {
            throw new InvalidOperationException("The live native control style could not be verified before editing.");
        }

        long style = stylePointer.ToInt64();
        if (ControlEnumerator.IsPasswordStyle(classBuilder.ToString(), style)) {
            throw new InvalidOperationException("Password controls cannot be updated through direct text messages.");
        }
    }

    private static void SendMessageWithTimeout(IntPtr handle, uint message, uint wParam, uint lParam) {
        TrySendMessageWithTimeout(handle, message, wParam, lParam);
    }

    private static bool GetCheckStateForHandle(IntPtr handle) {
        var control = new WindowControlInfo { Handle = handle };
        return TryGetCheckState(control, (int)MessageTimeoutMilliseconds, out bool? isChecked) && isChecked == true;
    }

    private static bool TrySendMessageWithTimeout(IntPtr handle, uint message, uint wParam, uint lParam) {
        IntPtr result = MonitorNativeMethods.SendMessageTimeout(
            handle,
            message,
            new IntPtr(unchecked((int)wParam)),
            new IntPtr(unchecked((int)lParam)),
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            MessageTimeoutMilliseconds,
            out _);
        return result != IntPtr.Zero;
    }

    private static bool TrySendStringMessageWithTimeout(IntPtr handle, uint message, IntPtr wParam, string text) {
        IntPtr result = MonitorNativeMethods.SendMessageTimeout(
            handle,
            message,
            wParam,
            text,
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            MessageTimeoutMilliseconds,
            out _);
        return result != IntPtr.Zero;
    }
}
