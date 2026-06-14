using System;
using System.Collections.Generic;

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
    /// <summary>Does not match a hotkey when only a required modifier key is pressed.</summary>
    public void Matches_ReturnsFalse_ForStandaloneModifierKey() {
        var registration = new LowLevelKeyboardHotkeyRegistration(
            1,
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            VirtualKey.VK_6,
            _ => { },
            new LowLevelKeyboardHotkeyOptions {
                SuppressPotentialChordKeys = true,
                ExclusiveForegroundProcessNames = new[] { "RemoteDesktopManager" }
            });

        var down = new HashSet<VirtualKey> {
            VirtualKey.VK_LCONTROL,
            VirtualKey.VK_LMENU,
            VirtualKey.VK_RSHIFT
        };

        Assert.IsFalse(registration.Matches(VirtualKey.VK_RSHIFT, down.Contains));
    }
}
