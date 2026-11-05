namespace DesktopManager.Tests;

/// <summary>
/// Protects which physical keys session typing presses for text on a given layout. Plans are written as
/// <c>+2A</c> (press scan code 2A), <c>-2A</c> (release), <c>E</c> before a code for the E0 prefix,
/// <c>U</c> before a code for a Unicode packet, and <c>|n</c> where the character ending at text position n is complete.
/// </summary>
[TestClass]
public class KeystrokePlannerTests {
    [TestMethod]
    [DataRow("US", 'a', "+1E -1E |1")]
    [DataRow("US", 'A', "+2A +1E -1E -2A |1")]
    [DataRow("US", '@', "+2A +03 -03 -2A |1")]
    [DataRow("UK", '@', "+2A +28 -28 -2A |1")]
    [DataRow("UK", '"', "+2A +03 -03 -2A |1")]
    [DataRow("UK", '#', "+2B -2B |1")]
    [DataRow("DE", 'z', "+15 -15 |1")]
    [DataRow("DE", 'y', "+2C -2C |1")]
    [DataRow("FR", 'a', "+10 -10 |1")]
    [DataRow("FR", '1', "+2A +02 -02 -2A |1")]
    [DataRow("FR", 'm', "+27 -27 |1")]
    public void Plan_PressesTheKeyThatProducesTheCharacterOnTheSessionLayout(string layoutName, char character, string expected) {
        Assert.AreEqual(expected, Describe(new[] { character }, Layout(layoutName), new KeystrokePlannerOptions()));
    }

    [TestMethod]
    [DataRow("DE", '@', "+1D +E38 +10 -10 -E38 -1D |1")]
    [DataRow("DE", '€', "+1D +E38 +12 -12 -E38 -1D |1")]
    [DataRow("FR", '@', "+1D +E38 +0B -0B -E38 -1D |1")]
    [DataRow("PL", 'ą', "+1D +E38 +1E -1E -E38 -1D |1")]
    [DataRow("PL", 'Ż', "+1D +E38 +2A +2C -2C -2A -E38 -1D |1")]
    public void Plan_AltGrPressesLeftCtrlAndExtendedRightAltAndReleasesInReverse(string layoutName, char character, string expected) {
        Assert.AreEqual(expected, Describe(new[] { character }, Layout(layoutName), new KeystrokePlannerOptions()));
    }

    [TestMethod]
    [DataRow("DE", '^', "+29 -29 +39 -39 |1")]
    [DataRow("DE", '`', "+2A +0D -0D -2A +39 -39 |1")]
    [DataRow("FR", '¨', "+2A +1A -1A -2A +39 -39 |1")]
    [DataRow("PL", '~', "+2A +29 -29 -2A +39 -39 |1")]
    [DataRow("US", '^', "+2A +07 -07 -2A |1")]
    public void Plan_DeadKeyIsFollowedBySpaceSoTheAccentAppearsByItself(string layoutName, char character, string expected) {
        Assert.AreEqual(expected, Describe(new[] { character }, Layout(layoutName), new KeystrokePlannerOptions()));
    }

    [TestMethod]
    [DataRow("US", 'a', "+2A +1E -1E -2A |1")]
    [DataRow("US", 'A', "+1E -1E |1")]
    [DataRow("US", '1', "+02 -02 |1")]
    [DataRow("FR", '1', "+02 -02 |1")]
    [DataRow("FR", '&', "+2A +02 -02 -2A |1")]
    public void Plan_CompensatesForCapsLockOnlyOnKeysCapsLockAffects(string layoutName, char character, string expected) {
        var options = new KeystrokePlannerOptions { CapsLockOn = true };

        Assert.AreEqual(expected, Describe(new[] { character }, Layout(layoutName), options));
    }

