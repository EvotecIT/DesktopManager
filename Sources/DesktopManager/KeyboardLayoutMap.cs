using System;
using System.Globalization;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>How Caps Lock changes the character a layout key produces.</summary>
internal enum CapsLockEffect {
    /// <summary>Caps Lock does not change the character.</summary>
    None,
    /// <summary>Caps Lock inverts Shift for this key, so pressing Shift the other way produces the same character.</summary>
    InvertsShift,
    /// <summary>The character cannot be produced on this key while Caps Lock is on.</summary>
    Unavailable
}

/// <summary>One physical key and modifier combination that produces a character on a layout.</summary>
internal readonly struct LayoutKey {
    internal LayoutKey(ushort scanCode, bool extended, bool shift, bool altGr, bool dead, CapsLockEffect capsLock) {
        ScanCode = scanCode;
        Extended = extended;
        Shift = shift;
        AltGr = altGr;
        Dead = dead;
        CapsLock = capsLock;
    }

    /// <summary>Gets the set 1 scan code without the E0 prefix.</summary>
    internal ushort ScanCode { get; }

    /// <summary>Gets whether the key carries the E0 (extended) prefix.</summary>
    internal bool Extended { get; }

    /// <summary>Gets whether Shift is held.</summary>
    internal bool Shift { get; }

    /// <summary>Gets whether AltGr (Ctrl+Alt) is held.</summary>
    internal bool AltGr { get; }

    /// <summary>Gets whether the key is a dead key that waits for the next key.</summary>
    internal bool Dead { get; }

    /// <summary>Gets how Caps Lock changes the result.</summary>
    internal CapsLockEffect CapsLock { get; }
}

/// <summary>Maps characters to the keys that produce them on one keyboard layout.</summary>
internal interface IKeyboardLayoutMap {
    /// <summary>Gets a short description of the layout for messages and results.</summary>
    string Name { get; }

    /// <summary>Finds the key that produces <paramref name="character"/>.</summary>
    bool TryMap(char character, out LayoutKey key);
}

/// <summary>
/// Reads character mappings from a Windows keyboard layout (<c>HKL</c>) with <c>VkKeyScanEx</c>,
/// <c>MapVirtualKeyEx</c> and a state-preserving <c>ToUnicodeEx</c> probe for dead keys and Caps Lock.
/// </summary>
/// <remarks>A layout this type loaded is unloaded on dispose; a layout that was already present is left alone.</remarks>
[SupportedOSPlatform("windows")]
internal sealed class NativeKeyboardLayoutMap : IKeyboardLayoutMap, IDisposable {
    private const byte KeyDown = 0x80;
    private const byte Toggled = 0x01;
    private const int UnknownLocale = 0x1000;
    private readonly IntPtr _layout;
    private readonly bool _unloadOnDispose;
    private readonly byte[] _keyState = new byte[256];
    private readonly char[] _buffer = new char[8];
    private bool _disposed;

