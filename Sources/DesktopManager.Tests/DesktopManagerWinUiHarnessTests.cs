using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Live automation certification for the MAUI/WinUI-backed harness.
/// </summary>
public class DesktopManagerWinUiHarnessTests {
    private const int StatusTimeoutMilliseconds = 20000;

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the WinUI harness publishes its initial status snapshot for external verification.
    /// </summary>
    public void DesktopManagerWinUiHarnessSession_ReadStatus_ExposesModernSnapshot() {
        RequireLiveWinUiHarness();
        using var session = DesktopManagerWinUiHarnessSession.Start("status");

        DesktopManagerWinUiHarnessStatus status = session.WaitForStatus(
            candidate => !string.IsNullOrWhiteSpace(candidate.WindowTitle) &&
                string.Equals(candidate.SelectedOption, "Alpha", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish its initial status snapshot in time.");

        Assert.AreEqual("Alpha", status.SelectedOption);
        Assert.IsTrue(status.AutomationCheckBoxChecked);
        Assert.AreEqual("Modern controls ready.", status.ActionStatus);
        Assert.IsFalse(string.IsNullOrWhiteSpace(status.EditorText));
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures DesktopManager can resolve MAUI/WinUI harness controls by AutomationId.
    /// </summary>
    public void DesktopAutomationService_GetControls_ResolvesWinUiHarnessControlsByAutomationId() {
        RequireLiveWinUiHarness();
        using var session = DesktopManagerWinUiHarnessSession.Start("discovery");
        DesktopAutomationService automation = new();

        session.WaitForStatus(
            candidate => string.Equals(candidate.SelectedOption, "Alpha", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The WinUI harness did not reach its ready state in time.");

        WindowControlTargetInfo editor = WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "ModernEditor",
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            "The WinUI harness editor was not discoverable through DesktopManager.");
        Assert.AreEqual(WindowControlSource.UiAutomation, editor.Control.Source);
        Assert.IsFalse(string.IsNullOrWhiteSpace(editor.Control.ControlType));

        WindowControlTargetInfo applyButton = WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "ModernApplyButton",
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            "The WinUI harness apply button was not discoverable through DesktopManager.");
        Assert.AreEqual(WindowControlSource.UiAutomation, applyButton.Control.Source);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures DesktopManager can mutate the WinUI harness editor and verify the resulting status snapshot.
    /// </summary>
    public void DesktopAutomationService_SetControlText_UpdatesWinUiHarnessEditor() {
        RequireLiveWinUiHarness();
        using var session = DesktopManagerWinUiHarnessSession.Start("edit");
        DesktopAutomationService automation = new();

        WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "ModernEditor",
                UseUiAutomation = true,
                EnsureForegroundWindow = true,
                AllowForegroundInputFallback = true
            },
            "The WinUI harness editor was not discoverable before text mutation.");

        string expectedText = "winui-harness-" + Guid.NewGuid().ToString("N");
        IReadOnlyList<WindowControlTargetInfo> updatedControls = automation.SetControlText(
            session.CreateWindowQuery(),
            new WindowControlQueryOptions {
                AutomationIdPattern = "ModernEditor",
                UseUiAutomation = true,
                EnsureForegroundWindow = true,
                AllowForegroundInputFallback = true
            },
            expectedText,
            allWindows: false,
            allControls: false);
        Assert.AreEqual(1, updatedControls.Count);

        DesktopManagerWinUiHarnessStatus updatedStatus = session.WaitForStatus(
            candidate => string.Equals(candidate.EditorText, expectedText, StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish the updated editor text after DesktopManager mutation.");
        Assert.AreEqual(expectedText, updatedStatus.EditorText);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures DesktopManager can invoke the WinUI harness apply button and verify the resulting action status.
    /// </summary>
    public void DesktopAutomationService_ClickControls_InvokesWinUiHarnessApplyButton() {
        RequireLiveWinUiHarness();
        using var session = DesktopManagerWinUiHarnessSession.Start("apply");
        DesktopAutomationService automation = new();

        WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "ModernApplyButton",
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            "The WinUI harness apply button was not discoverable before invoke.");

        IReadOnlyList<WindowControlTargetInfo> clickedControls = automation.ClickControls(
            session.CreateWindowQuery(),
            new WindowControlQueryOptions {
                AutomationIdPattern = "ModernApplyButton",
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            MouseButton.Left,
            allWindows: false,
            allControls: false);
        Assert.AreEqual(1, clickedControls.Count);

        DesktopManagerWinUiHarnessStatus updatedStatus = session.WaitForStatus(
            candidate => string.Equals(candidate.ActionStatus, "Applied option 'Alpha' with checkbox enabled.", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish the apply-button action result.");
        Assert.AreEqual("Applied option 'Alpha' with checkbox enabled.", updatedStatus.ActionStatus);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures DesktopManager can toggle the WinUI harness checkbox and read back the verified UI Automation state.
    /// </summary>
    public void DesktopAutomationService_SetControlCheckState_UpdatesWinUiHarnessCheckbox() {
        RequireLiveWinUiHarness();
        using var session = DesktopManagerWinUiHarnessSession.Start("checkbox");
        DesktopAutomationService automation = new();

        WindowControlTargetInfo checkBox = WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "ModernCheckBox",
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            "The WinUI harness checkbox was not discoverable before mutation.");

        automation.SetControlCheckState(checkBox.Control, false);

        DesktopManagerWinUiHarnessStatus uncheckedStatus = session.WaitForStatus(
            candidate => candidate.AutomationCheckBoxChecked == false,
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish the unchecked checkbox state.");
        Assert.IsFalse(uncheckedStatus.AutomationCheckBoxChecked);

        DesktopControlState uncheckedState = automation.GetControlState(checkBox.Control);
        Assert.AreEqual(false, uncheckedState.IsChecked);

        automation.SetControlCheckState(checkBox.Control, true);

        DesktopManagerWinUiHarnessStatus checkedStatus = session.WaitForStatus(
            candidate => candidate.AutomationCheckBoxChecked,
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish the checked checkbox state.");
        Assert.IsTrue(checkedStatus.AutomationCheckBoxChecked);

        DesktopControlState checkedState = automation.GetControlState(checkBox.Control);
        Assert.AreEqual(true, checkedState.IsChecked);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures DesktopManager can select a WinUI harness picker item and read back the verified UI Automation selection.
    /// </summary>
    public void DesktopAutomationService_SetControlSelectedValue_UpdatesWinUiHarnessPicker() {
        RequireLiveWinUiHarness();
        using var session = DesktopManagerWinUiHarnessSession.Start("picker");
        DesktopAutomationService automation = new();

        WindowControlTargetInfo picker = WaitForSingleControl(
            automation,
            session,
            new WindowControlQueryOptions {
                AutomationIdPattern = "ModernPicker",
                UseUiAutomation = true,
                EnsureForegroundWindow = true
            },
            "The WinUI harness picker was not discoverable before selection.");

        automation.SetControlSelectedValue(picker.Control, "Beta");

        DesktopManagerWinUiHarnessStatus updatedStatus = session.WaitForStatus(
            candidate => string.Equals(candidate.SelectedOption, "Beta", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish the updated picker selection.");
        Assert.AreEqual("Beta", updatedStatus.SelectedOption);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the query-driven CLI mutation path can toggle a WinUI checkbox without relying on a control handle.
    /// </summary>
    public void DesktopOperations_SetControlCheckState_UpdatesWinUiHarnessCheckboxByQuery() {
        RequireLiveWinUiHarness();
        using var session = DesktopManagerWinUiHarnessSession.Start("checkbox-cli");

        var windowCriteria = new global::DesktopManager.Cli.WindowSelectionCriteria {
            TitlePattern = session.WindowTitle,
            ProcessNamePattern = "DesktopManager.WinUiHarness",
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
        var controlCriteria = new global::DesktopManager.Cli.ControlSelectionCriteria {
            AutomationIdPattern = "ModernCheckBox",
            ControlTypePattern = "CheckBox",
            UiAutomation = true,
            IncludeUiAutomation = true,
            EnsureForegroundWindow = true
        };

        global::DesktopManager.Cli.ControlActionResult result = global::DesktopManager.Cli.DesktopOperations.SetControlCheckState(
            windowCriteria,
            controlCriteria,
            check: false,
            allWindows: false);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("uia-toggle", result.SafetyMode);

        DesktopManagerWinUiHarnessStatus updatedStatus = session.WaitForStatus(
            candidate => candidate.AutomationCheckBoxChecked == false,
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish the unchecked checkbox state after the query-driven CLI mutation.");
        Assert.IsFalse(updatedStatus.AutomationCheckBoxChecked);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the query-driven CLI mutation path can select a WinUI picker value without relying on a control handle.
    /// </summary>
    public void DesktopOperations_SetControlSelectedValue_UpdatesWinUiHarnessPickerByQuery() {
        RequireLiveWinUiHarness();
        using var session = DesktopManagerWinUiHarnessSession.Start("picker-cli");

        var windowCriteria = new global::DesktopManager.Cli.WindowSelectionCriteria {
            TitlePattern = session.WindowTitle,
            ProcessNamePattern = "DesktopManager.WinUiHarness",
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
        var controlCriteria = new global::DesktopManager.Cli.ControlSelectionCriteria {
            AutomationIdPattern = "ModernPicker",
            ControlTypePattern = "ComboBox",
            UiAutomation = true,
            IncludeUiAutomation = true,
            EnsureForegroundWindow = true
        };

        global::DesktopManager.Cli.ControlActionResult result = global::DesktopManager.Cli.DesktopOperations.SetControlSelectedValue(
            windowCriteria,
            controlCriteria,
            "Gamma",
            allWindows: false);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("uia-selection-item", result.SafetyMode);

        DesktopManagerWinUiHarnessStatus updatedStatus = session.WaitForStatus(
            candidate => string.Equals(candidate.SelectedOption, "Gamma", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish the updated picker selection after the query-driven CLI mutation.");
        Assert.AreEqual("Gamma", updatedStatus.SelectedOption);
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures the query-driven CLI mutation path can invoke a WinUI button without relying on a control handle.
    /// </summary>
    public void DesktopOperations_ClickControl_InvokesWinUiHarnessApplyButtonByQuery() {
        RequireLiveWinUiHarness();
        using var session = DesktopManagerWinUiHarnessSession.Start("apply-cli");

        var windowCriteria = new global::DesktopManager.Cli.WindowSelectionCriteria {
            TitlePattern = session.WindowTitle,
            ProcessNamePattern = "DesktopManager.WinUiHarness",
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
        var controlCriteria = new global::DesktopManager.Cli.ControlSelectionCriteria {
            AutomationIdPattern = "ModernApplyButton",
            ControlTypePattern = "Button",
            UiAutomation = true,
            IncludeUiAutomation = true,
            EnsureForegroundWindow = true
        };

        global::DesktopManager.Cli.ControlActionResult result = global::DesktopManager.Cli.DesktopOperations.ClickControl(
            windowCriteria,
            controlCriteria,
            button: "left",
            allWindows: false);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("uia-direct-invoke", result.SafetyMode);

        DesktopManagerWinUiHarnessStatus updatedStatus = session.WaitForStatus(
            candidate => string.Equals(candidate.ActionStatus, "Applied option 'Alpha' with checkbox enabled.", StringComparison.Ordinal),
            StatusTimeoutMilliseconds,
            "The WinUI harness did not publish the apply-button action result after the query-driven CLI mutation.");
        Assert.AreEqual("Applied option 'Alpha' with checkbox enabled.", updatedStatus.ActionStatus);
    }

    private static WindowControlTargetInfo WaitForSingleControl(DesktopAutomationService automation, DesktopManagerWinUiHarnessSession session, WindowControlQueryOptions controlQuery, string failureMessage) {
        session.WaitForStatus(
            _ => {
                IReadOnlyList<WindowControlTargetInfo> controls = automation.GetControls(session.CreateWindowQuery(), controlQuery, allWindows: false, allControls: true);
                return controls.Count == 1;
            },
            StatusTimeoutMilliseconds,
            failureMessage);

        IReadOnlyList<WindowControlTargetInfo> resolvedControls = automation.GetControls(session.CreateWindowQuery(), controlQuery, allWindows: false, allControls: true);
        Assert.AreEqual(1, resolvedControls.Count, failureMessage);
        return resolvedControls.Single();
    }

    private static void RequireLiveWinUiHarness() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows.");
        }

        TestHelper.RequireExternalDesktopApplicationTests();
    }
}
