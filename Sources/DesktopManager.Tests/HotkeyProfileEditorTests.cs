#if NET8_0_OR_GREATER
using DesktopManager.App.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests reusable profile editing helpers used by the app UI.
/// </summary>
public class HotkeyProfileEditorTests {
    [TestMethod]
    /// <summary>
    /// New custom actions should be disabled and receive a unique id.
    /// </summary>
    public void CreateCustomWindowAction_UsesUniqueDisabledAction() {
        List<HotkeyFunctionDefinition> existing = new() {
            new HotkeyFunctionDefinition {
                Id = "custom-window-action"
            }
        };

        HotkeyFunctionDefinition function = HotkeyProfileEditor.CreateCustomWindowAction(existing);

        Assert.AreEqual("custom-window-action-2", function.Id);
        Assert.IsFalse(function.Enabled);
        Assert.AreEqual(HotkeyActionKinds.ManageWindow, function.ActionType);
        Assert.AreEqual(MonitorTargets.Current, function.WindowAction.Monitor);
        Assert.AreEqual(WindowPlacements.Maximize, function.WindowAction.Placement);
    }

    [TestMethod]
    /// <summary>
    /// Rules created from actions should normalize empty patterns and clone the placement action.
    /// </summary>
    public void CreateRuleFromFunction_NormalizesPatternsAndClonesAction() {
        HotkeyFunctionDefinition function = new() {
            Id = "move-left",
            Name = "Move Left",
            WindowAction = new WindowHotkeyActionDefinition {
                Monitor = MonitorTargets.TopLeft,
                MonitorIndex = 1,
                Placement = WindowPlacements.LeftHalf
            }
        };

        WindowRuleDefinition rule = HotkeyProfileEditor.CreateRuleFromFunction(
            function,
            "  *PowerShell*  ",
            " ",
            Array.Empty<WindowRuleDefinition>());

        Assert.AreEqual("move-left", rule.Id);
        Assert.AreEqual("*PowerShell*", rule.Match.TitlePattern);
        Assert.AreEqual("*", rule.Match.ProcessNamePattern);
        Assert.AreEqual(WindowPlacements.LeftHalf, rule.Action.Placement);
        Assert.AreEqual(1, rule.Action.MonitorIndex);
        Assert.AreNotSame(function.WindowAction, rule.Action);
    }

    [TestMethod]
    /// <summary>
    /// Rule ids should be made unique inside a layout.
    /// </summary>
    public void CreateRuleFromFunction_DuplicateRuleId_AddsSuffix() {
        HotkeyFunctionDefinition function = new() {
            Id = "move-left",
            Name = "Move Left"
        };
        List<WindowRuleDefinition> existing = new() {
            new WindowRuleDefinition {
                Id = "move-left"
            }
        };

        WindowRuleDefinition rule = HotkeyProfileEditor.CreateRuleFromFunction(function, "*", "*", existing);

        Assert.AreEqual("move-left-2", rule.Id);
    }
}
#endif
