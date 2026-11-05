namespace DesktopManager.Tests;

/// <summary>
/// Hand-written key positions of common layouts, independent of the layouts installed on the test machine.
/// Only the keys the tests rely on are listed.
/// </summary>
internal sealed class StaticKeyboardLayout : IKeyboardLayoutMap {
    private const char None = '\0';
    private readonly Dictionary<char, LayoutKey> _keys = new();
    private readonly HashSet<char> _dead = new();
    private readonly HashSet<ushort> _capsInvertsShift = new();

    private StaticKeyboardLayout(string name) {
        Name = name;
    }

    public string Name { get; }

    public IEnumerable<char> Characters => _keys.Keys;

    public bool TryMap(char character, out LayoutKey key) {
        return _keys.TryGetValue(character, out key);
    }

    /// <summary>US English, 00000409.</summary>
    internal static StaticKeyboardLayout UnitedStates() {
        return WithUsBase(new StaticKeyboardLayout("US")).Build();
    }

    /// <summary>Polish (Programmers), 00000415: US positions plus AltGr letters and a dead tilde.</summary>
    internal static StaticKeyboardLayout PolishProgrammers() {
        var layout = new StaticKeyboardLayout("PL Programmers");
        layout.MarkDead('~');
        WithUsBase(layout);
        layout.AltGr(0x1E, 'ą', 'Ą').AltGr(0x2E, 'ć', 'Ć').AltGr(0x12, 'ę', 'Ę').AltGr(0x26, 'ł', 'Ł')
            .AltGr(0x31, 'ń', 'Ń').AltGr(0x18, 'ó', 'Ó').AltGr(0x1F, 'ś', 'Ś').AltGr(0x2D, 'ź', 'Ź')
            .AltGr(0x2C, 'ż', 'Ż').AltGr(0x16, '€', None);
        return layout.Build();
    }

    /// <summary>United Kingdom, 00000809.</summary>
    internal static StaticKeyboardLayout UnitedKingdom() {
        var layout = new StaticKeyboardLayout("UK");
        layout.Row(0x02, "1234567890-=", "!\"£$%^&*()_+")
            .Row(0x10, "qwertyuiop[]", "QWERTYUIOP{}")
            .Row(0x1E, "asdfghjkl;'", "ASDFGHJKL:@")
            .Key(0x2B, '#', '~')
            .Key(0x29, '`', '¬').Key(0x56, '\\', '|')
            .Row(0x2C, "zxcvbnm,./", "ZXCVBNM<>?")
            .Key(0x39, ' ', None)
            .AltGr(0x05, '€', None);
        return layout.Build();
    }

    /// <summary>German, 00000407: QWERTZ with dead ^, ´ and `.</summary>
    internal static StaticKeyboardLayout German() {
        var layout = new StaticKeyboardLayout("DE");
        layout.MarkDead('^').MarkDead('´').MarkDead('`');
        layout.Row(0x02, "1234567890ß´", "!\"§$%&/()=?`")
            .Row(0x10, "qwertzuiopü+", "QWERTZUIOPÜ*")
            .Row(0x1E, "asdfghjklöä", "ASDFGHJKLÖÄ")
            .Key(0x2B, '#', '\'')
            .Key(0x29, '^', '°').Key(0x56, '<', '>')
            .Row(0x2C, "yxcvbnm,.-", "YXCVBNM;:_")
            .Key(0x39, ' ', None)
            .AltGr(0x10, '@', None).AltGr(0x12, '€', None).AltGr(0x08, '{', None).AltGr(0x09, '[', None)
            .AltGr(0x0A, ']', None).AltGr(0x0B, '}', None).AltGr(0x0C, '\\', None).AltGr(0x1B, '~', None)
            .AltGr(0x56, '|', None);
        return layout.Build();
    }

    /// <summary>French, 0000040C: AZERTY, digits on Shift, Caps Lock giving digits, dead circumflex.</summary>
    internal static StaticKeyboardLayout French() {
        var layout = new StaticKeyboardLayout("FR");
        layout.MarkDead('^').MarkDead('¨');
        layout.Row(0x02, "&é\"'(-è_çà)=", "1234567890°+")
            .Row(0x10, "azertyuiop^$", "AZERTYUIOP¨£")
            .Row(0x1E, "qsdfghjklmù", "QSDFGHJKLM%")
            .Key(0x2B, '*', 'µ')
            .Key(0x56, '<', '>')
            .Row(0x2C, "wxcvbn,;:!", "WXCVBN?./§")
            .Key(0x39, ' ', None)
            .AltGr(0x04, '#', None).AltGr(0x05, '{', None).AltGr(0x06, '[', None).AltGr(0x07, '|', None)
            .AltGr(0x09, '\\', None).AltGr(0x0B, '@', None).AltGr(0x0C, ']', None).AltGr(0x0D, '}', None)
            .AltGr(0x12, '€', None);
        for (ushort scan = 0x02; scan <= 0x0B; scan++) {
            layout._capsInvertsShift.Add(scan);
        }

        return layout.Build();
    }

    private static StaticKeyboardLayout WithUsBase(StaticKeyboardLayout layout) {
        return layout.Row(0x02, "1234567890-=", "!@#$%^&*()_+")
            .Row(0x10, "qwertyuiop[]", "QWERTYUIOP{}")
            .Row(0x1E, "asdfghjkl;'", "ASDFGHJKL:\"")
            .Key(0x29, '`', '~').Key(0x2B, '\\', '|')
            .Row(0x2C, "zxcvbnm,./", "ZXCVBNM<>?")
            .Key(0x39, ' ', None);
    }

    private StaticKeyboardLayout MarkDead(char character) {
        _dead.Add(character);
        return this;
    }

    private StaticKeyboardLayout Row(ushort firstScanCode, string normal, string shifted) {
        for (int index = 0; index < normal.Length; index++) {
            Key((ushort)(firstScanCode + index), normal[index], index < shifted.Length ? shifted[index] : None);
        }

        return this;
    }

    private StaticKeyboardLayout Key(ushort scanCode, char normal, char shifted) {
        Add(normal, scanCode, shift: false, altGr: false);
        Add(shifted, scanCode, shift: true, altGr: false);
        return this;
    }

    private StaticKeyboardLayout AltGr(ushort scanCode, char normal, char shifted) {
        Add(normal, scanCode, shift: false, altGr: true);
        Add(shifted, scanCode, shift: true, altGr: true);
        return this;
    }

    private void Add(char character, ushort scanCode, bool shift, bool altGr) {
        if (character == None || _keys.ContainsKey(character)) {
            return;
        }

        _keys[character] = new LayoutKey(scanCode, extended: false, shift, altGr, _dead.Contains(character), CapsLockEffect.None);
    }

    private StaticKeyboardLayout Build() {
        // Letters with a case pair invert Shift under Caps Lock, as do keys a layout lists explicitly.
        foreach (char character in new List<char>(_keys.Keys)) {
            LayoutKey key = _keys[character];
            bool letterPair = !key.AltGr && char.IsLetter(character) && char.ToUpperInvariant(character) != char.ToLowerInvariant(character);
            if (letterPair || (!key.AltGr && _capsInvertsShift.Contains(key.ScanCode))) {
                _keys[character] = new LayoutKey(key.ScanCode, key.Extended, key.Shift, key.AltGr, key.Dead, CapsLockEffect.InvertsShift);
            }
        }

        return this;
    }
}
