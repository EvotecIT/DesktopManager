#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for layout-aware keyboard input helpers.
/// </summary>
public class KeyboardInputServiceTests {
    [TestMethod]
    /// <summary>
    /// Ensures keyboard-layout modifier state maps to Shift, Control, and Alt keys in order.
    /// </summary>
    public void GetModifierKeysForKeyboardState_MapsShiftControlAndAlt() {
        IReadOnlyList<VirtualKey> modifiers = KeyboardInputService.GetModifierKeysForKeyboardState(0b011);

        CollectionAssert.AreEqual(new[] {
            VirtualKey.VK_SHIFT,
            VirtualKey.VK_CONTROL
        }, modifiers.ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Ensures AltGr-style modifier state maps to Shift plus right Alt when required.
    /// </summary>
    public void GetModifierKeysForKeyboardState_MapsShiftAndAltGr() {
        IReadOnlyList<VirtualKey> modifiers = KeyboardInputService.GetModifierKeysForKeyboardState(0b111);

        CollectionAssert.AreEqual(new[] {
            VirtualKey.VK_SHIFT,
            VirtualKey.VK_RMENU
        }, modifiers.ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Ensures packed VkKeyScan results map to a key plus modifiers.
    /// </summary>
    public void TryCreateKeyboardLayoutStroke_MapsKeyAndModifiers() {
        bool mapped = KeyboardInputService.TryCreateKeyboardLayoutStroke(unchecked((short)0x0141), out KeyboardInputService.KeyboardLayoutStroke stroke);

        Assert.IsTrue(mapped);
        Assert.AreEqual(VirtualKey.VK_A, stroke.Key);
        CollectionAssert.AreEqual(new[] { VirtualKey.VK_SHIFT }, stroke.Modifiers.ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Ensures packed AltGr-style VkKeyScan results map to Control plus Alt modifiers.
    /// </summary>
    public void TryCreateKeyboardLayoutStroke_MapsAltGrStyleModifierState() {
        bool mapped = KeyboardInputService.TryCreateKeyboardLayoutStroke(unchecked((short)0x0645), out KeyboardInputService.KeyboardLayoutStroke stroke);

        Assert.IsTrue(mapped);
        Assert.AreEqual(VirtualKey.VK_E, stroke.Key);
        CollectionAssert.AreEqual(new[] { VirtualKey.VK_RMENU }, stroke.Modifiers.ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Ensures unsupported VkKeyScan results are rejected cleanly.
    /// </summary>
    public void TryCreateKeyboardLayoutStroke_UnsupportedCharacter_ReturnsFalse() {
        bool mapped = KeyboardInputService.TryCreateKeyboardLayoutStroke(-1, out KeyboardInputService.KeyboardLayoutStroke stroke);

        Assert.IsFalse(mapped);
        Assert.AreEqual(default, stroke);
    }

    [TestMethod]
    /// <summary>
    /// Ensures hosted-session US scancode mapping keeps braces on the bracket keys instead of the local-layout symbol path.
    /// </summary>
    public void TryCreateUsKeyboardScanCodeStroke_MapsBracesToBracketScanCodes() {
        bool openMapped = KeyboardInputService.TryCreateUsKeyboardScanCodeStroke('{', out KeyboardInputService.ScanCodeStroke openStroke);
        bool closeMapped = KeyboardInputService.TryCreateUsKeyboardScanCodeStroke('}', out KeyboardInputService.ScanCodeStroke closeStroke);

        Assert.IsTrue(openMapped);
        Assert.IsTrue(closeMapped);
        Assert.AreEqual((ushort)0x1A, openStroke.ScanCode);
        Assert.IsTrue(openStroke.ShiftRequired);
        Assert.AreEqual((ushort)0x1B, closeStroke.ScanCode);
        Assert.IsTrue(closeStroke.ShiftRequired);
    }

    [TestMethod]
    /// <summary>
    /// Ensures hosted-session US scancode mapping keeps letters on their physical key positions.
    /// </summary>
    public void TryCreateUsKeyboardScanCodeStroke_MapsLettersAndCase() {
        bool lowerMapped = KeyboardInputService.TryCreateUsKeyboardScanCodeStroke('a', out KeyboardInputService.ScanCodeStroke lowerStroke);
        bool upperMapped = KeyboardInputService.TryCreateUsKeyboardScanCodeStroke('A', out KeyboardInputService.ScanCodeStroke upperStroke);

        Assert.IsTrue(lowerMapped);
        Assert.IsTrue(upperMapped);
        Assert.AreEqual(lowerStroke.ScanCode, upperStroke.ScanCode);
        Assert.IsFalse(lowerStroke.ShiftRequired);
        Assert.IsTrue(upperStroke.ShiftRequired);
    }

    [TestMethod]
    [DataRow('[', (ushort)0x1A, false)]
    [DataRow('{', (ushort)0x1A, true)]
    [DataRow(']', (ushort)0x1B, false)]
    [DataRow('}', (ushort)0x1B, true)]
    [DataRow('\\', (ushort)0x2B, false)]
    [DataRow('|', (ushort)0x2B, true)]
    [DataRow('/', (ushort)0x35, false)]
    [DataRow('?', (ushort)0x35, true)]
    [DataRow('@', (ushort)0x03, true)]
    [DataRow('_', (ushort)0x0C, true)]
    [DataRow('+', (ushort)0x0D, true)]
    [DataRow(':', (ushort)0x27, true)]
    [DataRow('"', (ushort)0x28, true)]
    [DataRow('<', (ushort)0x33, true)]
    [DataRow('>', (ushort)0x34, true)]
    [DataRow('~', (ushort)0x29, true)]
    [DataRow('^', (ushort)0x07, true)]
    /// <summary>
    /// Ensures hosted-session US scancode mapping stays stable for script-heavy ASCII symbols.
    /// </summary>
    public void TryCreateUsKeyboardScanCodeStroke_MapsScriptHeavyAsciiSymbols(char character, ushort expectedScanCode, bool expectedShiftRequired) {
        bool mapped = KeyboardInputService.TryCreateUsKeyboardScanCodeStroke(character, out KeyboardInputService.ScanCodeStroke stroke);

        Assert.IsTrue(mapped, $"Expected a US scancode mapping for '{character}'.");
        Assert.AreEqual(expectedScanCode, stroke.ScanCode);
        Assert.AreEqual(expectedShiftRequired, stroke.ShiftRequired);
    }
}
#endif
