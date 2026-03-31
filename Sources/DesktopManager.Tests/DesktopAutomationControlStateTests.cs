using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for control focus and state helpers on DesktopAutomationService.
/// </summary>
public class DesktopAutomationControlStateTests {
    [TestMethod]
    /// <summary>
    /// Ensures control-state lookup rejects invalid control handles.
    /// </summary>
    public void DesktopAutomationService_GetControlState_ZeroControlHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.GetControlState(new IntPtr(1), IntPtr.Zero));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control focus rejects invalid window handles.
    /// </summary>
    public void DesktopAutomationService_FocusControl_ZeroWindowHandle_ThrowsArgumentException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.FocusControl(IntPtr.Zero, new IntPtr(1)));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control-state observation reflects live enabled and visible flags.
    /// </summary>
    public void DesktopAutomationService_GetControlState_ReturnsLiveFlags() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();

        TextBox? editor = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Control State Harness",
            form => {
                editor = new TextBox {
                    Name = "EditorBox",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    Text = "state"
                };
                form.Controls.Add(editor);
            });

        Assert.IsNotNull(editor);
        DesktopControlState? state = new DesktopAutomationService().GetControlState(harness.Window.Handle, editor.Handle);

        Assert.IsNotNull(state);
        Assert.AreEqual(true, state.IsEnabled);
        Assert.AreEqual(true, state.IsVisible);
        Assert.AreEqual(editor.Handle, state.ControlHandle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures control enablement can be toggled for a live WinForms textbox.
    /// </summary>
    public void DesktopAutomationService_SetControlEnabled_TogglesLiveTextbox() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowMutationTests();

        TextBox? editor = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Enable Control Harness",
            form => {
                editor = new TextBox {
                    Name = "EditorBox",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    Text = "toggle"
                };
                form.Controls.Add(editor);
            });

        Assert.IsNotNull(editor);
        DesktopAutomationService automation = new();
        DesktopControlState disabledState = automation.SetControlEnabled(harness.Window.Handle, editor.Handle, false);
        Application.DoEvents();
        Task.Delay(100).Wait();

        Assert.AreEqual(false, disabledState.IsEnabled);

        DesktopControlState enabledState = automation.SetControlEnabled(harness.Window.Handle, editor.Handle, true);
        Application.DoEvents();
        Task.Delay(100).Wait();

        Assert.AreEqual(true, enabledState.IsEnabled);
    }

    [TestMethod]
    /// <summary>
    /// Ensures control visibility can be toggled for a live WinForms textbox.
    /// </summary>
    public void DesktopAutomationService_SetControlVisibility_TogglesLiveTextbox() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowMutationTests();

        TextBox? editor = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Visibility Control Harness",
            form => {
                editor = new TextBox {
                    Name = "EditorBox",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    Text = "toggle"
                };
                form.Controls.Add(editor);
            });

        Assert.IsNotNull(editor);
        DesktopAutomationService automation = new();
        DesktopControlState hiddenState = automation.SetControlVisibility(harness.Window.Handle, editor.Handle, false);
        Application.DoEvents();
        Task.Delay(100).Wait();

        Assert.AreEqual(false, hiddenState.IsVisible);

        DesktopControlState visibleState = automation.SetControlVisibility(harness.Window.Handle, editor.Handle, true);
        Application.DoEvents();
        Task.Delay(100).Wait();

        Assert.AreEqual(true, visibleState.IsVisible);
    }

    [TestMethod]
    /// <summary>
    /// Ensures control focus can be redirected between live controls.
    /// </summary>
    public void DesktopAutomationService_FocusControl_FocusesLiveTextbox() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireForegroundWindowUiTests();

        TextBox? firstEditor = null;
        TextBox? secondEditor = null;
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(
            "DesktopManager Focus Control Harness",
            form => {
                firstEditor = new TextBox {
                    Name = "FirstEditor",
                    Left = 12,
                    Top = 12,
                    Width = 180,
                    Text = "first"
                };
                secondEditor = new TextBox {
                    Name = "SecondEditor",
                    Left = 12,
                    Top = 42,
                    Width = 180,
                    Text = "second"
                };
                form.Controls.Add(firstEditor);
                form.Controls.Add(secondEditor);
                form.Shown += (_, _) => {
                    firstEditor.Focus();
                };
            });

        Assert.IsNotNull(firstEditor);
        Assert.IsNotNull(secondEditor);
        WindowManager manager = new();
        manager.ActivateWindow(harness.Window);
        Application.DoEvents();
        Task.Delay(150).Wait();

        DesktopAutomationService automation = new();
        DesktopControlState focusedState = automation.FocusControl(harness.Window.Handle, secondEditor.Handle, ensureForegroundWindow: true);

        Application.DoEvents();
        Task.Delay(150).Wait();

        Assert.AreEqual(true, focusedState.IsFocused);
        Assert.AreEqual(secondEditor.Handle, automation.GetFocusedControlObservation(harness.Window.Handle)?.FocusedHandle);
    }
}