    [TestMethod]
    public void Plan_CharacterCapsLockMakesUnreachableFollowsTheUnmappedPolicy() {
        var layout = new SingleKeyLayout('x', new LayoutKey(0x2D, false, false, false, false, CapsLockEffect.Unavailable));

        Assert.ThrowsExactly<InvalidOperationException>(() => KeystrokePlanner.Plan(new[] { 'x' }, layout, new KeystrokePlannerOptions { CapsLockOn = true }));
        Assert.AreEqual("+2D -2D |1", Describe(new[] { 'x' }, layout, new KeystrokePlannerOptions()));
    }

    [TestMethod]
    [DataRow("a\rb", SessionLineBreakMode.Enter, "+1E -1E |1 +1C -1C |2 +30 -30 |3")]
    [DataRow("a\nb", SessionLineBreakMode.Enter, "+1E -1E |1 +1C -1C |2 +30 -30 |3")]
    [DataRow("a\r\nb", SessionLineBreakMode.Enter, "+1E -1E |1 +1C -1C |3 +30 -30 |4")]
    [DataRow("a\r\n\r\nb", SessionLineBreakMode.Enter, "+1E -1E |1 +1C -1C |3 +1C -1C |5 +30 -30 |6")]
    [DataRow("a\r\nb", SessionLineBreakMode.ShiftEnter, "+1E -1E |1 +2A +1C -1C -2A |3 +30 -30 |4")]
    [DataRow("a\r\nb", SessionLineBreakMode.None, "+1E -1E |1 +30 -30 |4")]
    [DataRow("a\tb", SessionLineBreakMode.Enter, "+1E -1E |1 +0F -0F |2 +30 -30 |3")]
    public void Plan_TypesLineBreaksAsOneKeyPressAndTabAsTheTabKey(string text, SessionLineBreakMode mode, string expected) {
        var options = new KeystrokePlannerOptions { LineBreak = mode };

        Assert.AreEqual(expected, Describe(text.ToCharArray(), Layout("US"), options));
    }

    [TestMethod]
    public void Plan_StripsLeadingIndentationOfEveryLineButNotInnerSpaces() {
        var options = new KeystrokePlannerOptions { StripLeadingIndentation = true };

        string plan = Describe("  a b\n\t\tb".ToCharArray(), Layout("US"), options);

        Assert.AreEqual("+1E -1E |3 +39 -39 |4 +30 -30 |5 +1C -1C |6 +30 -30 |9", plan);
    }

    [TestMethod]
    public void Plan_UnmappedCharacterFailsByDefaultWithoutRevealingIt() {
        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(
            () => KeystrokePlanner.Plan("ab€".ToCharArray(), Layout("US"), new KeystrokePlannerOptions()));

        StringAssert.Contains(error.Message, "position 2");
        Assert.AreEqual(-1, error.Message.IndexOf('€'), "The message must not contain the character; the text may be a secret.");
    }

    [TestMethod]
    public void Plan_UnmappedCharacterCanBeSentAsUnicodeOrSkipped() {
        char[] text = "a€b".ToCharArray();

        string unicode = Describe(text, Layout("US"), new KeystrokePlannerOptions { UnmappedCharacters = UnmappedCharacterPolicy.Unicode });
        string skipped = Describe(text, Layout("US"), new KeystrokePlannerOptions { UnmappedCharacters = UnmappedCharacterPolicy.Skip });

        Assert.AreEqual("+1E -1E |1 +U20AC -U20AC |2 +30 -30 |3", unicode);
        Assert.AreEqual("+1E -1E |1 +30 -30 |3", skipped);
    }

    [TestMethod]
    public void Plan_SurrogatePairIsOneCharacterSentAsBothUnits() {
        char[] text = "a\U0001F600".ToCharArray();

        string scanCodes = Describe(text, Layout("US"), new KeystrokePlannerOptions { UnmappedCharacters = UnmappedCharacterPolicy.Unicode });
        string unicode = Describe(text, null, new KeystrokePlannerOptions { Mode = KeystrokeMode.Unicode });

        Assert.AreEqual("+1E -1E |1 +UD83D -UD83D +UDE00 -UDE00 |3", scanCodes);
        Assert.AreEqual("+U0061 -U0061 |1 +UD83D -UD83D +UDE00 -UDE00 |3", unicode);
        Assert.ThrowsExactly<InvalidOperationException>(() => KeystrokePlanner.Plan(text, Layout("US"), new KeystrokePlannerOptions()));
    }

