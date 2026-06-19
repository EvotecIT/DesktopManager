#if NET8_0_OR_GREATER
using DesktopManager.App.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests compact runtime status text used by tray surfaces.
/// </summary>
public class RuntimeStatusSummaryTests {
    [TestMethod]
    /// <summary>
    /// Enabled profiles should report registered hotkeys and configured rules.
    /// </summary>
    public void FormatTrayTooltip_EnabledProfile_IncludesHotkeysAndRules() {
        string text = RuntimeStatusSummary.FormatTrayTooltip(
            profileEnabled: true,
            registeredHotkeys: 9,
            layoutCount: 1,
            ruleCount: 3,
            profileName: "Workstation");

        StringAssert.Contains(text, "Workstation");
        StringAssert.Contains(text, "9 hotkey(s)");
        StringAssert.Contains(text, "3 rule(s)");
    }

    [TestMethod]
    /// <summary>
    /// Disabled profiles should make the disabled state visible in the tray tooltip.
    /// </summary>
    public void FormatTrayTooltip_DisabledProfile_IncludesDisabledState() {
        string text = RuntimeStatusSummary.FormatTrayTooltip(
            profileEnabled: false,
            registeredHotkeys: 0,
            layoutCount: 0,
            ruleCount: 0,
            profileName: "Workstation");

        StringAssert.Contains(text, "hotkeys disabled");
        StringAssert.Contains(text, "no rules");
    }

    [TestMethod]
    /// <summary>
    /// Very long profile names should be trimmed before reaching the tray tooltip limit.
    /// </summary>
    public void FormatTrayTooltip_LongProfile_Truncates() {
        string text = RuntimeStatusSummary.FormatTrayTooltip(
            profileEnabled: true,
            registeredHotkeys: 12,
            layoutCount: 2,
            ruleCount: 20,
            profileName: new string('X', 200));

        Assert.IsTrue(text.Length <= 120);
        StringAssert.EndsWith(text, "...");
    }
}
#endif
