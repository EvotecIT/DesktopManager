using System;
using System.Runtime.Versioning;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Provides helper methods for interacting with window controls.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowControlService {
    private const uint MessageTimeoutMilliseconds = 1000;
    private const uint WmCommand = 0x0111;
    private const long ButtonStyleMask = 0x0000000F;
    private const long ButtonStyleCheckBox = 0x00000002;
    private const long ButtonStyleAutoCheckBox = 0x00000003;
    private const long ButtonStyleRadioButton = 0x00000004;
    private const long ButtonStyleThreeState = 0x00000005;
    private const long ButtonStyleAutoThreeState = 0x00000006;
    private const long ButtonStyleAutoRadioButton = 0x00000009;
    private const int ComboBoxError = -1;
    private const uint CbGetCount = 0x0146;
    private const uint CbGetCurSel = 0x0147;
    private const uint CbGetLbText = 0x0148;
    private const uint CbGetLbTextLen = 0x0149;
    private const uint CbSetCurSel = 0x014E;
    private const int CbnSelChange = 1;

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
    /// <returns><c>true</c> if checked; otherwise <c>false</c>.</returns>
    public static bool GetCheckState(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }
        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        int state = (int)MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.BM_GETCHECK, 0u, 0u);
        return state != 0;
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
    /// Returns whether the control exposes a native single-selection list such as a combo box.
    /// </summary>
    /// <param name="control">Control to inspect.</param>
    /// <returns><c>true</c> when the control supports native combo-box selection messages; otherwise <c>false</c>.</returns>
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

    /// <summary>
    /// Retrieves the selected index for a combo-box-style control.
    /// </summary>
    /// <param name="control">Control to query.</param>
    /// <returns>The selected index, or <c>-1</c> when no selection exists.</returns>
    public static int GetSelectedIndex(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        int selectedIndex = unchecked((int)MonitorNativeMethods.SendMessage(control.Handle, CbGetCurSel, 0u, 0u));
        return selectedIndex == ComboBoxError ? -1 : selectedIndex;
    }

    /// <summary>
    /// Retrieves the selected value for a combo-box-style control.
    /// </summary>
    /// <param name="control">Control to query.</param>
    /// <returns>The selected item text when available; otherwise the live control text.</returns>
    public static string GetSelectedValue(WindowControlInfo control) {
        return GetSelectedValueCore(control, maxTextLength: null);
    }

    internal static string GetSelectedValue(WindowControlInfo control, int maxTextLength) {
        return GetSelectedValue(control, maxTextLength, out _);
    }

    internal static string GetSelectedValue(WindowControlInfo control, int maxTextLength, out bool isTruncated) {
        if (maxTextLength < 1) {
            throw new ArgumentOutOfRangeException(nameof(maxTextLength), "maxTextLength must be greater than zero.");
        }

        return GetSelectedValueCore(control, maxTextLength, out isTruncated);
    }

    private static string GetSelectedValueCore(WindowControlInfo control, int? maxTextLength) {
        return GetSelectedValueCore(control, maxTextLength, out _);
    }

    private static string GetSelectedValueCore(WindowControlInfo control, int? maxTextLength, out bool isTruncated) {
        isTruncated = false;
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.IsPassword != false) {
            return string.Empty;
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        int selectedIndex = GetSelectedIndex(control);
        if (selectedIndex < 0) {
            if (maxTextLength.HasValue) {
                return WindowTextHelper.GetWindowText(control.Handle, maxTextLength.Value, out isTruncated);
            }

            return WindowTextHelper.GetWindowText(control.Handle);
        }

        return GetComboBoxItemText(control.Handle, selectedIndex, maxTextLength, out isTruncated);
    }

    /// <summary>
    /// Selects a combo-box-style item by its displayed text.
    /// </summary>
    /// <param name="control">Control to update.</param>
    /// <param name="value">Displayed item text to select.</param>
    public static void SetSelectedValue(WindowControlInfo control, string value) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        int itemCount = unchecked((int)MonitorNativeMethods.SendMessage(control.Handle, CbGetCount, 0u, 0u));
        if (itemCount == ComboBoxError) {
            throw new InvalidOperationException("The combo box item count could not be resolved.");
        }

        for (int index = 0; index < itemCount; index++) {
            string itemText = GetComboBoxItemText(control.Handle, index, maxTextLength: null, out _);
            if (!string.Equals(itemText, value, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            SetSelectedIndex(control, index);
            return;
        }

        throw new InvalidOperationException($"The combo box does not contain an item named '{value}'.");
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

        bool originalState = GetCheckState(control);
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
    /// Selects a combo-box-style item by its zero-based index.
    /// </summary>
    /// <param name="control">Control to update.</param>
    /// <param name="index">Zero-based item index.</param>
    public static void SetSelectedIndex(WindowControlInfo control, int index) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        if (index < 0) {
            throw new ArgumentOutOfRangeException(nameof(index), "The selected index must be zero or greater.");
        }

        int result = unchecked((int)MonitorNativeMethods.SendMessage(control.Handle, CbSetCurSel, unchecked((uint)index), 0u));
        if (result == ComboBoxError) {
            throw new InvalidOperationException($"The combo box does not expose an item at index {index}.");
        }

        NotifyParentSelectionChanged(control);
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

        if (control.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle", nameof(control));
        }

        EnsureNativeTextMutationAllowed(control.Handle);

        if (!TrySendStringMessageWithTimeout(control.Handle, MonitorNativeMethods.WM_SETTEXT, IntPtr.Zero, text)) {
            EnsureNativeTextMutationAllowed(control.Handle);
            MonitorNativeMethods.SendMessage(control.Handle, MonitorNativeMethods.WM_SETTEXT, IntPtr.Zero, text);
        }

        if (ControlTextMatches(control.Handle, text)) {
            return;
        }

        EnsureNativeTextMutationAllowed(control.Handle);
        ReplaceAllText(control.Handle, text);
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
        SendMessageWithTimeout(handle, MonitorNativeMethods.EM_SETSEL, start, end);
        EnsureNativeTextMutationAllowed(handle);
        if (!TrySendStringMessageWithTimeout(handle, MonitorNativeMethods.EM_REPLACESEL, new IntPtr(1), text)) {
            EnsureNativeTextMutationAllowed(handle);
            MonitorNativeMethods.SendMessage(handle, MonitorNativeMethods.EM_REPLACESEL, new IntPtr(1), text);
        }
    }

    private static bool ControlTextMatches(IntPtr handle, string expectedText) {
        return string.Equals(WindowTextHelper.GetWindowText(handle), expectedText, StringComparison.Ordinal);
    }

    private static void EnsureNativeTextMutationAllowed(IntPtr handle) {
        StringBuilder classBuilder = new StringBuilder(256);
        int classNameLength = MonitorNativeMethods.GetClassName(handle, classBuilder, classBuilder.Capacity);
        if (classNameLength <= 0) {
            throw new InvalidOperationException("The live native control class could not be verified before editing.");
        }

        long style = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_STYLE).ToInt64();
        if (ControlEnumerator.IsPasswordStyle(classBuilder.ToString(), style)) {
            throw new InvalidOperationException("Password controls cannot be updated through direct text messages.");
        }
    }

    private static string GetComboBoxItemText(IntPtr handle, int index, int? maxTextLength, out bool isTruncated) {
        isTruncated = false;
        int itemTextLength = unchecked((int)MonitorNativeMethods.SendMessage(handle, CbGetLbTextLen, unchecked((uint)index), 0u));
        if (itemTextLength == ComboBoxError) {
            return string.Empty;
        }

        if (maxTextLength.HasValue && itemTextLength > maxTextLength.Value) {
            isTruncated = true;
            return string.Empty;
        }

        var buffer = new StringBuilder(itemTextLength + 1);
        MonitorNativeMethods.SendMessage(handle, CbGetLbText, new IntPtr(index), buffer);
        string value = buffer.ToString();
        if (maxTextLength.HasValue && value.Length > maxTextLength.Value) {
            isTruncated = true;
            return value.Substring(0, maxTextLength.Value);
        }

        return value;
    }

    private static void NotifyParentSelectionChanged(WindowControlInfo control) {
        IntPtr parentHandle = control.ParentWindowHandle != IntPtr.Zero
            ? control.ParentWindowHandle
            : MonitorNativeMethods.GetParent(control.Handle);
        if (parentHandle == IntPtr.Zero) {
            return;
        }

        int controlId = control.Id != 0 ? control.Id : MonitorNativeMethods.GetDlgCtrlID(control.Handle);
        int wParam = unchecked((CbnSelChange << 16) | (controlId & 0xFFFF));
        MonitorNativeMethods.SendMessageTimeout(
            parentHandle,
            WmCommand,
            new IntPtr(wParam),
            control.Handle,
            MonitorNativeMethods.SMTO_ABORTIFHUNG,
            MessageTimeoutMilliseconds,
            out _);
    }

    private static void SendMessageWithTimeout(IntPtr handle, uint message, uint wParam, uint lParam) {
        TrySendMessageWithTimeout(handle, message, wParam, lParam);
    }

    private static bool GetCheckStateForHandle(IntPtr handle) {
        int state = (int)MonitorNativeMethods.SendMessage(handle, MonitorNativeMethods.BM_GETCHECK, 0u, 0u);
        return state != 0;
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
