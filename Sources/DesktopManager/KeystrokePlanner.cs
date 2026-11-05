using System;

namespace DesktopManager;

/// <summary>Flags of one planned key event.</summary>
[Flags]
internal enum KeystrokeFlags : byte {
    /// <summary>A scan-code key press.</summary>
    None = 0,
    /// <summary>A key release.</summary>
    Up = 1,
    /// <summary>The scan code carries the E0 (extended) prefix.</summary>
    Extended = 2,
    /// <summary>The code is a UTF-16 unit sent as a Unicode packet instead of a scan code.</summary>
    Unicode = 4
}

/// <summary>One key event of a plan.</summary>
internal readonly struct Keystroke {
    internal Keystroke(ushort code, KeystrokeFlags flags, int delayAfterMilliseconds, int sourceEnd) {
        Code = code;
        Flags = flags;
        DelayAfterMilliseconds = delayAfterMilliseconds;
        SourceEnd = sourceEnd;
    }

    /// <summary>Gets the scan code, or the UTF-16 unit for a Unicode packet.</summary>
    internal ushort Code { get; }

    /// <summary>Gets the event flags.</summary>
    internal KeystrokeFlags Flags { get; }

    /// <summary>Gets the pause after this event in milliseconds.</summary>
    internal int DelayAfterMilliseconds { get; }

    /// <summary>
    /// Gets the position in the text after the character this event completes, or -1 when more events of the same
    /// character follow. Every key pressed for a character is released by the event that completes it.
    /// </summary>
    internal int SourceEnd { get; }

    internal bool IsUp => (Flags & KeystrokeFlags.Up) != 0;

    internal bool IsExtended => (Flags & KeystrokeFlags.Extended) != 0;

    internal bool IsUnicode => (Flags & KeystrokeFlags.Unicode) != 0;

    internal bool CompletesCharacter => SourceEnd >= 0;

    internal Keystroke WithEnd(int sourceEnd, int delayAfterMilliseconds) {
        return new Keystroke(Code, Flags, Math.Max(DelayAfterMilliseconds, delayAfterMilliseconds), sourceEnd);
    }
}

/// <summary>How characters are turned into key events.</summary>
internal enum KeystrokeMode {
    /// <summary>Scan codes chosen from a keyboard layout.</summary>
    ScanCode,
    /// <summary>Unicode packets, independent of any layout.</summary>
    Unicode
}

/// <summary>Settings for <see cref="KeystrokePlanner"/>.</summary>
internal sealed class KeystrokePlannerOptions {
    internal KeystrokeMode Mode { get; set; } = KeystrokeMode.ScanCode;
    internal SessionLineBreakMode LineBreak { get; set; } = SessionLineBreakMode.Enter;
    internal bool StripLeadingIndentation { get; set; }
    internal UnmappedCharacterPolicy UnmappedCharacters { get; set; } = UnmappedCharacterPolicy.Fail;
    internal bool CapsLockOn { get; set; }
    internal int KeyDelayMilliseconds { get; set; }
    internal int ModifierHoldMilliseconds { get; set; }
    internal int LineDelayMilliseconds { get; set; }
}

/// <summary>Planned key events. Disposing clears them, because they encode the text.</summary>
internal sealed class KeystrokePlan : IDisposable {
    private Keystroke[] _items;

    internal KeystrokePlan(Keystroke[] items, int count, int sourceLength) {
        _items = items;
        Count = count;
        SourceLength = sourceLength;
    }

    internal int Count { get; private set; }

    internal int SourceLength { get; }

    internal Keystroke this[int index] {
        get {
            if ((uint)index >= (uint)Count) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _items[index];
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        Array.Clear(_items, 0, _items.Length);
        _items = Array.Empty<Keystroke>();
        Count = 0;
    }
}

/// <summary>Turns text into scan-code or Unicode key events without touching the desktop.</summary>
internal static class KeystrokePlanner {
    internal const ushort LeftShiftScanCode = 0x2A;
    internal const ushort LeftControlScanCode = 0x1D;
    internal const ushort AltScanCode = 0x38;
    internal const ushort EnterScanCode = 0x1C;
    internal const ushort TabScanCode = 0x0F;
    internal const ushort SpaceScanCode = 0x39;
    internal const ushort CapsLockScanCode = 0x3A;

