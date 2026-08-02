using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>Indicates that only part of a requested input sequence was accepted and the mutation outcome is unknown.</summary>
internal sealed class KeyboardInputDeliveryException : InvalidOperationException {
    internal KeyboardInputDeliveryException(string operation, uint requested, uint delivered)
        : base($"{operation} accepted {delivered} of {requested} input events; the mutation outcome is unknown.") {
    }
}

/// <summary>
/// Provides methods for simulating keyboard input.
/// </summary>
[SupportedOSPlatform("windows")]
public static class KeyboardInputService {
    private const ushort LeftShiftScanCode = 0x2A;

    internal readonly struct KeyboardLayoutStroke {
        public KeyboardLayoutStroke(VirtualKey key, IReadOnlyList<VirtualKey> modifiers) {
            Key = key;
            Modifiers = modifiers;
        }

        public VirtualKey Key { get; }
        public IReadOnlyList<VirtualKey> Modifiers { get; }
    }

    internal readonly struct ScanCodeStroke {
        public ScanCodeStroke(ushort scanCode, bool shiftRequired) {
            ScanCode = scanCode;
            ShiftRequired = shiftRequired;
        }

        public ushort ScanCode { get; }
        public bool ShiftRequired { get; }
    }

    /// <summary>
    /// Presses a single key by sending a down and up event.
    /// </summary>
    /// <param name="key">Key to press.</param>
    public static void PressKey(VirtualKey key) {
        KeyDown(key);
        KeyUp(key);
    }

    /// <summary>
    /// Sends a key down event for the specified key.
    /// </summary>
    /// <param name="key">Key to press down.</param>
    public static void KeyDown(VirtualKey key) {
        MonitorNativeMethods.INPUT input = new();
        input.Type = MonitorNativeMethods.INPUT_KEYBOARD;
        input.Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = (ushort)key,
            Scan = 0,
            Flags = 0,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        SendInputOrThrow([input], "KeyDown");
    }

    /// <summary>
    /// Sends a key up event for the specified key.
    /// </summary>
    /// <param name="key">Key to release.</param>
    public static void KeyUp(VirtualKey key) {
        MonitorNativeMethods.INPUT input = new();
        input.Type = MonitorNativeMethods.INPUT_KEYBOARD;
        input.Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = (ushort)key,
            Scan = 0,
            Flags = MonitorNativeMethods.KEYEVENTF_KEYUP,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        SendInputOrThrow([input], "KeyUp");
    }

    /// <summary>
    /// Presses a shortcut combination of keys.
    /// </summary>
    /// <param name="delay">Delay in milliseconds between each key event.</param>
    /// <param name="keys">Keys to press in order.</param>
    public static void PressShortcut(int delay, params VirtualKey[] keys) {
        if (keys == null || keys.Length == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        foreach (VirtualKey key in keys) {
            KeyDown(key);
            if (delay > 0) {
                Thread.Sleep(delay);
            }
        }

        for (int i = keys.Length - 1; i >= 0; i--) {
            KeyUp(keys[i]);
            if (delay > 0) {
                Thread.Sleep(delay);
            }
        }
    }

    /// <summary>
    /// Sends one or more keys to the current foreground target using SendInput.
    /// </summary>
    /// <param name="keys">Keys to send.</param>
    public static void SendToForeground(params VirtualKey[] keys) {
        if (keys == null || keys.Length == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        var heldModifiers = new List<VirtualKey>();
        for (int index = 0; index < keys.Length; index++) {
            VirtualKey key = keys[index];
            bool hasTrailingKey = index < keys.Length - 1;
            if (IsModifierKey(key) && hasTrailingKey) {
                KeyDown(key);
                heldModifiers.Add(key);
                continue;
            }

            PressKey(key);
            ReleaseHeldModifiers(heldModifiers);
        }

        ReleaseHeldModifiers(heldModifiers);
    }

    /// <summary>
    /// Sends Unicode text to the current foreground target using SendInput.
    /// </summary>
    /// <param name="text">Text to send.</param>
    /// <param name="delayMilliseconds">Optional delay between characters.</param>
    public static void SendTextToForeground(string text, int delayMilliseconds = 0) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        if (delayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "delayMilliseconds must be zero or greater.");
        }

        foreach (char character in text) {
            SendCharacterToForeground(character, delayMilliseconds);
        }
    }

