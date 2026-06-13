using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>Tests low-level hotkey registration matching.</summary>
public class LowLevelKeyboardHotkeyRegistrationTests {
    [TestMethod]
    /// <summary>Matches a hotkey when required left/right modifier keys are down.</summary>
    public void Matches_ReturnsTrue_WhenRequiredModifiersAreDown() {
        var registration = new LowLevelKeyboardHotkeyRegistration(
            1,
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            VirtualKey.VK_6,
            _ => { });

        var down = new HashSet<VirtualKey> {
            VirtualKey.VK_LCONTROL,
            VirtualKey.VK_LMENU,
            VirtualKey.VK_RSHIFT
        };

        Assert.IsTrue(registration.Matches(VirtualKey.VK_6, down.Contains));
    }

    [TestMethod]
    /// <summary>Does not match a hotkey when a required modifier is missing.</summary>
    public void Matches_ReturnsFalse_WhenRequiredModifierIsMissing() {
        var registration = new LowLevelKeyboardHotkeyRegistration(
            1,
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            VirtualKey.VK_6,
            _ => { });

        var down = new HashSet<VirtualKey> {
            VirtualKey.VK_LCONTROL,
            VirtualKey.VK_LMENU
        };

        Assert.IsFalse(registration.Matches(VirtualKey.VK_6, down.Contains));
    }

    [TestMethod]
    /// <summary>Does not match a hotkey when unrelated modifiers are also down.</summary>
    public void Matches_ReturnsFalse_WhenExtraModifierIsDown() {
        var registration = new LowLevelKeyboardHotkeyRegistration(
            1,
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            VirtualKey.VK_6,
            _ => { });

        var down = new HashSet<VirtualKey> {
            VirtualKey.VK_LCONTROL,
            VirtualKey.VK_LMENU,
            VirtualKey.VK_RSHIFT,
            VirtualKey.VK_LWIN
        };

        Assert.IsFalse(registration.Matches(VirtualKey.VK_6, down.Contains));
    }

    [TestMethod]
    /// <summary>Reports completed modifier set only when all required modifier groups are down.</summary>
    public void CountRequiredModifiersDown_ReturnsCompletedModifierGroupCount() {
        var registration = new LowLevelKeyboardHotkeyRegistration(
            1,
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            VirtualKey.VK_6,
            _ => { });

        var down = new HashSet<VirtualKey> {
            VirtualKey.VK_LCONTROL,
            VirtualKey.VK_LMENU,
            VirtualKey.VK_RSHIFT
        };

        Assert.AreEqual(3, registration.RequiredModifierCount);
        Assert.AreEqual(3, registration.CountRequiredModifiersDown(down.Contains));
    }

    [TestMethod]
    /// <summary>Only required modifier keys are included in a suppressible chord.</summary>
    public void GetChordKeys_ReturnsTriggerAndRequiredModifiersOnly() {
        var registration = new LowLevelKeyboardHotkeyRegistration(
            1,
            HotkeyModifiers.Control | HotkeyModifiers.Shift,
            VirtualKey.VK_5,
            _ => { });

        IReadOnlyList<VirtualKey> chordKeys = registration.GetChordKeys();

        Assert.IsTrue(chordKeys.Contains(VirtualKey.VK_5));
        Assert.IsTrue(chordKeys.Contains(VirtualKey.VK_CONTROL));
        Assert.IsTrue(chordKeys.Contains(VirtualKey.VK_SHIFT));
        Assert.IsFalse(chordKeys.Contains(VirtualKey.VK_MENU));
    }
}