    /// <summary>Plans the key events for <paramref name="text"/>.</summary>
    /// <param name="text">The text. It is not copied or retained.</param>
    /// <param name="layout">The receiving layout; required for <see cref="KeystrokeMode.ScanCode"/>.</param>
    /// <param name="options">Planner settings.</param>
    /// <exception cref="InvalidOperationException">A character cannot be typed and the policy is <see cref="UnmappedCharacterPolicy.Fail"/>.</exception>
    /// <exception cref="ArgumentException">The text contains an unpaired surrogate.</exception>
    internal static KeystrokePlan Plan(char[] text, IKeyboardLayoutMap? layout, KeystrokePlannerOptions options) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }
        if (options.Mode == KeystrokeMode.ScanCode && layout == null) {
            throw new ArgumentNullException(nameof(layout), "Scan-code planning needs a keyboard layout.");
        }

        var builder = new Builder(Math.Max(16, text.Length * 4));
        try {
            PlanInto(builder, text, layout, options);
            return builder.ToPlan(text.Length);
        } catch {
            builder.Clear();
            throw;
        }
    }

    private static void PlanInto(Builder builder, char[] text, IKeyboardLayoutMap? layout, KeystrokePlannerOptions options) {
        bool lineStart = true;
        int index = 0;
        while (index < text.Length) {
            char character = text[index];
            if (character == '\r' || character == '\n') {
                int next = character == '\r' && index + 1 < text.Length && text[index + 1] == '\n' ? index + 2 : index + 1;
                AddLineBreak(builder, options, next);
                index = next;
                lineStart = true;
                continue;
            }

            if (lineStart && options.StripLeadingIndentation && (character == ' ' || character == '\t')) {
                index++;
                continue;
            }

            lineStart = false;
            if (character == '\t') {
                AddTap(builder, TabScanCode, index + 1, options.KeyDelayMilliseconds);
                index++;
                continue;
            }

            if (char.IsHighSurrogate(character)) {
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1])) {
                    throw new ArgumentException($"The text has an unpaired surrogate at position {index}.", nameof(text));
                }

                // No layout produces characters outside the Basic Multilingual Plane from a single key.
                AddUnmapped(builder, options, layout, index, character, text[index + 1], index + 2, forceUnicode: options.Mode == KeystrokeMode.Unicode);
                index += 2;
                continue;
            }

            if (char.IsLowSurrogate(character)) {
                throw new ArgumentException($"The text has an unpaired surrogate at position {index}.", nameof(text));
            }

            if (options.Mode == KeystrokeMode.Unicode) {
                AddUnicode(builder, character, '\0', index + 1, options.KeyDelayMilliseconds);
                index++;
                continue;
            }

            if (char.IsControl(character) || !layout!.TryMap(character, out LayoutKey key)) {
                AddUnmapped(builder, options, layout, index, character, '\0', index + 1, forceUnicode: false);
                index++;
                continue;
            }

            bool shift = key.Shift;
            if (options.CapsLockOn) {
                if (key.CapsLock == CapsLockEffect.Unavailable) {
                    AddUnmapped(builder, options, layout, index, character, '\0', index + 1, forceUnicode: false);
                    index++;
                    continue;
                }

                if (key.CapsLock == CapsLockEffect.InvertsShift) {
                    shift = !shift;
                }
            }

            AddKey(builder, key, shift, index + 1, options);
            index++;
        }
    }

    private static void AddKey(Builder builder, LayoutKey key, bool shift, int sourceEnd, KeystrokePlannerOptions options) {
        int modifierCount = (key.AltGr ? 2 : 0) + (shift ? 1 : 0);
        int hold = modifierCount > 0 ? options.ModifierHoldMilliseconds : 0;
        if (key.AltGr) {
            // AltGr is right Alt (E0 38); left Ctrl is pressed too so layouts without AltGr still see Ctrl+Alt.
            builder.Add(LeftControlScanCode, KeystrokeFlags.None, hold);
            builder.Add(AltScanCode, KeystrokeFlags.Extended, hold);
        }
        if (shift) {
            builder.Add(LeftShiftScanCode, KeystrokeFlags.None, hold);
        }

        KeystrokeFlags keyFlags = key.Extended ? KeystrokeFlags.Extended : KeystrokeFlags.None;
        builder.Add(key.ScanCode, keyFlags, 0);
        builder.Add(key.ScanCode, keyFlags | KeystrokeFlags.Up, hold);
        if (shift) {
            builder.Add(LeftShiftScanCode, KeystrokeFlags.Up, 0);
        }
        if (key.AltGr) {
            builder.Add(AltScanCode, KeystrokeFlags.Extended | KeystrokeFlags.Up, 0);
            builder.Add(LeftControlScanCode, KeystrokeFlags.Up, 0);
        }

        if (key.Dead) {
            // A dead key followed by Space produces the accent itself.
            builder.Add(SpaceScanCode, KeystrokeFlags.None, 0);
            builder.Add(SpaceScanCode, KeystrokeFlags.Up, 0);
        }

        builder.EndCharacter(sourceEnd, options.KeyDelayMilliseconds);
    }

    private static void AddLineBreak(Builder builder, KeystrokePlannerOptions options, int sourceEnd) {
        int delay = Math.Max(options.LineDelayMilliseconds, options.KeyDelayMilliseconds);
        switch (options.LineBreak) {
            case SessionLineBreakMode.None:
                return;
            case SessionLineBreakMode.ShiftEnter:
                builder.Add(LeftShiftScanCode, KeystrokeFlags.None, options.ModifierHoldMilliseconds);
                builder.Add(EnterScanCode, KeystrokeFlags.None, 0);
                builder.Add(EnterScanCode, KeystrokeFlags.Up, options.ModifierHoldMilliseconds);
                builder.Add(LeftShiftScanCode, KeystrokeFlags.Up, 0);
                builder.EndCharacter(sourceEnd, delay);
                return;
            default:
                AddTap(builder, EnterScanCode, sourceEnd, delay);
                return;
        }
    }

    private static void AddTap(Builder builder, ushort scanCode, int sourceEnd, int delay) {
        builder.Add(scanCode, KeystrokeFlags.None, 0);
        builder.Add(scanCode, KeystrokeFlags.Up, 0);
        builder.EndCharacter(sourceEnd, delay);
    }

    private static void AddUnicode(Builder builder, char first, char second, int sourceEnd, int delay) {
        builder.Add(first, KeystrokeFlags.Unicode, 0);
        builder.Add(first, KeystrokeFlags.Unicode | KeystrokeFlags.Up, 0);
        if (second != '\0') {
            builder.Add(second, KeystrokeFlags.Unicode, 0);
            builder.Add(second, KeystrokeFlags.Unicode | KeystrokeFlags.Up, 0);
        }

        builder.EndCharacter(sourceEnd, delay);
    }

    private static void AddUnmapped(Builder builder, KeystrokePlannerOptions options, IKeyboardLayoutMap? layout, int position, char first, char second, int sourceEnd, bool forceUnicode) {
        UnmappedCharacterPolicy policy = forceUnicode ? UnmappedCharacterPolicy.Unicode : options.UnmappedCharacters;
        switch (policy) {
            case UnmappedCharacterPolicy.Unicode:
                AddUnicode(builder, first, second, sourceEnd, options.KeyDelayMilliseconds);
                return;
            case UnmappedCharacterPolicy.Skip:
                return;
            default:
                // The character itself stays out of the message; the text may be a secret.
                string capsNote = options.CapsLockOn ? " while Caps Lock is on" : string.Empty;
                throw new InvalidOperationException(
                    $"The character at position {position} cannot be typed with keyboard layout {layout?.Name ?? "(none)"}{capsNote}. " +
                    "Choose the session's layout, or allow Unicode packets or skipping for characters the layout lacks.");
        }
    }

    private sealed class Builder {
        private Keystroke[] _items;
        private int _count;

        internal Builder(int capacity) {
            _items = new Keystroke[capacity];
        }

        internal void Add(ushort code, KeystrokeFlags flags, int delayAfterMilliseconds) {
            if (_count == _items.Length) {
                var larger = new Keystroke[_items.Length * 2];
                Array.Copy(_items, larger, _count);
                Array.Clear(_items, 0, _items.Length);
                _items = larger;
            }

            _items[_count++] = new Keystroke(code, flags, delayAfterMilliseconds, -1);
        }

        internal void EndCharacter(int sourceEnd, int delayAfterMilliseconds) {
            _items[_count - 1] = _items[_count - 1].WithEnd(sourceEnd, delayAfterMilliseconds);
        }

        internal KeystrokePlan ToPlan(int sourceLength) {
            return new KeystrokePlan(_items, _count, sourceLength);
        }

        internal void Clear() {
            Array.Clear(_items, 0, _items.Length);
            _count = 0;
        }
    }
}
