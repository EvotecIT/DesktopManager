namespace DesktopManager.Tests;

/// <summary>
/// Protects how keyboard layouts are named and read. The native comparison only reads layouts that are already loaded
/// on the machine; it never loads, activates or unloads one.
/// </summary>
[TestClass]
public class KeyboardLayoutMapTests {
    [TestMethod]
    [DataRow("00000407", "00000407")]
    [DataRow("0000040c", "0000040C")]
    [DataRow("0x409", "00000409")]
    [DataRow(" 00010415 ", "00010415")]
    [DataRow("de-DE", "00000407")]
    [DataRow("pl-PL", "00000415")]
    public void ResolveLayoutId_AcceptsLayoutIdentifiersAndCultureNames(string input, string expected) {
        Assert.AreEqual(expected, NativeKeyboardLayoutMap.ResolveLayoutId(input));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not-a-culture-zz")]
    [DataRow("de")]
    [DataRow("0x123456789")]
    public void ResolveLayoutId_RejectsValuesThatNameNoLayout(string input) {
        Assert.ThrowsExactly<ArgumentException>(() => NativeKeyboardLayoutMap.ResolveLayoutId(input));
    }

    [TestMethod]
    [DataRow(0x0409, "US")]
    [DataRow(0x0415, "PL")]
    public void NativeMap_FindsTheSameKeysAsTheKnownLayoutTable(int layoutId, string tableName) {
        IntPtr layout = SessionTypingNative.GetKeyboardLayouts().FirstOrDefault(handle => ((handle.ToInt64() >> 16) & 0xFFFF) == layoutId);
        if (layout == IntPtr.Zero) {
            Assert.Inconclusive($"Keyboard layout {layoutId:X8} is not loaded on this machine; it is never loaded by tests.");
        }

        var expected = (StaticKeyboardLayout)KeystrokePlannerTests.Layout(tableName);
        using NativeKeyboardLayoutMap native = NativeKeyboardLayoutMap.ForLayout(layoutId.ToString("X8"));
        var mismatches = new List<string>();
        foreach (char character in expected.Characters) {
            expected.TryMap(character, out LayoutKey want);
            if (!native.TryMap(character, out LayoutKey actual)) {
                mismatches.Add($"U+{(int)character:X4} unmapped");
                continue;
            }

            bool asciiLetter = character < 0x80 && char.IsLetter(character);
            if (actual.ScanCode != want.ScanCode || actual.Extended || actual.Shift != want.Shift || actual.AltGr != want.AltGr ||
                actual.Dead != want.Dead || (asciiLetter && actual.CapsLock != want.CapsLock)) {
                mismatches.Add($"U+{(int)character:X4} scan {actual.ScanCode:X2} shift {actual.Shift} altGr {actual.AltGr} dead {actual.Dead} caps {actual.CapsLock}");
            }
        }

        Assert.AreEqual(0, mismatches.Count, string.Join("; ", mismatches));
    }
}