    private NativeKeyboardLayoutMap(IntPtr layout, string name, bool unloadOnDispose) {
        _layout = layout;
        Name = name;
        _unloadOnDispose = unloadOnDispose;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>Uses the layout currently active on a thread, normally the target window's thread.</summary>
    internal static NativeKeyboardLayoutMap ForThread(uint threadId) {
        IntPtr layout = MonitorNativeMethods.GetKeyboardLayout(threadId);
        if (layout == IntPtr.Zero) {
            throw new InvalidOperationException("The target window's keyboard layout could not be read.");
        }

        return ForLoadedLayout(layout);
    }

    /// <summary>Uses a layout that is already loaded, such as one returned by <c>GetKeyboardLayoutList</c>; it is never unloaded.</summary>
    internal static NativeKeyboardLayoutMap ForLoadedLayout(IntPtr layout) {
        return new NativeKeyboardLayoutMap(layout, FormatLayoutHandle(layout), unloadOnDispose: false);
    }

    /// <summary>
    /// Loads a layout by keyboard layout identifier (for example <c>00000407</c>) or culture name (for example <c>de-DE</c>)
    /// without activating it.
    /// </summary>
    internal static NativeKeyboardLayoutMap Load(string layout) {
        string klid = ResolveLayoutId(layout);
        IntPtr[] present = SessionTypingNative.GetKeyboardLayouts();
        IntPtr handle = SessionTypingNative.LoadKeyboardLayout(klid, SessionTypingNative.KLF_NOTELLSHELL);
        if (handle == IntPtr.Zero) {
            throw new ArgumentException($"Keyboard layout '{layout}' could not be loaded.", nameof(layout));
        }

        bool alreadyPresent = Array.IndexOf(present, handle) >= 0;
        return new NativeKeyboardLayoutMap(handle, klid, unloadOnDispose: !alreadyPresent);
    }

    /// <summary>Normalises a KLID or culture name to the eight-digit identifier <c>LoadKeyboardLayout</c> expects.</summary>
    internal static string ResolveLayoutId(string layout) {
        if (string.IsNullOrWhiteSpace(layout)) {
            throw new ArgumentException("A keyboard layout identifier or culture name is required.", nameof(layout));
        }

        string trimmed = layout.Trim();
        bool hexPrefix = trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        string digits = hexPrefix ? trimmed.Substring(2) : trimmed;
        // Culture names such as "de" are valid hex, so only eight digits or an explicit 0x prefix mean a KLID.
        if ((hexPrefix || digits.Length == 8) && digits.Length <= 8 && IsHex(digits)) {
            uint id = uint.Parse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return id.ToString("X8", CultureInfo.InvariantCulture);
        }

        try {
            // Unknown names can still produce a culture (LCID 0x1000) that falls back to the US layout; neutral cultures
            // such as "de" have no layout of their own.
            CultureInfo culture = CultureInfo.GetCultureInfo(trimmed);
            if (!culture.IsNeutralCulture && culture.LCID != UnknownLocale && culture.KeyboardLayoutId > 0) {
                return culture.KeyboardLayoutId.ToString("X8", CultureInfo.InvariantCulture);
            }
        } catch (CultureNotFoundException) {
            // Reported below with the original value.
        }

        throw new ArgumentException($"'{layout}' is neither a keyboard layout identifier such as 00000407 nor a culture name with a keyboard layout.", nameof(layout));
    }

    /// <inheritdoc />
    public bool TryMap(char character, out LayoutKey key) {
        key = default;
        if (_disposed) {
            throw new ObjectDisposedException(nameof(NativeKeyboardLayoutMap));
        }

        short packed = MonitorNativeMethods.VkKeyScanEx(character, _layout);
        if (packed == -1) {
            return false;
        }

        uint virtualKey = (uint)(packed & 0xFF);
        int modifiers = (packed >> 8) & 0xFF;
        bool shift = (modifiers & 1) != 0;
        bool control = (modifiers & 2) != 0;
        bool alt = (modifiers & 4) != 0;
        // Control-only or Alt-only combinations produce control characters or shortcuts, never text.
        if ((modifiers & ~7) != 0 || control != alt || virtualKey == 0 || IsNumericKeypad(virtualKey)) {
            return false;
        }

        uint scan = SessionTypingNative.MapVirtualKeyEx(virtualKey, SessionTypingNative.MAPVK_VK_TO_VSC_EX, _layout);
        uint prefix = scan >> 8;
        if ((scan & 0xFF) == 0 || (prefix != 0 && prefix != 0xE0)) {
            return false;
        }

        bool altGr = control;
        int produced = Probe(virtualKey, scan, shift, altGr, capsLock: false, out char output);
        if (produced == 0 || (produced > 0 && (produced != 1 || output != character))) {
            return false;
        }

        bool dead = produced < 0;
        CapsLockEffect capsLock = ProbeCapsLock(virtualKey, scan, shift, altGr, produced, output);
        key = new LayoutKey((ushort)(scan & 0xFF), prefix == 0xE0, shift, altGr, dead, capsLock);
        return true;
    }

    private CapsLockEffect ProbeCapsLock(uint virtualKey, uint scan, bool shift, bool altGr, int produced, char output) {
        int withCaps = Probe(virtualKey, scan, shift, altGr, capsLock: true, out char capsOutput);
        if (withCaps == produced && capsOutput == output) {
            return CapsLockEffect.None;
        }

        int inverted = Probe(virtualKey, scan, !shift, altGr, capsLock: true, out char invertedOutput);
        return inverted == produced && invertedOutput == output
            ? CapsLockEffect.InvertsShift
            : CapsLockEffect.Unavailable;
    }

    private int Probe(uint virtualKey, uint scan, bool shift, bool altGr, bool capsLock, out char output) {
        Array.Clear(_keyState, 0, _keyState.Length);
        Array.Clear(_buffer, 0, _buffer.Length);
        if (shift) {
            _keyState[0x10] = KeyDown;
            _keyState[0xA0] = KeyDown;
        }
        if (altGr) {
            _keyState[0x11] = KeyDown;
            _keyState[0xA2] = KeyDown;
            _keyState[0x12] = KeyDown;
            _keyState[0xA5] = KeyDown;
        }
        if (capsLock) {
            _keyState[SessionTypingNative.VK_CAPITAL] = Toggled;
        }

        // Flag 0x4 keeps the thread's keyboard state, including any pending dead key, unchanged.
        int result = SessionTypingNative.ToUnicodeEx(virtualKey, scan & 0xFF, _keyState, _buffer, _buffer.Length,
            SessionTypingNative.ToUnicodeDoNotChangeKeyboardState, _layout);
        output = _buffer[0];
        Array.Clear(_buffer, 0, _buffer.Length);
        return result;
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        if (_unloadOnDispose) {
            SessionTypingNative.UnloadKeyboardLayout(_layout);
        }
    }

    private static bool IsNumericKeypad(uint virtualKey) {
        return virtualKey >= 0x60 && virtualKey <= 0x6F;
    }

    private static bool IsHex(string value) {
        foreach (char c in value) {
            if (!Uri.IsHexDigit(c)) {
                return false;
            }
        }

        return value.Length > 0;
    }

    private static string FormatLayoutHandle(IntPtr layout) {
        return "0x" + layout.ToInt64().ToString("X8", CultureInfo.InvariantCulture);
    }
}