    /// <summary>
    /// Sends text to the current foreground target using layout-aware physical key presses when available.
    /// </summary>
    /// <param name="text">Text to send.</param>
    /// <param name="threadId">Target thread identifier used to resolve keyboard layout.</param>
    /// <param name="delayMilliseconds">Optional delay between characters.</param>
    public static void SendTextToForegroundUsingKeyboardLayout(string text, uint threadId, int delayMilliseconds = 0) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        if (delayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "delayMilliseconds must be zero or greater.");
        }

        IntPtr keyboardLayout = MonitorNativeMethods.GetKeyboardLayout(threadId);
        foreach (char character in text) {
            SendCharacterToForegroundUsingKeyboardLayout(character, keyboardLayout, delayMilliseconds);
        }
    }

    /// <summary>
    /// Sends text to the current foreground target using a fixed US-style scancode map.
    /// </summary>
    /// <param name="text">Text to send.</param>
    /// <param name="delayMilliseconds">Optional delay between characters.</param>
    public static void SendTextToForegroundUsingUsScanCodes(string text, int delayMilliseconds = 0) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        if (delayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "delayMilliseconds must be zero or greater.");
        }

        foreach (char character in text) {
            SendCharacterToForegroundUsingUsScanCodes(character, delayMilliseconds);
        }
    }

    /// <summary>
    /// Sends key presses directly to a background control.
    /// </summary>
    /// <param name="control">Target control.</param>
    /// <param name="keys">Keys to send.</param>
    public static void SendToControl(WindowControlInfo control, params VirtualKey[] keys) {
        WindowControlService.SendKeys(control, keys);
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

    internal static bool TryCreateKeyboardLayoutStroke(short layoutResult, out KeyboardLayoutStroke stroke) {
        stroke = default;
        if (layoutResult == -1) {
            return false;
        }

        byte virtualKeyCode = unchecked((byte)(layoutResult & 0xFF));
        if (virtualKeyCode == 0) {
            return false;
        }

        byte modifierState = unchecked((byte)((layoutResult >> 8) & 0xFF));
        stroke = new KeyboardLayoutStroke((VirtualKey)virtualKeyCode, GetModifierKeysForKeyboardState(modifierState));
        return true;
    }

    internal static IReadOnlyList<VirtualKey> GetModifierKeysForKeyboardState(byte modifierState) {
        List<VirtualKey> modifiers = new();
        bool usesAltGr = (modifierState & 0b110) == 0b110;
        if ((modifierState & 1) != 0) {
            modifiers.Add(VirtualKey.VK_SHIFT);
        }

        if (usesAltGr) {
            modifiers.Add(VirtualKey.VK_RMENU);
            return modifiers;
        }

        if ((modifierState & 2) != 0) {
            modifiers.Add(VirtualKey.VK_CONTROL);
        }

        if ((modifierState & 4) != 0) {
            modifiers.Add(VirtualKey.VK_MENU);
        }

        return modifiers;
    }

    internal static bool TryCreateUsKeyboardScanCodeStroke(char character, out ScanCodeStroke stroke) {
        stroke = default;

        if (TryCreateUsLetterStroke(character, out stroke)) {
            return true;
        }

        if (TryCreateUsDigitOrShiftedDigitStroke(character, out stroke)) {
            return true;
        }

        return character switch {
            ' ' => TrySetUsStroke(0x39, shiftRequired: false, out stroke),
            '\t' => TrySetUsStroke(0x0F, shiftRequired: false, out stroke),
            '`' => TrySetUsStroke(0x29, shiftRequired: false, out stroke),
            '~' => TrySetUsStroke(0x29, shiftRequired: true, out stroke),
            '-' => TrySetUsStroke(0x0C, shiftRequired: false, out stroke),
            '_' => TrySetUsStroke(0x0C, shiftRequired: true, out stroke),
            '=' => TrySetUsStroke(0x0D, shiftRequired: false, out stroke),
            '+' => TrySetUsStroke(0x0D, shiftRequired: true, out stroke),
            '[' => TrySetUsStroke(0x1A, shiftRequired: false, out stroke),
            '{' => TrySetUsStroke(0x1A, shiftRequired: true, out stroke),
            ']' => TrySetUsStroke(0x1B, shiftRequired: false, out stroke),
            '}' => TrySetUsStroke(0x1B, shiftRequired: true, out stroke),
            '\\' => TrySetUsStroke(0x2B, shiftRequired: false, out stroke),
            '|' => TrySetUsStroke(0x2B, shiftRequired: true, out stroke),
            ';' => TrySetUsStroke(0x27, shiftRequired: false, out stroke),
            ':' => TrySetUsStroke(0x27, shiftRequired: true, out stroke),
            '\'' => TrySetUsStroke(0x28, shiftRequired: false, out stroke),
            '"' => TrySetUsStroke(0x28, shiftRequired: true, out stroke),
            ',' => TrySetUsStroke(0x33, shiftRequired: false, out stroke),
            '<' => TrySetUsStroke(0x33, shiftRequired: true, out stroke),
            '.' => TrySetUsStroke(0x34, shiftRequired: false, out stroke),
            '>' => TrySetUsStroke(0x34, shiftRequired: true, out stroke),
            '/' => TrySetUsStroke(0x35, shiftRequired: false, out stroke),
            '?' => TrySetUsStroke(0x35, shiftRequired: true, out stroke),
            _ => false
        };
    }

    internal static void SendCharacterToForeground(char character, int delayMilliseconds = 0) {
        if (delayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "delayMilliseconds must be zero or greater.");
        }

        MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[2];

        inputs[0].Type = MonitorNativeMethods.INPUT_KEYBOARD;
        inputs[0].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = 0,
            Scan = character,
            Flags = MonitorNativeMethods.KEYEVENTF_UNICODE,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };

        inputs[1].Type = MonitorNativeMethods.INPUT_KEYBOARD;
        inputs[1].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = 0,
            Scan = character,
            Flags = MonitorNativeMethods.KEYEVENTF_UNICODE | MonitorNativeMethods.KEYEVENTF_KEYUP,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };

        SendInputOrThrow(inputs, "Unicode text input");
        if (delayMilliseconds > 0) {
            Thread.Sleep(delayMilliseconds);
        }
    }

    internal static void SendCharacterToForegroundUsingKeyboardLayout(char character, uint threadId, int delayMilliseconds = 0) {
        if (delayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "delayMilliseconds must be zero or greater.");
        }

        IntPtr keyboardLayout = MonitorNativeMethods.GetKeyboardLayout(threadId);
        SendCharacterToForegroundUsingKeyboardLayout(character, keyboardLayout, delayMilliseconds);
    }

    internal static void SendCharacterToForegroundUsingUsScanCodes(char character, int delayMilliseconds = 0) {
        if (delayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "delayMilliseconds must be zero or greater.");
        }

        if (TryCreateUsKeyboardScanCodeStroke(character, out ScanCodeStroke stroke)) {
            SendScanCodeStroke(stroke, delayMilliseconds);
            return;
        }

        SendCharacterToForeground(character, delayMilliseconds);
    }

    private static bool TryCreateUsLetterStroke(char character, out ScanCodeStroke stroke) {
        stroke = default;
        char upper = char.ToUpperInvariant(character);
        if (upper < 'A' || upper > 'Z') {
            return false;
        }

        VirtualKey key = (VirtualKey)Enum.Parse(typeof(VirtualKey), $"VK_{upper}", ignoreCase: false);
        ushort scanCode = GetScanCodeForVirtualKey(key);
        bool shiftRequired = char.IsUpper(character);
        return TrySetUsStroke(scanCode, shiftRequired, out stroke);
    }

    private static bool TryCreateUsDigitOrShiftedDigitStroke(char character, out ScanCodeStroke stroke) {
        stroke = default;
        return character switch {
            '1' => TrySetUsStroke(0x02, shiftRequired: false, out stroke),
            '!' => TrySetUsStroke(0x02, shiftRequired: true, out stroke),
            '2' => TrySetUsStroke(0x03, shiftRequired: false, out stroke),
            '@' => TrySetUsStroke(0x03, shiftRequired: true, out stroke),
            '3' => TrySetUsStroke(0x04, shiftRequired: false, out stroke),
            '#' => TrySetUsStroke(0x04, shiftRequired: true, out stroke),
            '4' => TrySetUsStroke(0x05, shiftRequired: false, out stroke),
            '$' => TrySetUsStroke(0x05, shiftRequired: true, out stroke),
            '5' => TrySetUsStroke(0x06, shiftRequired: false, out stroke),
            '%' => TrySetUsStroke(0x06, shiftRequired: true, out stroke),
            '6' => TrySetUsStroke(0x07, shiftRequired: false, out stroke),
            '^' => TrySetUsStroke(0x07, shiftRequired: true, out stroke),
            '7' => TrySetUsStroke(0x08, shiftRequired: false, out stroke),
            '&' => TrySetUsStroke(0x08, shiftRequired: true, out stroke),
            '8' => TrySetUsStroke(0x09, shiftRequired: false, out stroke),
            '*' => TrySetUsStroke(0x09, shiftRequired: true, out stroke),
            '9' => TrySetUsStroke(0x0A, shiftRequired: false, out stroke),
            '(' => TrySetUsStroke(0x0A, shiftRequired: true, out stroke),
            '0' => TrySetUsStroke(0x0B, shiftRequired: false, out stroke),
            ')' => TrySetUsStroke(0x0B, shiftRequired: true, out stroke),
            _ => false
        };
    }

    private static bool TrySetUsStroke(ushort scanCode, bool shiftRequired, out ScanCodeStroke stroke) {
        stroke = default;
        if (scanCode == 0) {
            return false;
        }

        stroke = new ScanCodeStroke(scanCode, shiftRequired);
        return true;
    }

    private static void SendKeyboardLayoutStroke(KeyboardLayoutStroke stroke, int delayMilliseconds) {
        foreach (VirtualKey modifier in stroke.Modifiers) {
            KeyDown(modifier);
        }

        PressKey(stroke.Key);

        for (int index = stroke.Modifiers.Count - 1; index >= 0; index--) {
            KeyUp(stroke.Modifiers[index]);
        }

        if (delayMilliseconds > 0) {
            Thread.Sleep(delayMilliseconds);
        }
    }

    private static void SendCharacterToForegroundUsingKeyboardLayout(char character, IntPtr keyboardLayout, int delayMilliseconds) {
        short layoutResult = MonitorNativeMethods.VkKeyScanEx(character, keyboardLayout);
        if (TryCreateKeyboardLayoutStroke(layoutResult, out KeyboardLayoutStroke stroke)) {
            SendKeyboardLayoutStroke(stroke, delayMilliseconds);
            return;
        }

        SendCharacterToForeground(character, delayMilliseconds);
    }

    private static ushort GetScanCodeForVirtualKey(VirtualKey key) {
        return unchecked((ushort)MonitorNativeMethods.MapVirtualKey((uint)key, 0));
    }

    private static void SendScanCodeStroke(ScanCodeStroke stroke, int delayMilliseconds) {
        if (stroke.ShiftRequired) {
            SendScanCodeKey(LeftShiftScanCode, keyUp: false);
        }

        SendScanCodeKey(stroke.ScanCode, keyUp: false);
        SendScanCodeKey(stroke.ScanCode, keyUp: true);

        if (stroke.ShiftRequired) {
            SendScanCodeKey(LeftShiftScanCode, keyUp: true);
        }

        if (delayMilliseconds > 0) {
            Thread.Sleep(delayMilliseconds);
        }
    }

    private static void SendScanCodeKey(ushort scanCode, bool keyUp) {
        MonitorNativeMethods.INPUT input = new();
        input.Type = MonitorNativeMethods.INPUT_KEYBOARD;
        input.Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = 0,
            Scan = scanCode,
            Flags = MonitorNativeMethods.KEYEVENTF_SCANCODE | (keyUp ? MonitorNativeMethods.KEYEVENTF_KEYUP : 0),
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };

        SendInputOrThrow([input], "Keyboard input");
    }

    private static void SendInputOrThrow(MonitorNativeMethods.INPUT[] inputs, string operation) {
        uint requested = (uint)inputs.Length;
        uint delivered = MonitorNativeMethods.SendInput(
            requested,
            inputs,
            Marshal.SizeOf<MonitorNativeMethods.INPUT>());
        EnsureInputDelivery(requested, delivered, operation);
    }

    internal static void EnsureInputDelivery(uint requested, uint delivered, string operation) {
        if (delivered != requested) {
            throw new KeyboardInputDeliveryException(operation, requested, delivered);
        }
    }

    private static void ReleaseHeldModifiers(List<VirtualKey> heldModifiers) {
        for (int index = heldModifiers.Count - 1; index >= 0; index--) {
            KeyUp(heldModifiers[index]);
        }

        heldModifiers.Clear();
    }
}