    [TestMethod]
    [DataRow('a', (char)0xD83D)]
    [DataRow((char)0xDE00, 'a')]
    public void Plan_UnpairedSurrogateIsRejectedInEveryMode(char first, char second) {
        char[] text = { first, second };

        Assert.ThrowsExactly<ArgumentException>(() => KeystrokePlanner.Plan(text, Layout("US"), new KeystrokePlannerOptions { UnmappedCharacters = UnmappedCharacterPolicy.Unicode }));
        Assert.ThrowsExactly<ArgumentException>(() => KeystrokePlanner.Plan(text, null, new KeystrokePlannerOptions { Mode = KeystrokeMode.Unicode }));
    }

    [TestMethod]
    public void Plan_UnicodeModeStillUsesTheEnterAndTabKeys() {
        string plan = Describe("é\r\n\t".ToCharArray(), null, new KeystrokePlannerOptions { Mode = KeystrokeMode.Unicode });

        Assert.AreEqual("+U00E9 -U00E9 |1 +1C -1C |3 +0F -0F |4", plan);
    }

    [TestMethod]
    public void Plan_HoldsModifiersAroundTheKeyAndPausesAfterCharactersAndLines() {
        var options = new KeystrokePlannerOptions {
            KeyDelayMilliseconds = 7,
            ModifierHoldMilliseconds = 30,
            LineDelayMilliseconds = 90
        };

        using KeystrokePlan plan = KeystrokePlanner.Plan("A\nb".ToCharArray(), Layout("US"), options);
        int[] delays = Enumerable.Range(0, plan.Count).Select(index => plan[index].DelayAfterMilliseconds).ToArray();

        // Shift down, A down, A up, Shift up | Enter down, Enter up | B down, B up.
        CollectionAssert.AreEqual(new[] { 30, 0, 30, 7, 0, 90, 0, 7 }, delays);
    }


    internal static IKeyboardLayoutMap Layout(string name) {
        return name switch {
            "US" => StaticKeyboardLayout.UnitedStates(),
            "UK" => StaticKeyboardLayout.UnitedKingdom(),
            "DE" => StaticKeyboardLayout.German(),
            "FR" => StaticKeyboardLayout.French(),
            "PL" => StaticKeyboardLayout.PolishProgrammers(),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
    }

    internal static string Describe(char[] text, IKeyboardLayoutMap? layout, KeystrokePlannerOptions options) {
        using KeystrokePlan plan = KeystrokePlanner.Plan(text, layout, options);
        var parts = new List<string>();
        for (int index = 0; index < plan.Count; index++) {
            Keystroke keystroke = plan[index];
            string prefix = (keystroke.IsUnicode ? "U" : string.Empty) + (keystroke.IsExtended ? "E" : string.Empty);
            string code = keystroke.Code.ToString(keystroke.IsUnicode ? "X4" : "X2");
            parts.Add((keystroke.IsUp ? "-" : "+") + prefix + code);
            if (keystroke.CompletesCharacter) {
                parts.Add("|" + keystroke.SourceEnd);
            }
        }

        return string.Join(" ", parts);
    }

    private sealed class SingleKeyLayout : IKeyboardLayoutMap {
        private readonly char _character;
        private readonly LayoutKey _key;

        internal SingleKeyLayout(char character, LayoutKey key) {
            _character = character;
            _key = key;
        }

        public string Name => "single key";

        public bool TryMap(char character, out LayoutKey key) {
            key = _key;
            return character == _character;
        }
    }
}
